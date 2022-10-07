using StarterAssets;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
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
    Queue<Chunk> mChunkQueue;
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

        // Setup components
        mNoiseParameters = GetComponent<NoiseSettings>().Parameterize();
        Debug.Log("Setting default parameters to :" + mNoiseParameters.ToString());
        mTerrainParameters = GetComponent<TerrainSettings>().Paramterize();
        mPlayerLocation = GetComponent<TerrainSettings>().GetPlayerTransform();

        // Setup Chunk list
        _ChunkRenderDistance = _ChunkRenderDistance * 2 + 1;
        mNumberofChunks = _ChunkRenderDistance * _ChunkRenderDistance;
        Debug.Log("Instantitating chunk pool of size: " + mNumberofChunks);
        mChunkList = new List<Chunk>(mNumberofChunks);
        mChunksInUse = new List<bool>(mNumberofChunks);

        for (int i = 0; i < mNumberofChunks; i++)
        {
            mChunksInUse.Add(false);
        }

        Vector3 forward = mPlayerLocation.transform.forward;
        Vector3 playerLocation = mPlayerLocation.transform.position;
        mCurrentChunkCenter = GetChunkCenterFromLocation(playerLocation);
        List<Vector3> currentChunkOrigins = GetChunksFromCenterLocation(mCurrentChunkCenter);


        for (int i = 0; i < mNumberofChunks; i++)
        {
            //mChunkLoaderList.Add(new ChunkLoader(noiseParameters, terrainParameters));
            Chunk chunk = SetupChunk(currentChunkOrigins[i], i);

            mChunkList.Add(chunk);
            //mChunkQueue.Enqueue(chunk);
            Debug.Log("Chunk " + chunk.ChunkObject);


        }


        // use the editor provided origin
        ScheduleChunks();
    }

    private Chunk SetupChunk(Vector3 currentChunkOrigins, int i)
    {
        Chunk chunk = new Chunk();
        TerrainParameters terrainParameters = mTerrainParameters;
        terrainParameters.Origin = currentChunkOrigins;
        Debug.Log("Creating chunk at :" + terrainParameters.Origin);



        chunk.IJobID = ChunkIDFromLocation(terrainParameters.Origin);

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
        return chunk;
    }

    private Vector2 ChunkIDFromLocation(Vector3 origin)
    {
        // Xid and Zid are multiple of the sampling height sincee they are the centers
        // of each chunk
        float chunkXid = origin.x / mTerrainParameters.SamplingWidth;
        float chunkZid = origin.z / mTerrainParameters.SamplingWidth;
        return new Vector2(chunkXid, chunkZid);
    }

    private List<Vector3> GetChunksFromCenterLocation(in Vector3 playerchunkorigin)
    {


        int numberChunks = mNumberofChunks,
            length = mTerrainParameters.SamplingLength,
            width = mTerrainParameters.SamplingWidth;


        float bootomLeftX = playerchunkorigin.x - ((_ChunkRenderDistance - 1) / 2) * width;
        float bootomLeftz = playerchunkorigin.z - ((_ChunkRenderDistance - 1) / 2) * length;

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
    private Vector3 GetChunkCenterFromLocation(in Vector3 playerLocation)
    {

        int length = mTerrainParameters.SamplingLength,
            width = mTerrainParameters.SamplingWidth;

        Vector3 center = new Vector3();
        center.x = NearestCommonMultiple(playerLocation.x, width);
        center.y = playerLocation.y;
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

        Vector3 snapped = GetChunkCenterFromLocation(mPlayerLocation.position);

        if (mCurrentChunkCenter != snapped)
        {
            Vector3 oldCenter = mCurrentChunkCenter;
            Debug.Log("Player center chunk change from :" + mCurrentChunkCenter + " to :" + snapped);
            mCurrentChunkCenter = snapped;
            // find a new job to create a chunk with from the job pool
            ReloadEdgeChunksFromCenter(oldCenter);

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
                    continue;

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

    private void ReloadEdgeChunksFromCenter(in Vector3 oldCenter)
    {
        Debug.Log("Oldcenter: " + oldCenter + " -> CurrentCenter: " + mCurrentChunkCenter);

        List<Vector3> newChunksCenters = GetChunksFromCenterLocation(mCurrentChunkCenter);
        List<Vector3> oldChunkCenters = GetChunksFromCenterLocation(oldCenter);

        IEnumerable<Vector3> evicDiff = oldChunkCenters.Except(newChunksCenters);
        IEnumerable<Vector3> newDiff = newChunksCenters.Except(oldChunkCenters);

        Debug.Assert(evicDiff.ToList().Count == newDiff.ToList().Count);

        Queue<Vector3> q = new Queue<Vector3>(newDiff);

        List<Vector3> evictionCenters = evicDiff.ToList();
        List<Vector2> evictionIDs = new List<Vector2>();
        List<int> reuseIndices = new List<int>();


        for (int i = 0; i < mChunkList.Count; i++)
        {
            for (int j = 0; j < evictionCenters.Count; j++)
            {
                if (mChunkList[i].ChunkOrigin == evictionCenters[j]) {
                    mChunkList[i].ChunkOrigin = q.Dequeue();
                    reuseIndices.Add(i);
                
                }

            }
        }

        foreach (var i in reuseIndices)
        {

            // Schedule the new chunks to update

            var tp = mTerrainParameters;
            tp.Origin = mChunkList[i].ChunkOrigin;
            mChunkList[i].Loader.ReInitialize(mNoiseParameters, tp, new Vector2());
            mChunkList[i].Vertices = mChunkList[i].Loader.Vertices;
            mChunkList[i].Triangles = mChunkList[i].Loader.Triangles;
            mChunkList[i].UpdateMainThread = mChunkList[i].Loader.UpdateMainThread;
            mChunkList[i].NumberOfTriangles = mChunkList[i].Loader.NumberOfTriangles;
            mChunkList[i].Points = mChunkList[i].Loader.Points;

            mChunkList[i].Handle = mChunkList[i].Loader.Schedule();
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
