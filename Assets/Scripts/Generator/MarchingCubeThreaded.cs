using StarterAssets;
using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;

public partial class MarchingCubeThreaded : MonoBehaviour
{

    NoiseParameters noiseParameters;
    TerrainParameters mTerrainParameters;

    List<Chunk> mChunkList;

    public int _ChunkRenderDistance;
    private Transform mPlayerLocation;


    class Chunk
    {

        public NativeArray<Voxel> Points;
        public NativeArray<Vector3> Vertices;
        public NativeArray<int> Triangles;
        public NativeArray<bool> UpdateMainThread;
        public NativeArray<int> NumberOfTriangles;
        public MeshFilter Filter;
        public MeshRenderer Renderer;
        public GameObject ChunkObject;

        public JobHandle Handle;
        public Vector3 ChunkOrigin;
        public double IJobID;
        public ChunkLoader Loader;


    }

    private int mNumberofChunks;

    // Start is called before the first frame update
    void Start()
    {
        _ChunkRenderDistance = _ChunkRenderDistance * 2 + 1;

        mNumberofChunks = _ChunkRenderDistance * _ChunkRenderDistance;
        Debug.Log("Instantitating chunk pool of size: " + mNumberofChunks);
        mChunkList = new List<Chunk>(mNumberofChunks);
        //mChunkLoaderList = new List<ChunkLoader>(mNumberofChunks);
        //mJobHandles = new List<JobHandle>(mNumberofChunks);



        //mCollider = gameObject.AddComponent<MeshCollider>();

        noiseParameters = GetComponent<NoiseManager>().Parameterize();
        Debug.Log(noiseParameters);
        mTerrainParameters = GetComponent<TerrainSettings>().Paramterize();
        mPlayerLocation = GetComponent<TerrainSettings>().GetPlayerTransform();

        Vector3 forward = mPlayerLocation.transform.forward;
        Vector3 playerLocation = mPlayerLocation.transform.position;
        Vector3 playerchunkorigin = GetChunkCenterFromLocation(playerLocation, mTerrainParameters);
        List<Vector3> currentChunkOrigins = InitialChunksFromPLayerSpawnChunk(playerchunkorigin, mNumberofChunks, mTerrainParameters);


        for (int i = 0; i < mNumberofChunks; i++)
        {



            //mChunkLoaderList.Add(new ChunkLoader(noiseParameters, terrainParameters));
            Chunk chunk = new Chunk();
            TerrainParameters terrainParameters = mTerrainParameters;
            terrainParameters.Origin = currentChunkOrigins[i];
            Debug.Log("Creating chunk at :" + terrainParameters.Origin);

            chunk.IJobID = i;
            chunk.Loader = new ChunkLoader(noiseParameters, terrainParameters, i);


            chunk.ChunkOrigin = terrainParameters.Origin;
            chunk.Vertices = chunk.Loader.Vertices;
            chunk.Triangles = chunk.Loader.Triangles;
            chunk.UpdateMainThread = chunk.Loader.UpdateMainThread;
            chunk.NumberOfTriangles = chunk.Loader.NumberOfTriangles;
            chunk.Points = chunk.Loader.Points;
            chunk.ChunkObject = new GameObject();
            chunk.ChunkObject.name = "Chunk #" + i;

            chunk.ChunkObject.transform.SetParent(this.transform);
            chunk.Filter = chunk.ChunkObject.AddComponent<MeshFilter>();
            chunk.Renderer = chunk.ChunkObject.AddComponent<MeshRenderer>();
            chunk.Filter.mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
            chunk.Renderer.material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            chunk.Renderer.material.SetFloat("_Cull", 0);



            mChunkList.Add(chunk);
            Debug.Log("Chunk " + chunk.ChunkObject);


        }


        // use the editor provided origin
        ScheduleChunks(mPlayerLocation);
    }

    private List<Vector3> InitialChunksFromPLayerSpawnChunk(Vector3 playerchunkorigin, int numberChunks, in TerrainParameters terrainParameters)
    {



        float bootomLeftX = playerchunkorigin.x - ((_ChunkRenderDistance - 1) / 2) * terrainParameters.SamplingWidth;
        float bootomLeftz = playerchunkorigin.y - ((_ChunkRenderDistance - 1) / 2) * terrainParameters.SamplingLength;

        List<Vector3> chunksOrigins = new List<Vector3>(numberChunks);


        for (int i = 0; i < _ChunkRenderDistance; i++)
        {


            for (int j = 0; j < _ChunkRenderDistance; j++)
            {
                Vector3 point = new Vector3();
                point.x = bootomLeftX + terrainParameters.SamplingWidth * j;
                point.z = bootomLeftz + terrainParameters.SamplingLength * i;
                chunksOrigins.Add(point);

            }


        }



        return chunksOrigins;
    }

    private Vector3 GetChunkCenterFromLocation(Vector3 playerLocation, TerrainParameters terrainParameters)
    {


        float m = terrainParameters.SamplingWidth;

        Vector3 center = new Vector3();
        center.x = NearestCommonMultiple(playerLocation.x, terrainParameters.SamplingWidth);
        center.z = NearestCommonMultiple(playerLocation.z, terrainParameters.SamplingLength);
        return center;
    }

    /// <summary>
    /// Get the nearest common multiple of the number m
    /// given a number n 
    /// </summary>
    /// <param name="n"></param>
    /// <param name="m"></param>
    /// <param name="p"></param>
    /// <returns>The nearest common multiple p</returns>
    private static float NearestCommonMultiple(float n, float m)
    {
        float f = MathF.Floor(n / m) * m;//floor(n, m);
        float c = Mathf.Ceil(n / m) * m;//ceil(n, m);
        float lower = n - f;
        float upper = c - n;
        float p;
        p = f;
        if (lower > upper)
        {

            p = c;

        }

        return p;
    }

    public void ScheduleChunks(Transform location)
    {


        for (int i = 0; i < mChunkList.Count; i++)
        {

            mChunkList[i].Handle = mChunkList[i].Loader.Schedule();
        }




    }
    public void LateUpdate()
    {
        for (int i = 0; i < mChunkList.Count; i++)
        {

            if (!mChunkList[i].Handle.IsCompleted)
                mChunkList[i].Handle.Complete();

        }



    }

    // Update is called once per frame
    void Update()
    {

        for (int i = 0; i < mChunkList.Count; i++)
        {

            if (mChunkList[i].Handle.IsCompleted)
            {
                mChunkList[i].Handle.Complete();
                if (!mChunkList[i].Loader.UpdateMainThread[0])
                    break;

                int index = (mChunkList[i].Loader.NumberOfTriangles[0] * 3);

                Vector3[] ver = mChunkList[i].Loader.Vertices.GetSubArray(0, index).ToArray();
                int[] ind = mChunkList[i].Loader.Triangles.GetSubArray(0, index).ToArray();

                mChunkList[i].Filter.mesh.Clear();
                mChunkList[i].Filter.mesh.vertices = ver;
                mChunkList[i].Filter.mesh.triangles = ind;
                mChunkList[i].Filter.mesh.RecalculateNormals();
                mChunkList[i].Filter.mesh.RecalculateBounds();
                //mCollider.sharedMesh = filter.mesh;

                var cl = mChunkList[i].Loader;
                var n = cl.UpdateMainThread;
                n[0] = false;
                cl.UpdateMainThread = n;
                mChunkList[i].Loader = cl;


            }



        }



    }
    public void OnDestroy()
    {
        for (int i = 0; i < mChunkList.Count; i++)
        {

            mChunkList[i].Vertices.Dispose();
            mChunkList[i].Triangles.Dispose();
            mChunkList[i].UpdateMainThread.Dispose();
            mChunkList[i].NumberOfTriangles.Dispose();
            mChunkList[i].Points.Dispose();
        }

    }
}
