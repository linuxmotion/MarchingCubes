using StarterAssets;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;
using UnityEngine.Pool;

[RequireComponent(typeof(TerrainSettings))]
[RequireComponent(typeof(NoiseSettings))]
public partial class ChunkRenderer : MonoBehaviour
{

    TerrainSettings mTerrainSettings;
    NoiseSettings mNoiseSettings;
    TerrainParameters mTerrainParameters;
    NoiseParameters mNoiseParameters;


    List<Chunk> mChunkList;
    List<bool> mChunksInUse;
    // Queue<Chunk> mChunkQueue;
    //ObjectPool<TerrainLoader> mLoaderPool;
    public int _ChunkRenderDistance;
    private int mChunkRenderDistance;
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
        public ChunkJob Loader;

        public void ReleaseChunk()
        {


            Points.Dispose();
            Vertices.Dispose();
            Triangles.Dispose();
            UpdateMainThread.Dispose();
            NumberOfTriangles.Dispose();
            Filter = null;
            Renderer = null;
            Destroy(ChunkObject);

        }


    }

    private int mNumberofChunks;

    // Start is called before the first frame update
    void OnEnable()
    {

        // Setup components
        mNoiseSettings = GetComponent<NoiseSettings>();
        mTerrainSettings = GetComponent<TerrainSettings>();
        mNoiseParameters = mNoiseSettings.Parameterize();
        mTerrainParameters = mTerrainSettings.Parameterize();

        Debug.Log("Setting initial noise parameters to :" + mNoiseParameters.ToString());
        Debug.Log("Setting initial terrain parameters to :" + mTerrainParameters.ToString());

        mPlayerLocation = GetComponent<TerrainSettings>().GetPlayerTransform();

        // Setup Chunk list
        mChunkRenderDistance = _ChunkRenderDistance * 2 + 1;
        SetupChunkList(mChunkRenderDistance*mChunkRenderDistance);


        // use the editor provided origin
        ScheduleChunks();
    }
    [SerializeField]
    GameObject _ScaleCubePrefab;
    public void Start()
    {
        if (_ScaleCubePrefab == null)
        {
            Debug.Log("No scale prefab set, not render cube scale");
                return;
        }
        Transform parent = GameObject.FindGameObjectWithTag("TerrainScale").transform;
        for (int i = 0; i < mTerrainParameters.SamplingHeight; i++) {

            GameObject g = GameObject.Instantiate(_ScaleCubePrefab);
            g.transform.SetParent(parent);
            MeshRenderer rend = g.GetComponent<MeshRenderer>();
                rend.material.mainTexture = null;
            if (i % 2 == 0) rend.material.color = Color.white;
            else rend.material.color = Color.black;
            g.transform.SetPositionAndRotation(new Vector3(0, mTerrainParameters.BedrockLevel + i, 0), new Quaternion());
            
        
        }

    }

    private void SetupChunkList(int numChunks)
    {
        mNumberofChunks = numChunks;
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
    }

    private Chunk SetupChunk(Vector3 currentChunkOrigins, int i)
    {
        Chunk chunk = new Chunk();
        TerrainParameters terrainParameters = mTerrainParameters;
        terrainParameters.Origin = currentChunkOrigins;
        Debug.Log("Creating chunk at :" + terrainParameters.Origin);



        chunk.IJobID = ChunkIDFromLocation(terrainParameters.Origin);


        Debug.Log("Chunk location ID: " + chunk.IJobID);
        chunk.Loader = new ChunkJob(mNoiseParameters, terrainParameters, chunk.IJobID);

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


        float bootomLeftX = playerchunkorigin.x - ((mChunkRenderDistance - 1) / 2) * width;
        float bootomLeftz = playerchunkorigin.z - ((mChunkRenderDistance - 1) / 2) * length;

        List<Vector3> chunksOrigins = new List<Vector3>(numberChunks);


        for (int i = 0; i < mChunkRenderDistance; i++)
        {
            for (int j = 0; j < mChunkRenderDistance; j++)
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
        CheckForAndApplyEditorChanges();

    }

    private void CheckForAndApplyEditorChanges()
    {
        // check to see if the noise settings have change
        // only need to check one since all chunks have the same settings
        bool update = false;
        NoiseParameters noiseParameters1 = mNoiseSettings.Parameterize();
        TerrainParameters terrainParameters = mTerrainSettings.Parameterize();
        // Check the editor values against the default initialized values
        // if its different save the new valuesa as the default apply the changes
        if (noiseParameters1 != mNoiseParameters)
        {
            Debug.Log("Noise values in editor changed from : " + mNoiseParameters + " to: " + noiseParameters1);
            mNoiseParameters = mNoiseSettings.Parameterize();
            update = true;
        }
        if (!terrainParameters.EqualsExecptOrigin(mTerrainParameters))
        {
            Debug.Log("Terrain values in editor changed from : " + mTerrainParameters + " to: " + terrainParameters);
            mTerrainParameters = mTerrainSettings.Parameterize();
            update = true;
        }
        int rd = (_ChunkRenderDistance * 2 + 1);
        if (mChunkRenderDistance != rd  ){

            Debug.Log("Chunk render distance change from: " + mChunkRenderDistance +" to: " + _ChunkRenderDistance);
            mChunkRenderDistance = rd;
            OnDisable();
            SetupChunkList(mChunkRenderDistance * mChunkRenderDistance);
            ScheduleChunks();
            return;
          

        }

        if (update) ApplyEditorChangesToTerrain();
    }

    public void ApplyEditorChangesToTerrain()
    {

        // reinit the whole chunk list
        // with the new settings since the chunks already exist
        for (int i = 0; i < mChunkList.Count; i++)
        {

            var tp = mTerrainParameters;
            tp.Origin = mChunkList[i].ChunkOrigin;
            mChunkList[i].Loader.ReInitialize(mNoiseParameters, tp, new Vector2());
            mChunkList[i].Vertices = mChunkList[i].Loader.Vertices;
            mChunkList[i].Triangles = mChunkList[i].Loader.Triangles;
            mChunkList[i].UpdateMainThread = mChunkList[i].Loader.UpdateMainThread;
            mChunkList[i].NumberOfTriangles = mChunkList[i].Loader.NumberOfTriangles;
            mChunkList[i].Points = mChunkList[i].Loader.Points;

        }
        // Finally schedule the chunks to update
        ScheduleChunks();




    }



    // Update is called once per frame
    void Update()
    {
        CheckForNewChunkCenter();
        CheckAndCompleteCompletedJobs();

    }

    private void CheckAndCompleteCompletedJobs()
    {
        for (int i = 0; i < mChunkList.Count; i++)
        {
            // Dont attempt to complete the chunk unless the job is done

            if (mChunkList[i].Handle.IsCompleted)
            {
                //Debug.Break();
                mChunkList[i].Handle.Complete();
                if (!mChunkList[i].Loader.UpdateMainThread[0])
                    continue;
                AssignChunkDataFromJob(i);

            }



        }
    }

    private void AssignChunkDataFromJob(int i)
    {
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

    private void CheckForNewChunkCenter()
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
                if (mChunkList[i].ChunkOrigin == evictionCenters[j])
                {
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
    public void OnDisable()
    {

        // Called when the component is disable or when a hot reload happens
        for (int i = 0; i < mChunkList.Count; i++)
        {
            // release all chunks
            mChunkList[i].ReleaseChunk();

        }
        // clear the list and delete
        mChunkList.Clear();
        mChunkList = null;



    }
    public void OnDestroy()
    {


    }
}
