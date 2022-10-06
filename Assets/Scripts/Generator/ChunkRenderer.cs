using StarterAssets;
using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;

[RequireComponent(typeof(TerrainSettings))]
[RequireComponent(typeof(NoiseSettings))]
public partial class ChunkRenderer : MonoBehaviour
{

    NoiseParameters mNoiseParameters;
    TerrainParameters mTerrainParameters;

    List<Chunk> mChunkList;
    List<bool> mChunksInUse;

    public int _ChunkRenderDistance;
    private Transform mPlayerLocation;
    private Vector3 mCurrentChunkCenter;


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
        public Vector2 IJobID;
        public TerrainLoader Loader;


    }

    private int mNumberofChunks;

    // Start is called before the first frame update
    void Start()
    {
        _ChunkRenderDistance = _ChunkRenderDistance * 2 + 1;

        mNumberofChunks = _ChunkRenderDistance * _ChunkRenderDistance;
        Debug.Log("Instantitating chunk pool of size: " + mNumberofChunks);
        mChunkList = new List<Chunk>(mNumberofChunks);
        mChunksInUse = new List<bool>(mNumberofChunks);

        // Init each list member
        for (int i = 0; i < mNumberofChunks; i++)
            mChunksInUse.Add(false);
        //mChunkLoaderList = new List<ChunkLoader>(mNumberofChunks);
        //mJobHandles = new List<JobHandle>(mNumberofChunks);



        //mCollider = gameObject.AddComponent<MeshCollider>();

        mNoiseParameters = GetComponent<NoiseSettings>().Parameterize();
        Debug.Log("Setting default parameters to :" + mNoiseParameters.ToString());
        mTerrainParameters = GetComponent<TerrainSettings>().Paramterize();
        mPlayerLocation = GetComponent<TerrainSettings>().GetPlayerTransform();

        Vector3 forward = mPlayerLocation.transform.forward;
        Vector3 playerLocation = mPlayerLocation.transform.position;
        mCurrentChunkCenter = GetChunkCenterFromLocation(playerLocation, mTerrainParameters.SamplingLength, mTerrainParameters.SamplingWidth);
        List<Vector3> currentChunkOrigins = ChunksFromCenterLocation(mCurrentChunkCenter, mNumberofChunks, mTerrainParameters.SamplingLength, mTerrainParameters.SamplingWidth);


        for (int i = 0; i < mNumberofChunks; i++)
        {



            //mChunkLoaderList.Add(new ChunkLoader(noiseParameters, terrainParameters));
            Chunk chunk = new Chunk();
            TerrainParameters terrainParameters = mTerrainParameters;
            terrainParameters.Origin = currentChunkOrigins[i];
            Debug.Log("Creating chunk at :" + terrainParameters.Origin);

            // Xid and Zid are multiple of the sampling height sincee they are the centers
            // of each chunk
            float chunkXid = terrainParameters.Origin.x / terrainParameters.SamplingWidth;
            float chunkZid = terrainParameters.Origin.z / terrainParameters.SamplingWidth;
            chunk.IJobID = new Vector2(chunkXid, chunkZid);
            Debug.Log("Chunk location ID: " + chunk.IJobID);
            chunk.Loader = new TerrainLoader(mNoiseParameters, terrainParameters, chunk.IJobID);


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
        ScheduleChunks();
    }

    private List<Vector3> ChunksFromCenterLocation(in Vector3 playerchunkorigin, in int numberChunks, in int length, in int width)
    {



        float bootomLeftX = playerchunkorigin.x - ((_ChunkRenderDistance - 1) / 2) * width;
        float bootomLeftz = playerchunkorigin.y - ((_ChunkRenderDistance - 1) / 2) * length;

        List<Vector3> chunksOrigins = new List<Vector3>(numberChunks);


        for (int i = 0; i < _ChunkRenderDistance; i++)
        {


            for (int j = 0; j < _ChunkRenderDistance; j++)
            {
                Vector3 point = new Vector3();
                point.x = bootomLeftX + width * j;
                point.z = bootomLeftz + length * i;
                chunksOrigins.Add(point);

            }


        }



        return chunksOrigins;
    }

    /// <summary>
    /// Gets the center of the chunk that the current playerLocation is in.
    /// </summary>
    /// <param name="playerLocation">The current positon of the player</param>
    /// <param name="length">Unit length of the chunk length(Z-Axis)</param>
    /// <param name="width">Unit length of the chunk width(X-Axis)</param>
    /// <returns></returns>
    private Vector3 GetChunkCenterFromLocation(in Vector3 playerLocation, in int length, in int width)
    {



        Vector3 center = new Vector3();
        center.x = NearestCommonMultiple(playerLocation.x, width);
        center.z = NearestCommonMultiple(playerLocation.z, length);
        return center;
    }

    /// <summary>
    /// Get the nearest common multiple of the number m
    /// given a number n.
    /// </summary>
    /// <param name="n">The number to find the nearest multiple of.</param>
    /// <param name="m">The multiple to use.</param>
    /// <returns>The nearest common multiple.</returns>
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

    private void ScheduleChunks()
    {


        for (int i = 0; i < mChunkList.Count; i++)
        {
            mChunksInUse[i] = true;
            mChunkList[i].Handle = mChunkList[i].Loader.Schedule();
        }



    }
    public void LateUpdate()
    {
        //for (int i = 0; i < mChunkList.Count; i++)
        //{

        //    if (!mChunkList[i].Handle.IsCompleted)
        //        mChunkList[i].Handle.Complete();

        //}

    }

    // Update is called once per frame
    void Update()
    {

        Vector3 snapped = GetChunkCenterFromLocation(mPlayerLocation.position, mTerrainParameters.SamplingLength, mTerrainParameters.SamplingWidth);

        if (mCurrentChunkCenter != snapped)
        {
            Debug.Log("Player center chunk change from :" + mCurrentChunkCenter + " to :" + snapped);
            mCurrentChunkCenter = snapped;
            // find a new job to create a chunk with from the job pool
            ReloadEdgeChunks();

            // schedule the job and resave the given handle
        }

        for (int i = 0; i < mChunkList.Count; i++)
        {
            // Dont attempt to complete the chunk unless the job is done
            
            if (mChunkList[i].Handle.IsCompleted)
            {
                //Debug.Break();
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
                mChunksInUse[i] = false;


            }



        }



    }

    private void ReloadEdgeChunks()
    {


        //int unusedJob = -1;
        //for (int i = 0; i < mNumberofChunks; i++)
        //{

        //    if (!mChunksInUse[i])
        //    {
        //        unusedJob = i;
        //        break;
        //    }

        //}
        //// once a job is found, reinitialize it 
        //if (unusedJob != -1)
        //{
        //    TerrainParameters parameters = mTerrainParameters;
        //    parameters.Origin = mCurrentChunkCenter
        //        float chunkXid = terrainParameters.Origin.x / mTerrainParameters.SamplingWidth;
        //    float chunkZid = terrainParameters.Origin.z / mTerrainParameters.SamplingWidth;
        //    mChunkList[unusedJob].Loader.ReInitialize(mTerrainParameters, mTerrainParameters, new Vector2(chunkXid, chunkZid));

        //}

        throw new NotImplementedException();
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
