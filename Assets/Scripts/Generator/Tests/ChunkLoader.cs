using System;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;

namespace Assets.Scripts.SIMD
{

    class ChunkLoader
    {

        public NativeArray<Voxel> Points;
        public NativeArray<Vector3> Vertices;
        public NativeArray<int> Triangles;
        public NativeArray<bool> UpdateMainThread;
        public NativeArray<int> NumberOfTriangles;

        public SimdChunkJob Job;
        public JobHandle Handle;
        public int LoaderId;

        public ChunkLoader(NoiseParameters noiseParameters, TerrainParameters terrainParameters, Vector3 center, int loaderId)
        {

            LoaderId = loaderId;
            Job = new SimdChunkJob(noiseParameters, terrainParameters, center);
            Vertices = Job.Vertices;
            Triangles = Job.Triangles;
            UpdateMainThread = Job.UpdateMainThread;
            NumberOfTriangles = Job.NumberOfTriangles;
            Points = Job.Points;

        }

        public void ResetArrays() {

            Vertices = Job.Vertices;
            Triangles = Job.Triangles;
            UpdateMainThread = Job.UpdateMainThread;
            NumberOfTriangles = Job.NumberOfTriangles;
            Points = Job.Points;

        }
        public void ReleaseLoader()
        {
            Job.Dispose();
    
        }
        public JobHandle Schedule()
        {

            Handle = Job.Schedule();
            return Handle;

        }
        public void Complete()
        {

            Handle.Complete();
        }




    }

}