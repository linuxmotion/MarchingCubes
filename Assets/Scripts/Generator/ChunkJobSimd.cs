using System.Collections.Generic;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Profiling;
using UnityEngine;
using UnityEngine.Assertions;


namespace Assets.Scripts.SIMD
{

    [BurstCompile]
    struct ChunkJobSimd : IJob
    {

        public float2 IJobID;

        public NoiseParameters noiseParameters;
        public TerrainParameters terrainParameters;

        public NativeArray<Voxel> Points;

        public NativeArray<int> NumberOfTriangles;
        public NativeArray<Vector3> Vertices;
        public NativeArray<int> Triangles;

        public NativeArray<bool> UpdateMainThread;

        NativeArray<Voxel> cell;
        NativeArray<int4> edgeConnections;
        NativeArray<float4> vertices ;

        /// <summary>
        /// Initialize the IJob for use with a chunk
        /// </summary>
        /// <param name="noiseP">The noise parameters to create the chunk with</param>
        /// <param name="terrainP">The terrain parameters that controls generation, LOD and size</param>
        /// <param name="id">A vector in 2d space the represnts the chunk centers for a given terrainP </param>
        public ChunkJobSimd(NoiseParameters noiseP, TerrainParameters terrainP, Vector2 id)
        {

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

            cell = new NativeArray<Voxel>(8, Allocator.Persistent);
           edgeConnections = new NativeArray<int4>(5, Allocator.Persistent);
            vertices = new NativeArray<float4>(5 * 3, Allocator.Persistent);

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
            if (Vertices.IsCreated)
                Vertices.Dispose();
            if (Triangles.IsCreated)
                Triangles.Dispose();

            IJobID = id;
            noiseParameters = noiseP;
            terrainParameters = terrainP;

            if (terrainParameters.SamplingLength == 0 || terrainParameters.SamplingWidth == 0 || terrainParameters.SamplingHeight == 0)
                throw new UnityException("Cannot have zero size volume");

            int size = (terrainParameters.SamplingLength + 1) * (terrainParameters.SamplingWidth + 1) * (terrainParameters.SamplingHeight + 1) * terrainParameters.Scale;

            int numPossiblePoints = (terrainParameters.SamplingLength * terrainParameters.Scale + 1) * (terrainParameters.SamplingWidth * terrainParameters.Scale + 1) * (terrainParameters.SamplingHeight * terrainParameters.Scale + 1);
            
            Points = new NativeArray<Voxel>(numPossiblePoints, Allocator.Persistent);
            Vertices = new NativeArray<Vector3>(size * 15, Allocator.Persistent);
            Triangles = new NativeArray<int>(size * 15, Allocator.Persistent);


        }

        /// <summary>
        /// Called after a job is completed to release a large portion of the native memory
        /// </summary>
        public void Cleanup() {

            Vertices.Dispose();
            Triangles.Dispose();
            Points.Dispose();
            NumberOfTriangles[0] = 0;

        }
        public void Dispose()
        {
            NumberOfTriangles.Dispose();
            UpdateMainThread.Dispose();

            cell.Dispose();
            edgeConnections.Dispose();
            vertices.Dispose();

        }


        public void Execute()
        {
            float4 point = 0;
            point.x = terrainParameters.Origin.x;
            point.y = terrainParameters.Origin.y;
            point.z = terrainParameters.Origin.z;


            GenerateScalarField(point);
            CreateCubeData();
            // CalulateMesh(VoxelCells);
            UpdateMainThread[0] = true;

        }



        static readonly ProfilerMarker s_CalculateMeshMarker = new ProfilerMarker("CalculateMesh");



        static readonly ProfilerMarker s_CreateCubeDataMarker = new ProfilerMarker("CreateCubeData");
        private void CreateCubeData()
        {

            s_CreateCubeDataMarker.Begin();
            int levelOffset, rowOffset;

            int Length = terrainParameters.SamplingLength;
            int Width = terrainParameters.SamplingWidth;
            int Height = terrainParameters.SamplingHeight;
            int Scale = terrainParameters.Scale;
            int levelSize = ((Width * Scale + 1) * (Length * Scale + 1));
            int rowSize = Width * Scale + 1;
            int index = 0;
            for (int level = 0; level < Height * Scale; level++)
            {

                levelOffset = level * levelSize;

                for (int row = 0; row < Length * Scale; row++)
                {
                    rowOffset = row * (Width * Scale + 1);
                    for (int coloumn = 0; coloumn < Width * Scale; coloumn++)
                    {

                        cell[0] = Points[coloumn + rowOffset + levelOffset]; //(c, r)
                        cell[1] = Points[coloumn + rowOffset + levelOffset + levelSize]; //(c, r)
                        cell[2] = Points[coloumn + rowOffset + levelOffset + levelSize + 1]; //(c, r)
                        cell[3] = Points[coloumn + rowOffset + levelOffset + 1]; //(c, r)

                        cell[4] = Points[coloumn + rowOffset + levelOffset + rowSize]; //(c, r)
                        cell[5] = Points[coloumn + rowOffset + levelOffset + rowSize + levelSize]; //(c, r)
                        cell[6] = Points[coloumn + rowOffset + levelOffset + rowSize + levelSize + 1]; //(c, r)
                        cell[7] = Points[coloumn + rowOffset + levelOffset + rowSize + 1]; //(c, r)

                        byte edgelist = 0;
                        // create the edge list
                        VoxelCell.CreateEdgeList(ref edgelist, 0, cell);

                        // check if the cell in on the surface
                        if (VoxelCell.IsOnSurface(edgelist))
                        {
                            int numTri = 0;
                            VoxelCell.CreateVertexConnections( edgelist, ref numTri, ref edgeConnections );
                            VoxelCell.CalculateMesh(numTri,ref vertices, edgeConnections, cell  );
                            for (int i = 0; i < numTri*3; i++) {
                                Vertices[index + i] = new Vector3(vertices[i].x, vertices[i].y, vertices[i].z);
                                Triangles[index + i] = index + i;

                            }
                            index += numTri*3;
                        }


                    }

                }
            }
            NumberOfTriangles[0] = index / 3;

            s_CreateCubeDataMarker.End();
             
        }


        static readonly ProfilerMarker s_GenerateScalarFieldfMarker = new ProfilerMarker("GenerateScalarField");
        private void GenerateScalarField(float4 around)
        {
            s_GenerateScalarFieldfMarker.Begin();


            float x, y, z;

            int Length = terrainParameters.SamplingLength;
            int Width = terrainParameters.SamplingWidth;
            int Height = terrainParameters.SamplingHeight;
            int Scale = terrainParameters.Scale;
            float Scalef = (float)Scale;
            int bedrock = terrainParameters.BedrockLevel;
            float4 pos;
            int index = 0;
            for (int level = 0; level <= Height * Scale; level++)
            {
                // levelOffset = level * ((Width * Scale + 1) * (Length * Scale + 1));

                for (int row = 0; row <= Length * Scale; row++)
                {
                    for (int column = 0; column <= Width * Scale; column++)
                    {

                        x = terrainParameters.Origin.x - ((Width) / 2f) + (column / Scalef);
                        y = bedrock + (level / Scalef);
                        z = terrainParameters.Origin.z - ((Length) / 2f) + (row / Scalef);
                         
                        //pos =; ;// + new Vector3(x, y, z);
                        pos.x = x;
                        pos.y = y;
                        pos.z = z;
                        pos.w = 0;

                        // TODO: Generate better noise - create a flat plane

                        float4 noise = Noise.GenerateNoise(pos, noiseParameters, terrainParameters.SurfaceLevel);
                        Voxel v = new(pos, noise);
                        Points[index++] = v;

                    }

                }
            }
           // Points.CopyFrom(nativePointsBuffer.ToArray());

            s_GenerateScalarFieldfMarker.End();
        }

    }
}