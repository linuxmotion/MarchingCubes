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
    struct SimdChunkJob : IJob
    {
        public Vector3 ChunkCenter;
        public bool SmoothNormals;

        public NoiseParameters MnoiseParameters;
        public TerrainParameters MterrainParameters;

        public NativeArray<Voxel> Points;

        public NativeArray<int> NumberOfTriangles;
        public NativeArray<Vector3> Vertices;
        public NativeArray<int> Triangles;

        public NativeArray<bool> UpdateMainThread;

        NativeArray<Voxel> cell;
        NativeArray<int4> edgeConnections;
        NativeArray<float4> vertices;

        /// <summary>
        /// Initialize the IJob for use with a chunk
        /// </summary>
        /// <param name="noiseP">The noise parameters to create the chunk with</param>
        /// <param name="terrainP">The terrain parameters that controls generation, LOD and size</param>
        /// <param name="center">A vector in 2d space the represnts the chunk centers for a given terrainP </param>
        public SimdChunkJob(NoiseParameters noiseP, TerrainParameters terrainP, Vector3 center)
        {
            SmoothNormals = false;
            ChunkCenter = center;
            MnoiseParameters = noiseP;
            MterrainParameters = terrainP;
            NumberOfTriangles = new NativeArray<int>(2, Allocator.Persistent);// we will also hold the number of points in index 1
            UpdateMainThread = new NativeArray<bool>(1, Allocator.Persistent);

            if (MterrainParameters.SamplingLength == 0 || MterrainParameters.SamplingWidth == 0 || MterrainParameters.SamplingHeight == 0)
                throw new UnityException("Cannot have zero size volume");

            int size = (MterrainParameters.SamplingLength + 1) * (MterrainParameters.SamplingWidth + 1) * (MterrainParameters.SamplingHeight + 1) * MterrainParameters.Scale;

            int numPossiblePoints = (MterrainParameters.SamplingLength * MterrainParameters.Scale + 1) * (MterrainParameters.SamplingWidth * MterrainParameters.Scale + 1) * (MterrainParameters.SamplingHeight * MterrainParameters.Scale + 1);
            Points = new NativeArray<Voxel>(numPossiblePoints, Allocator.Persistent);
            Vertices = new NativeArray<Vector3>(size * 15, Allocator.Persistent);
            Triangles = new NativeArray<int>(size * 15, Allocator.Persistent);

            cell = new NativeArray<Voxel>(8, Allocator.Persistent);
            edgeConnections = new NativeArray<int4>(5, Allocator.Persistent);
            vertices = new NativeArray<float4>(5 * 3, Allocator.Persistent);
        }

        public void RecenterChunk(Vector3 newCenter)
        {
            ChunkCenter = newCenter;
            UpdateMainThread[0] = false;
            NumberOfTriangles[0] = 0;
        }
        public void ResetChunkParameters(NoiseParameters noiseP, TerrainParameters terrainP, Vector3 center)
        {
            ChunkCenter = center;
            MnoiseParameters = noiseP;
            MterrainParameters = terrainP;

            if (MterrainParameters.SamplingLength == 0 || MterrainParameters.SamplingWidth == 0 || MterrainParameters.SamplingHeight == 0)
                throw new UnityException("Cannot have zero size volume");

            int size = (MterrainParameters.SamplingLength + 1) * (MterrainParameters.SamplingWidth + 1) * (MterrainParameters.SamplingHeight + 1) * MterrainParameters.Scale;
            int numPossiblePoints = (MterrainParameters.SamplingLength * MterrainParameters.Scale + 1) * (MterrainParameters.SamplingWidth * MterrainParameters.Scale + 1) * (MterrainParameters.SamplingHeight * MterrainParameters.Scale + 1);

            // the parameters are smaller, no need to allocate more memeory
            if (Points.Length >= numPossiblePoints)
            {
                Debug.Log("Not allocating memory for reset chunk Job - Terrain Parameters are not bigger");
                return;
            }
            else // The parameters are bigger so more memory needs to be allocated to hold all the data
            {
                Points.Dispose();
                Points = new NativeArray<Voxel>(numPossiblePoints, Allocator.Persistent);
                Vertices.Dispose();
                Vertices = new NativeArray<Vector3>(size * 15, Allocator.Persistent);
                Triangles.Dispose();
                Triangles = new NativeArray<int>(size * 15, Allocator.Persistent);

            }


        }

        public void Dispose()
        {
            NumberOfTriangles.Dispose();
            UpdateMainThread.Dispose();
            Vertices.Dispose();
            Triangles.Dispose();
            Points.Dispose();
            cell.Dispose();
            edgeConnections.Dispose();
            vertices.Dispose();
        }


        public void Execute()
        {
            float4 point = 0;
            point.x = ChunkCenter.x;
            point.y = ChunkCenter.y;
            point.z = ChunkCenter.z;
            GenerateScalarField(point);
            CreateCubeData();
            // CalulateMesh(VoxelCells);
            UpdateMainThread[0] = true;
        }

        static readonly ProfilerMarker s_CreateCubeDataMarker = new ProfilerMarker("CreateCubeData");
        private void CreateCubeData()
        {
            s_CreateCubeDataMarker.Begin();
            int levelOffset, rowOffset;

            int Length = MterrainParameters.SamplingLength;
            int Width = MterrainParameters.SamplingWidth;
            int Height = MterrainParameters.SamplingHeight;
            int Scale = MterrainParameters.Scale;
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

                        // create the edge list
                        VoxelCell.CreateEdgeList(out byte edgelist, MterrainParameters.ISO_Level, cell);
                        // check if the cell in on the surface
                        if (VoxelCell.IsOnSurface(edgelist))
                        {
                            VoxelCell.CreateVertexConnections(edgelist, out int numTri, ref edgeConnections);
                            VoxelCell.CalculateMesh(numTri, ref vertices, edgeConnections, cell);
                            for (int i = 0; i < numTri * 3; i++)
                            {

                                Vertices[index + i] = new Vector3(vertices[i].x, vertices[i].y, vertices[i].z);
                                Triangles[index + i] = index + i;

                            }
                            index += numTri * 3;
                        }
                    }

                }
            }
            NumberOfTriangles[0] = index / 3;
            // Debug.Log("Number of triangle in job centered at " + ChunkCenter + " : " + NumberOfTriangles[0]);
            s_CreateCubeDataMarker.End();
        }

        static readonly ProfilerMarker s_GenerateScalarFieldfMarker = new ProfilerMarker("GenerateScalarField");
        private void GenerateScalarField(float4 around)
        {
            s_GenerateScalarFieldfMarker.Begin();


            float x, y, z;

            int Length = MterrainParameters.SamplingLength;
            int Width = MterrainParameters.SamplingWidth;
            int Height = MterrainParameters.SamplingHeight;
            int Scale = MterrainParameters.Scale;
            float Scalef = (float)Scale;
            int bedrock = MterrainParameters.BedrockLevel;
            float4 pos;
            int index = 0;
            for (int level = 0; level <= Height * Scale; level++)
            {
                // levelOffset = level * ((Width * Scale + 1) * (Length * Scale + 1));

                for (int row = 0; row <= Length * Scale; row++)
                {
                    for (int column = 0; column <= Width * Scale; column++)
                    {

                        x = ChunkCenter.x - ((Width) / 2f) + (column / Scalef);
                        y = bedrock + (level / Scalef);
                        z = ChunkCenter.z - ((Length) / 2f) + (row / Scalef);

                        //pos =; ;// + new Vector3(x, y, z);
                        pos.x = x;
                        pos.y = y;
                        pos.z = z;
                        pos.w = 0;


                        float4 noise = Noise.GenerateNoise(pos, MnoiseParameters, MterrainParameters);
                        // if the noise produces air but is below sea level produce water instead
                        Voxel v = new(pos, noise);
                        Points[index++] = v;

                    }

                }
            }
            NumberOfTriangles[1] = index-1;

            s_GenerateScalarFieldfMarker.End();
        }

    }
}