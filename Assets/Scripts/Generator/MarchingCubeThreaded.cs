using StarterAssets;
using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;

public partial class MarchingCubeThreaded : MonoBehaviour
{

    MeshFilter filter;
    List<ChunkLoader> mChunkLoaderList;
    NoiseParameters noiseParameters;
    TerrainParameters terrainParameters;
    MeshCollider mCollider;

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


    }



    // Start is called before the first frame update
    void Start()
    {

        mChunkList = new List<Chunk>(_ChunkRenderDistance*_ChunkRenderDistance);
        mChunkLoaderList = new List<ChunkLoader>(_ChunkRenderDistance*_ChunkRenderDistance);

        MeshRenderer meshRenderer = gameObject.AddComponent<MeshRenderer>();
        meshRenderer.material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        meshRenderer.material.SetFloat("_Cull", 0);
        filter = gameObject.AddComponent<MeshFilter>();
        filter.mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
        mCollider = gameObject.AddComponent<MeshCollider>();

        noiseParameters = GetComponent<NoiseManager>().Parameterize();
        terrainParameters = GetComponent<TerrainSettings>().Paramterize();
        
        mPlayerLocation = GetComponent<TerrainSettings>().GetPlayerTransform();


        for (int i = 0; i < _ChunkRenderDistance*_ChunkRenderDistance; i++)
        {
            int k = 0;
            mChunkLoaderList.Add(new ChunkLoader(noiseParameters, terrainParameters));
            mChunkList.Add(new Chunk());

            mChunkList[i].Vertices = mChunkLoaderList[i].Vertices;
            mChunkList[i].Triangles = mChunkLoaderList[i].Triangles;
            mChunkList[i].UpdateMainThread = mChunkLoaderList[i].UpdateMainThread;
            mChunkList[i].NumberOfTriangles = mChunkLoaderList[i].NumberOfTriangles;
            mChunkList[i].Points = mChunkLoaderList[i].Points;


        }


        // use the editor provided origin
        UpdateChunks(mPlayerLocation);
    }

    public void UpdateChunks(Transform location)
    {


        // Vector3 around = Vector3.zero;// GetChunkFromXZ(chunkx, chunkz);

        // Get the direction that the player is facing

        // set the center of the chunk to render

        // 

        Vector3 forward = location.transform.forward;
        Vector3 playerLocation = location.transform.position;
        

        // n x n grid of chunks
        for (int i = 0; i < _ChunkRenderDistance; i++)
        {
            for (int j = 0; j < _ChunkRenderDistance; i++) { 
            
            
            
            
            
            }


            noiseParameters = GetComponent<NoiseManager>().Parameterize();
            terrainParameters = GetComponent<TerrainSettings>().Paramterize();

           

            ChunkLoader loader = mChunkLoaderList[i];
            loader.terrainParameters = terrainParameters;
            loader.noiseParameters = noiseParameters;
            terrainParameters.Origin = playerLocation;

            mChunkLoaderList[i] = loader;
            JobHandle handle = mChunkLoaderList[i].Schedule();
            handle.Complete();



        }




    }

    // Update is called once per frame
    void Update()
    {

        for (int i = 0; i < _ChunkRenderDistance; i++)
        {

            if (mChunkLoaderList[i].UpdateMainThread[0])
            {
                int index = (mChunkLoaderList[i].NumberOfTriangles[0] * 3);

                Vector3[] ver = mChunkLoaderList[i].Vertices.GetSubArray(0, index).ToArray();
                int[] ind = mChunkLoaderList[i].Triangles.GetSubArray(0, index).ToArray();

                filter.mesh.Clear();
                filter.mesh.vertices = ver;
                filter.mesh.triangles = ind;
                filter.mesh.RecalculateNormals();
                filter.mesh.RecalculateBounds();
                mCollider.sharedMesh = filter.mesh;

                var cl =  mChunkLoaderList[i];
                var n = cl.UpdateMainThread;
                n[0] = false;
                cl.UpdateMainThread = n;
                mChunkLoaderList[i] = cl;


            }



        }



    }
    public void OnDestroy()
    {
        for (int i = 0; i < _ChunkRenderDistance; i++)
        {

            mChunkList[i].Vertices.Dispose();
            mChunkList[i].Triangles.Dispose();
            mChunkList[i].UpdateMainThread.Dispose();
            mChunkList[i].NumberOfTriangles.Dispose();
            mChunkList[i].Points.Dispose();
        }

    }
}
