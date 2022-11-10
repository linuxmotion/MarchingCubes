using System;
using System.Collections.Generic;
using Unity.Jobs;
using UnityEngine;

namespace Assets.Scripts.SIMD
{

    class Chunk
    {
        public MeshFilter Filter;
        public MeshRenderer Renderer;
        public GameObject ChunkObject;

        public Vector3 ChunkOrigin;
        public readonly int ChunkID;

        public Chunk(int ID)
        {
            ChunkID = ID;
        }

        public void ReleaseChunk()
        {
            Filter = null;
            Renderer = null;
            GameObject.Destroy(ChunkObject);
        }

        public static List<Vector3> GetChunksFromCenterLocation(in Vector3 playerchunkorigin, int numChunks, int renderDistance, TerrainParameters terrainParameters)
        {
            int numberChunks = numChunks,
                length = terrainParameters.SamplingLength,
                width = terrainParameters.SamplingWidth;

            float bottomLeftX = playerchunkorigin.x - ((renderDistance - 1) / 2) * width;
            float bottomLeftz = playerchunkorigin.z - ((renderDistance - 1) / 2) * length;

            List<Vector3> chunksOrigins = new List<Vector3>(numberChunks);
            for (int i = 0; i < renderDistance; i++)
            {
                for (int j = 0; j < renderDistance; j++)
                {
                    Vector3 point = new Vector3();
                    point.x = bottomLeftX + width * j;
                    point.z = bottomLeftz + length * i;
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
        public static Vector3 GetChunkCenterFromLocation(in Vector3 playerLocation, TerrainParameters terrainParameters)
        {
            int length = terrainParameters.SamplingLength,
                width = terrainParameters.SamplingWidth;

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
            if (lower > upper)
            {
                return c;
            }
            return f;
        }
    }
}