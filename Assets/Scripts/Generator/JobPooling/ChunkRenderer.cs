using Assets.Scripts.SIMD;
using StarterAssets;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Jobs;
using UnityEngine;
using UnityEngine.Pool;

namespace Assets.Scripts.SIMD
{
    [RequireComponent(typeof(TerrainSettings))]
    [RequireComponent(typeof(NoiseSettings))]
    public class ChunkRenderer : MonoBehaviour
    {

        TerrainSettings mTerrainSettings;
        NoiseSettings mNoiseSettings;

        public bool _UseSmoothNormals;
        private bool UseSmoothNormals;


        ChunkLoaderPool mLoaderPool;

        public int _ChunkRenderDistance;
        private int mSizeOfChunkSide;
        private Transform mPlayerLocation;
        private Vector3 mCurrentChunkCenter;

        private int mNumberofChunks;

        // Start is called before the first frame update
        void OnEnable()
        {

            UseSmoothNormals = _UseSmoothNormals;
            // Setup components
            mNoiseSettings = GetComponent<NoiseSettings>();
            // mNoiseParameters = mNoiseSettings.Parameterize();

            mTerrainSettings = GetComponent<TerrainSettings>();
            //mTerrainParameters = mTerrainSettings.Parameterize();

            mPlayerLocation = GetComponent<TerrainSettings>().GetPlayerTransform();

            // Setup Chunk list
            mSizeOfChunkSide = _ChunkRenderDistance * 2 + 1;

            int numberOFChunks = mSizeOfChunkSide * mSizeOfChunkSide;
            List<Chunk> chunks = SetupChunkList(numberOFChunks, mTerrainSettings.Parameterize());

            mLoaderPool = new ChunkLoaderPool(ref chunks,
                Unity.Jobs.LowLevel.Unsafe.JobsUtility.JobWorkerCount,
                mSizeOfChunkSide,
                mPlayerLocation.transform.position,
                mNoiseSettings.Parameterize(),
                mTerrainSettings.Parameterize());

            mLoaderPool.DispatchUnsafeQueue();

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
            int bedrock = mTerrainSettings.Parameterize().BedrockLevel;
            for (int i = 0; i < mTerrainSettings.Parameterize().SamplingHeight; i++)
            {

                GameObject g = GameObject.Instantiate(_ScaleCubePrefab);
                g.transform.SetParent(parent);
                MeshRenderer rend = g.GetComponent<MeshRenderer>();
                rend.material.mainTexture = null;
                if (i % 2 == 0) rend.material.color = Color.white;
                else rend.material.color = Color.black;
                g.transform.SetPositionAndRotation(new Vector3(0, bedrock + i, 0), new Quaternion());


            }

        }

        private List<Chunk> SetupChunkList(in int numChunks, in TerrainParameters terrainParameters)
        {
            mNumberofChunks = numChunks;
            Debug.Log("Instantitating chunk list of size: " + mNumberofChunks);
            List<Chunk> chunkList = new List<Chunk>(mNumberofChunks);


            Vector3 playerLocation = mPlayerLocation.transform.position;

            mCurrentChunkCenter = Chunk.GetChunkCenterFromLocation(playerLocation, terrainParameters);
            List<Vector3> currentChunkOrigins = Chunk.GetChunksFromCenterLocation(mCurrentChunkCenter, numChunks, mSizeOfChunkSide, terrainParameters);


            for (int i = 0; i < mNumberofChunks; i++)
            {
                Chunk chunk = SetupChunk(currentChunkOrigins[i], i);
                chunkList.Add(chunk);
                Debug.Log("Chunk " + chunk.ChunkObject);
            }


            return chunkList;

        }

        private Chunk SetupChunk(in Vector3 chunkOrigin, int chunkNumber)
        {
            Chunk chunk = new Chunk(chunkNumber);
            // Debug.Log("Creating chunk at :" + chunkOrigin);
            chunk.ChunkOrigin = chunkOrigin;
            // Debug.Log(chunk + "Chunk ID: " + chunk.ChunkID +" | Location: " + chunk.ChunkOrigin);
            chunk.ChunkObject = new GameObject();
            chunk.ChunkObject.name = "Chunk #" + chunkNumber;
            chunk.ChunkObject.transform.SetPositionAndRotation(chunk.ChunkOrigin, new Quaternion(0, 0, 0, 0));

            chunk.ChunkObject.transform.SetParent(this.transform);
            chunk.Filter = chunk.ChunkObject.AddComponent<MeshFilter>();
            chunk.Renderer = chunk.ChunkObject.AddComponent<MeshRenderer>();
            chunk.Filter.mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
            chunk.Renderer.material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            chunk.Renderer.material.SetFloat("_Cull", 0);
            return chunk;
        }

        public void LateUpdate()
        {


            mLoaderPool.CreateChunkQueue(mPlayerLocation.transform.position);
            mLoaderPool.SmoothNormals = _UseSmoothNormals;
            mLoaderPool.DispatchQueue();
            mLoaderPool.ReceiveDispatch();


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
            if (noiseParameters1 != mLoaderPool.NoiseParams)
            {
                Debug.Log("Noise values in editor changed from : " + mLoaderPool.NoiseParams + " to: " + noiseParameters1);
                update = true;
            }
            if (!terrainParameters.EqualsExecptOrigin(mLoaderPool.TerrainParams))
            {
                Debug.Log("Terrain values in editor changed from : " + mLoaderPool.TerrainParams + " to: " + terrainParameters);
                update = true;
            }
            int rd = (_ChunkRenderDistance * 2 + 1);
            if (mSizeOfChunkSide != rd)
            {

                Debug.Log("Chunk render distance change from: " + mSizeOfChunkSide + " to: " + rd);
                mSizeOfChunkSide = rd;
                int size = rd * rd - mNumberofChunks;
                for (int i = 0; i < size; i++)
                {
                    Chunk chunk = SetupChunk(new Vector3(i, 1, 1), mLoaderPool.NumberOfChunks + i);
                    mLoaderPool.AddChunkToList(ref chunk);
                    Debug.Log("Chunk " + chunk.ChunkObject);

                }

                update = true;

            }

            if (UseSmoothNormals != _UseSmoothNormals) {

                update = true;
                UseSmoothNormals = _UseSmoothNormals;
            }

            if (update)
            {
                mLoaderPool.ResetLoaderPoolParameters(rd, noiseParameters1, terrainParameters);
                mLoaderPool.ApplyChangesAfterReset();
                //mLoaderPool.DispatchQueue();
            }
        }
        // Update is called once per frame
        void Update()
        {

        }
        public void OnDisable()
        {
            mLoaderPool.ReleasePool();
        }
        public void OnDestroy()
        {


        }
    }
}