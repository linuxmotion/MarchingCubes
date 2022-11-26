using System.Collections.Generic;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Profiling;
using UnityEngine;
using UnityEngine.Assertions;

namespace Assets.Scripts.Threaded
{
    struct ChunkJob : IJob
    {

        public Vector2 IJobID;

        public NoiseParameters noiseParameters;
        public TerrainParameters terrainParameters;

        public NativeArray<Voxel> Points;
        public NativeArray<int> NumberOfTriangles;
        public NativeArray<Vector3> Vertices;
        public NativeArray<int> Triangles;
        public NativeArray<bool> UpdateMainThread;

        /// <summary>
        /// Initialize the IJob for use with a chunk
        /// </summary>
        /// <param name="noiseP">The noise parameters to create the chunk with</param>
        /// <param name="terrainP">The terrain parameters that controls generation, LOD and size</param>
        /// <param name="id">A vector in 2d space the represnts the chunk centers for a given terrainP </param>
        public ChunkJob(NoiseParameters noiseP, TerrainParameters terrainP, Vector2 id)
        {
            ComputeShader shader = Resources.Load<ComputeShader>("NewComputeShader");

            IJobID = id;
            noiseParameters = noiseP;
            terrainParameters = terrainP;
            NumberOfTriangles = new NativeArray<int>(1, Allocator.Persistent);
            UpdateMainThread = new NativeArray<bool>(1, Allocator.Persistent);

            if (terrainParameters.SamplingLength == 0 || terrainParameters.SamplingWidth == 0 || terrainParameters.SamplingHeight == 0)
                throw new UnityException("Cannot have zero size volume");

            int size = (terrainParameters.SamplingLength + 1) * (terrainParameters.SamplingWidth + 1) * (terrainParameters.SamplingHeight + 1) * terrainParameters.Scale;

            int numPossiblePoints = (terrainParameters.SamplingLength * terrainParameters.Scale + 1) * (terrainParameters.SamplingWidth * terrainParameters.Scale + 1) * (terrainParameters.SamplingHeight * terrainParameters.Scale + 1);
            Points = new NativeArray<Voxel>(numPossiblePoints, Allocator.Persistent);
            Vertices = new NativeArray<Vector3>(size * 15, Allocator.Persistent);
            Triangles = new NativeArray<int>(size * 15, Allocator.Persistent);

        }
        /// <summary>
        /// Reinitialize the chunk job for loading a chunk
        /// </summary>
        /// <param name="noiseP">The new noise parameters</param>
        /// <param name="terrainP"> The new parameters to create the terrain with</param>
        /// <param name="id">The id of the chunk as an x,z coordinate pair</param>
        public void ReInitialize(NoiseParameters noiseP, TerrainParameters terrainP, Vector2 id)
        {
            // ComputeBuffer buffer = new ComputeBuffer(1, 4);
            // ComputeShader shader = Resources.Load<ComputeShader>("NewComputeShader");
            // shader.SetBuffer(0, 0, buffer);
            // shader.Dispatch(0, 1, 0, 0);

            // The array may change size, dump them then set the struct back up
            if (Points.IsCreated)
                Points.Dispose();
            if (NumberOfTriangles.IsCreated)
                NumberOfTriangles.Dispose();
            if (Vertices.IsCreated)
                Vertices.Dispose();
            if (Triangles.IsCreated)
                Triangles.Dispose();
            if (UpdateMainThread.IsCreated)
                UpdateMainThread.Dispose();

            IJobID = id;
            noiseParameters = noiseP;
            terrainParameters = terrainP;
            NumberOfTriangles = new NativeArray<int>(1, Allocator.Persistent);
            UpdateMainThread = new NativeArray<bool>(1, Allocator.Persistent);

            if (terrainParameters.SamplingLength == 0 || terrainParameters.SamplingWidth == 0 || terrainParameters.SamplingHeight == 0)
                throw new UnityException("Cannot have zero size volume");

            int size = (terrainParameters.SamplingLength + 1) * (terrainParameters.SamplingWidth + 1) * (terrainParameters.SamplingHeight + 1) * terrainParameters.Scale;

            int numPossiblePoints = (terrainParameters.SamplingLength * terrainParameters.Scale + 1) * (terrainParameters.SamplingWidth * terrainParameters.Scale + 1) * (terrainParameters.SamplingHeight * terrainParameters.Scale + 1);
            Points = new NativeArray<Voxel>(numPossiblePoints, Allocator.Persistent);
            Vertices = new NativeArray<Vector3>(size * 15, Allocator.Persistent);
            Triangles = new NativeArray<int>(size * 15, Allocator.Persistent);



        }


        public void Execute()
        {

            GenerateScalarField(terrainParameters.Origin);
            // have a set of volumetric data
            // turn all that data into cubes to march over
            List<VoxelCell> VoxelCells = new List<VoxelCell>();
            CreateCubeData(ref VoxelCells);
            CalulateMesh(ref VoxelCells);
            UpdateMainThread[0] = true;

        }



        static readonly ProfilerMarker s_CalculateMeshMarker = new ProfilerMarker("CalculateMesh");


        private void CalulateMesh(ref List<VoxelCell> VoxelCells)
        {
            s_CalculateMeshMarker.Begin();
            //for each voxel cell we get a mesh that consists of vertices and triangle.
            //now we need to put all the vertices and indices into a single array
            // since cooridinate are givine in world space we will need to shift all the but the first cell
            // into the correct world space location by adding the the column number to the given triangle
            // we also must correctly index the new triangles

            // We will have at most the amount of cells times 5 since we can have no more than 5 triangles per mesh
            List<Vector3[]> agVertices = new List<Vector3[]>(VoxelCells.Count);// TODO: Dont allocate as much space 
            List<int[]> agTriangles = new List<int[]>(VoxelCells.Count);   // has up to 5 triangles
            int numTri = 0;


            for (int i = 0; i < VoxelCells.Count; i++)
            {

                agVertices.Add(VoxelCells[i].mTriangleVertices); // has 3,6,9,12, or 15 vertices
                agTriangles.Add(VoxelCells[i].mTriangleIndex);   // has up to 5 triangles
                numTri += VoxelCells[i].mNumberTriangles;

            }
            NumberOfTriangles[0] = numTri;

            Assert.IsTrue(agVertices.Count == agTriangles.Count);

            List<Vector3> linVertices = new List<Vector3>(agVertices.Count * 5);
            List<int> linIndexedTriangles = new List<int>(agVertices.Count * 5);
            int offset = 0;
            Vector3[] vertList;
            int[] triList;
            // now we must linearlize the vertices List<> into a array[]
            for (int i = 0; i < agVertices.Count; i++)
            {
                vertList = agVertices[i];
                triList = agTriangles[i];
                for (int j = 0; j < vertList.Length; j++)
                {
                    linIndexedTriangles.Add(agTriangles[i][j] + offset);
                    linVertices.Add(agVertices[i][j]);
                }
                offset += triList.Length;
            }

            Assert.IsTrue(linVertices.Count == linIndexedTriangles.Count);

            for (int i = 0; i < linVertices.Count; i++)
            {
                Vertices[i] = linVertices[i];
                Triangles[i] = linIndexedTriangles[i];

            }


            s_CalculateMeshMarker.End();
        }

        static readonly ProfilerMarker s_CreateCubeDataMarker = new ProfilerMarker("CreateCubeData");
        [BurstDiscard]
        private void CreateCubeData(ref List<VoxelCell> VoxelCells)
        {
            s_CreateCubeDataMarker.Begin();
            int levelOffset, rowOffset;

            int Length = terrainParameters.SamplingLength;
            int Width = terrainParameters.SamplingWidth;
            int Height = terrainParameters.SamplingHeight;
            int Scale = terrainParameters.Scale;
            List<Voxel> localPoints = new(Points.ToArray());

            int levelSize = ((Width * Scale + 1) * (Length * Scale + 1));
            int rowSize = Width * Scale + 1;
            VoxelCell cell = new VoxelCell(0);
            bool newCellNeeded = false;

            for (int level = 0; level < Height * Scale; level++)
            {

                levelOffset = level * levelSize;

                for (int row = 0; row < Length * Scale; row++)
                {
                    rowOffset = row * (Width * Scale + 1);
                    for (int coloumn = 0; coloumn < Width * Scale; coloumn++)
                    {


                        if (newCellNeeded)
                        {
                            cell = new VoxelCell(0);
                            newCellNeeded = false;
                        }

                        cell.mVoxel[0] = localPoints[coloumn + rowOffset + levelOffset];  //(c, r) 
                        cell.mVoxel[1] = localPoints[coloumn + rowOffset + levelOffset + levelSize];  //(c, r) 
                        cell.mVoxel[2] = localPoints[coloumn + rowOffset + levelOffset + levelSize + 1];  //(c, r) 
                        cell.mVoxel[3] = localPoints[coloumn + rowOffset + levelOffset + 1];  //(c, r) 

                        cell.mVoxel[4] = localPoints[coloumn + rowOffset + levelOffset + rowSize];  //(c, r) 
                        cell.mVoxel[5] = localPoints[coloumn + rowOffset + levelOffset + rowSize + levelSize];  //(c, r) 
                        cell.mVoxel[6] = localPoints[coloumn + rowOffset + levelOffset + rowSize + levelSize + 1];  //(c, r) 
                        cell.mVoxel[7] = localPoints[coloumn + rowOffset + levelOffset + rowSize + 1];  //(c, r) 

                        // create the edge list
                        cell.CreateEdgeList();

                        // check if the cell in on the surface
                        if (cell.IsOnSurface())
                        {
                            cell.CreateVertexConnections();
                            cell.CalculateMesh();
                            VoxelCells.Add(cell);
                            newCellNeeded = true;
                        }


                    }

                }
            }

            s_CreateCubeDataMarker.End();

        }


        static readonly ProfilerMarker s_GenerateScalarFieldfMarker = new ProfilerMarker("GenerateScalarField");

        [BurstCompile]
        private void GenerateScalarField(Vector3 around)
        {
            s_GenerateScalarFieldfMarker.Begin();


            float x, y, z;

            int Length = terrainParameters.SamplingLength;
            int Width = terrainParameters.SamplingWidth;
            int Height = terrainParameters.SamplingHeight;
            int Scale = terrainParameters.Scale;
            float Scalef = (float)Scale;
            int bedrock = terrainParameters.BedrockLevel;
            Vector3 pos;
            List<Voxel> nativePointsBuffer = new List<Voxel>(Length * Width * Height * Scale * Scale * Scale);

            for (int level = 0; level <= Height * Scale; level++)
            {
                // levelOffset = level * ((Width * Scale + 1) * (Length * Scale + 1));

                for (int row = 0; row <= Length * Scale; row++)
                {
                    for (int column = 0; column <= Width * Scale; column++)
                    {

                        x = -((Width) / 2f) + (column / Scalef);
                        y = bedrock + (level / Scalef);
                        z = -((Length) / 2f) + (row / Scalef);

                        pos = around; ;// + new Vector3(x, y, z);
                        pos.x += x;
                        pos.y += y;
                        pos.z += z;

                        // TODO: Generate better noise - create a flat plane

                        float noise = Noise.GenerateNoise(pos, noiseParameters, terrainParameters);
                        Voxel v = new(pos, noise);
                        nativePointsBuffer.Add(v);

                    }

                }
            }
            Points.CopyFrom(nativePointsBuffer.ToArray());

            s_GenerateScalarFieldfMarker.End();
        }
    }
}