using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Assets.Scripts.SIMD
{
    class ChunkLoaderPool
    {

        private List<ChunkLoader> LoaderPool;
        private List<Chunk> ChunkPool;

        private Queue<int> LoaderPoolInUse;
        private Queue<int> LoaderPoolAvailable;


        private Queue<int> ChunkPoolAvailable;


        private Queue<Vector3> ChunkOriginQueue;

        public NoiseParameters mNoiseParameters { get; private set; }
        public TerrainParameters mTerrainParameters { get; private set; }
        public int ChunkRenderDistance { get; private set; }
        public Vector3 mCurrentChunkOrigin { get; private set; }
        public readonly int NumberOfChunks;
        readonly private int MaxSize;



        public ChunkLoaderPool(ref List<Chunk> chunks, int numJobs, int renderDistance, Vector3 initialLocation, NoiseParameters noiseParameters, TerrainParameters terrainParameters)
        {

            Debug.Log("Creating chunk loader pool");
            Debug.Log("Number of chunks to pool:" + chunks.Count);
            Debug.Log("Number of Jobs to pool: " + numJobs);
            Debug.Log("Setting initial noise parameters to: " + noiseParameters.ToString());
            Debug.Log("Setting initial terrain parameters to: " + terrainParameters.ToString());
            ChunkRenderDistance = renderDistance;

            MaxSize = numJobs * 2;

            mCurrentChunkOrigin = initialLocation;
            mNoiseParameters = noiseParameters;
            mTerrainParameters = terrainParameters;
            NumberOfChunks = chunks.Count;
            InitializeChunkPool(ref chunks);
            IntializeLoaderPool();
        }

        private void InitializeChunkPool(ref List<Chunk> chunks)
        {

            ChunkPool = chunks;
            ChunkPoolAvailable = new Queue<int>(NumberOfChunks);
            //The total number of chunks in the world
            ChunkOriginQueue = new Queue<Vector3>(NumberOfChunks);

            for (int i = 0; i < NumberOfChunks; i++)
            {
                Vector3 origin = ChunkPool[i].ChunkOrigin;
                ChunkOriginQueue.Enqueue(origin);
                ChunkPoolAvailable.Enqueue(i);
            }
            // Number of threads available to the program

        }

        public void IntializeLoaderPool()
        {
            // Number of threads available to the program


            LoaderPool = new List<ChunkLoader>(MaxSize);
            LoaderPoolInUse = new Queue<int>(MaxSize);
            LoaderPoolAvailable = new Queue<int>(MaxSize);



            for (int loaderNum = 0; loaderNum < MaxSize; loaderNum++)
            {
                LoaderPool.Add(new ChunkLoader(mNoiseParameters, mTerrainParameters, Vector3.positiveInfinity, loaderNum));
                LoaderPoolAvailable.Enqueue(loaderNum);
            }


        }

       

        /// <summary>
        /// Check for current running Job. If there are some Jobs that are currenbtly running
        /// and have not had there data read into a chunk, assign that data into a free chunk
        /// </summary>
        /// <returns>
        /// False if there are no Job to check for or the chunk pool is empty,
        /// True if at least one Job was available for data retrieval
        /// </returns>
        public bool ReceiveDispatch()
        {
            if (LoaderPoolInUse.Count == 0)
                return false;


            // Scan the whole loader list to see if there is a loader in use

            // maybe there is a better structure to hold this in,
            // maybe another queue that holds the currently in use loader

            bool dispatchReceived = false;
            for (int i = 0, inUseCount = LoaderPoolInUse.Count; i < inUseCount; i++)
            {

                // get some loader that was in use 
                int loaderId = LoaderPoolInUse.Peek();

                // if the loader was being used a job was issued on it

                //we have completed a Job
                if (LoaderPool[loaderId].Handle.IsCompleted)
                {

                    LoaderPool[loaderId].Handle.Complete();
                    // Can now acces all the data from the job
                    // and assign it to a free chunks

                    int chunkNumber;
                    //ChunkPoolInUse.Enqueue(chunkNumber);
                    // int chunkNumber = freeChunks.Dequeue();
                    if (ChunkPoolAvailable.TryDequeue(out chunkNumber))
                    {

                        AssignChunkDataFromJob(loaderId, chunkNumber);
                        LoaderPoolInUse.Dequeue();
                        LoaderPoolAvailable.Enqueue(loaderId);
                        dispatchReceived = true;
                    }
                    else
                    {
                        Debug.Log("The chunk pool is empty, could not assign a chunk from the loader");
                        return false;
                    }

                }

            }
       
            return dispatchReceived;
        }


        /// <summary>
        /// Assign Data from a Job into a chunk for rendering
        /// </summary>
        /// <param name="loaderId">The Job id to retrieve the data from</param>
        /// <param name="chunkNum">The Chunk that the data should be assigned to</param>
        private void AssignChunkDataFromJob(int loaderId, int chunkNum)
        {
            int index = LoaderPool[loaderId].NumberOfTriangles[0] * 3;

            Vector3[] ver = LoaderPool[loaderId].Vertices.GetSubArray(0, index).ToArray();

            var localVert = new Vector3[ver.Length];

            ChunkPool[chunkNum].ChunkOrigin = LoaderPool[loaderId].Job.ChunkCenter;
            ChunkPool[chunkNum].ChunkObject.transform.position = ChunkPool[chunkNum].ChunkOrigin;

            for (int j = 0; j < ver.Length; j++)
            {
                localVert[j] = ChunkPool[chunkNum].ChunkObject.transform.InverseTransformPoint(ver[j]);
            }
            int[] ind = LoaderPool[loaderId].Triangles.GetSubArray(0, index).ToArray();

            ChunkPool[chunkNum].Filter.mesh.Clear();
            ChunkPool[chunkNum].Filter.mesh.vertices = localVert;
            ChunkPool[chunkNum].Filter.mesh.triangles = ind;
            ChunkPool[chunkNum].Filter.mesh.RecalculateNormals();
            ChunkPool[chunkNum].Filter.mesh.RecalculateBounds();


            ////mCollider.sharedMesh = filter.mesh;

            //var cl = mChunkList[i].Loader;
            //var n = cl.UpdateMainThread;
            //n[0] = false;
            //cl.UpdateMainThread = n;
            //mChunkList[i].Loader = cl;
            //mChunkList[i].Loader.Cleanup();
            //mChunksInUse[i] = false;
        }

        /// <summary>
        /// Create a queue of chunks to add to the list to eventualy render
        /// </summary>
        /// <param name="playerOrigin">The players current origin</param>
        /// <returns>False if the current player origin is the same as the older player origin otherwise return true after creating a origin list and submitting it to the render queue</returns>
        public bool CreateChunkQueue(in Vector3 playerOrigin)
        {

            Vector3 playerChunkOrigin = Chunk.GetChunkCenterFromLocation(playerOrigin, mTerrainParameters);
            if (mCurrentChunkOrigin == playerChunkOrigin)
                return false;

            // we need to dispatch a new chunk, so lets set the new origin 


            // compare old and new centers
            List<Vector3> oldChunkList = Chunk.GetChunksFromCenterLocation(mCurrentChunkOrigin, NumberOfChunks, ChunkRenderDistance, mTerrainParameters);
            List<Vector3> newChunksCenters = Chunk.GetChunksFromCenterLocation(playerChunkOrigin, NumberOfChunks, ChunkRenderDistance, mTerrainParameters);
            // the old centers that are still in the list are the centers that can be reused
            mCurrentChunkOrigin = playerChunkOrigin;

            // A list of the current origins minus the origin in the new center
            IEnumerable<Vector3> evictionListIE = oldChunkList.Except(newChunksCenters);
            var evictionList = evictionListIE.ToList();
            // a list of the origin that are in the new list but not it the cached list
            IEnumerable<Vector3> newListIE = newChunksCenters.Except(oldChunkList);
            var newOriginsToQueue = newListIE.ToList();

            Debug.Assert(evictionList.Count == newOriginsToQueue.Count);


            for (int i = 0; i < newOriginsToQueue.Count; i++)
            {
                ChunkOriginQueue.Enqueue(newOriginsToQueue[i]);
            }

        
            for (int i = 0; i < evictionList.Count; i++)
            {
                int chunkNumber = getChunkNumberFromOrigin(evictionList[i]);
                ChunkPoolAvailable.Enqueue(chunkNumber);
            }

            return true;

        }

        private int getChunkNumberFromOrigin(in Vector3 origin)
        {
            // do a linear search through the list to see where the chunk is
            // maybbe a dictionary might be a faster search????
            for (int i = 0; i < ChunkPool.Capacity; i++)
            {
                if (origin == ChunkPool[i].ChunkOrigin)
                    return i;
            }
            // return an inpossible index since we should find a chunk origin in the list
            return -1;
        }

        /// <summary>
        /// Dispatch the current queue on the Job system.
        /// </summary>
        /// <returns>False if no chunks were dispatch for for rendering or there are to many Jobs scheduled. True if a Job was scheduled </returns>
        public bool DispatchQueue()
        {
            // check to see if we need to dispatch a new chunk

            // CreateChunkQueue(playerChunkOrigin);
            // dont schedule if empty
            if (ChunkOriginQueue.Count == 0)
                return false;

            // dont schedule if jobsa are backing up
            // Should probably log this event
            if (LoaderPoolInUse.Count > MaxSize)
            {
                Debug.Log("Maximum number of Jobs Scheduled. Will Not schedule more until less than: " + MaxSize);
                return false;
            }

            //int initialQueueSize = ChunkOriginQueue.Count;
            for (int i = 0, initialQueueSize = ChunkOriginQueue.Count; i < initialQueueSize; i++)
            {


                if (LoaderPoolAvailable.Count > 0)
                {
                    int nextAvailableLoader = LoaderPoolAvailable.Dequeue();
                    LoaderPoolInUse.Enqueue(nextAvailableLoader);

                    Vector3 chunkCenter = ChunkOriginQueue.Dequeue();
                    LoaderPool[nextAvailableLoader].Job.RecenterChunk(chunkCenter);
                    LoaderPool[nextAvailableLoader].Schedule();
                    // Check every time since the queue may be empty after the last Dequeue event
                    // this may be redundednt as when the queue is empty we loop the queue size
                    if (ChunkOriginQueue.Count == 0)
                        break;


                }

            }

            return true;

        }
        /// <summary>
        /// Release all reference to native memory that the loader currently hold onto. Clear all the lists to remove all references to objects.
        /// </summary>
        public void ReleasePool()
        {

            foreach (var loader in LoaderPool)
            {
                loader.ReleaseLoader();
            }
            LoaderPool.Clear();

            foreach (var chunk in ChunkPool)
            {
                chunk.ReleaseChunk();
            }
            ChunkPool.Clear();

            LoaderPoolInUse.Clear();
            ChunkOriginQueue.Clear();




        }


    }

}