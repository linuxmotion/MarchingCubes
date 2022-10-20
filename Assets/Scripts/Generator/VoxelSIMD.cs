using System;
using System.Collections;
using Unity.Collections;
using Unity.Mathematics;
using Unity.Profiling;
using UnityEngine;

namespace Assets.Scripts.SIMD
{

    public struct Voxel
    {
        // Extra padding for vector optimizations
        public float4 Point;
        // allow for vector opertions and more control over generation
        public float4 Densities;
        public Voxel(float4 vect, float4 den) { Point = vect; Densities = den; }

    }

    public struct VoxelCell
    {
        public const int SIZE = 8;
        public const int NUM_POSSIBLE_TRIANGLES = 5;


        static readonly ProfilerMarker EdgeList = new ProfilerMarker("Simd CreateEdgeList-Edgelist");
        static readonly ProfilerMarker VertexMarker = new ProfilerMarker("Simd CreateEdgeList-VertexConnections");

        /// <summary>
        /// Creates the list of edges that are connected together, always calculates a new edgelist
        /// </summary>
        public static void CreateEdgeList(ref byte mEdgeList, float4 ISOLevel, NativeArray<Voxel> mVoxel)
        {
            EdgeList.Begin();
            if (mEdgeList != 0) mEdgeList = 0;
            bool pointBelowSurface;
            for (int i = 0; i < SIZE; i++)
            {
                pointBelowSurface = false;
                // for now only use the first density value
                if ((mVoxel[i].Densities >= ISOLevel).Equals(true))
                    pointBelowSurface = true;

                if (pointBelowSurface)
                {
                    mEdgeList |= (byte)(0b00000001 << i);
                }

            }

            EdgeList.End();

        }
        public static void CreateVertexConnections(in byte mEdgeList, ref int mNumberTriangles,ref NativeArray<int4> mVertexConnections)
        {
            VertexMarker.Begin();
            int numTri = MarchingCube.CASENUMBERTOTRIANGLES[mEdgeList];
            mNumberTriangles = numTri;
            if (numTri > NUM_POSSIBLE_TRIANGLES)
                throw new UnityException("More than the maximum triangles are to be produced: Logic error");


            //mVertexConnections = new float4[numTri];

            for (int i = 0; i < numTri; i++)
            {
                mVertexConnections[i] = MarchingCube.EDGECONNECTIONLIST[mEdgeList * NUM_POSSIBLE_TRIANGLES + i];
            }
            VertexMarker.End();
        }

        public static bool IsOnSurface(in byte mEdgeList)
        {
            if (mEdgeList == 0)
                return false;
            if (mEdgeList == 255)
                return false;
            return true;
        }
        public static bool IsOnOrUnderSurface(in byte mEdgeList)
        {
            if (mEdgeList == 0)
                return false;
            return true;
        }




        public static void CalculateMesh(in int numberofTriangles,ref  NativeArray<float4> triangleVertices,in  NativeArray<int4> vertexConnections,in  NativeArray<Voxel> voxels)
        {



            // know which edges connect, and how many triangle to generate

            for (int i = 0; i < numberofTriangles; i++)
            {
                Points3 p =  GetVertexFromEdge(vertexConnections[i], voxels);
                triangleVertices[i*3] = p.x;
                triangleVertices[i*3 + 1] = p.y;
                triangleVertices[i*3 + 2] = p.z;

            }

        }


        public struct Points3
        {
           public  float4 x;
            public float4 y;
                public float4 z;
        }

        /// <summary>
        /// The three edge number stored in a int4 
        /// </summary>
        /// <param name="edges"></param>
        /// <returns></returns>
        /// 
        private static Points3 GetVertexFromEdge(int4 edges, NativeArray<Voxel> mVoxel)
        {
            int4 index1 = 0;
            int4 index2 = 0;
            int edge;

            // Check each internal float for the edge and set each index
            for (int i = 0; i < 3; i++)
            {
                edge = edges[i];

                switch (edge)
                {

                    case 0:
                        {
                            // 0,1
                            index1[i] = 0;
                            index2[i] = 1;
                        };
                        break;
                    case 1:
                        {
                            // lerp vertex 1,2
                            index1[i] = 1;
                            index2[i] = 2;
                        };
                        break;
                    case 2:
                        {
                            //lerp vertex 2,3
                            index1[i] = 2;
                            index2[i] = 3;
                        };
                        break;
                    case 3:
                        {
                            // lerp vertex 3,0
                            index1[i] = 3;
                            index2[i] = 0;
                        };
                        break;

                    case 4:
                        {
                            //lerp 4, 5
                            index1[i] = 4;
                            index2[i] = 5;
                        };
                        break;
                    case 5:
                        {
                            // lerp 5, 6
                            index1[i] = 5;
                            index2[i] = 6;
                        };
                        break;
                    case 6:
                        {
                            // lerp 6, 7
                            index1[i] = 6;
                            index2[i] = 7;

                        };
                        break;
                    case 7:
                        {
                            // lerp 7,4
                            index1[i] = 7;
                            index2[i] = 4;
                        };
                        break;
                    case 8:
                        {
                            // lerp 0,4
                            index1[i] = 0;
                            index2[i] = 4;

                        };
                        break;
                    case 9:
                        {
                            // lerp vertices 1,5
                            index1[i] = 1;
                            index2[i] = 5;
                        };
                        break;
                    case 10:
                        {
                            //lerp 2,6
                            index1[i] = 2;
                            index2[i] = 6;
                        };
                        break;
                    case 11:
                        {
                            // lerp 3, 7
                            index1[i] = 3;
                            index2[i] = 7;
                        };
                        break;
                    default:
                        { // 0,1
                            index1[i] = -1;
                            index2[i] = -1;
                            break;
                        }



                }
            }

            Points3 p = new Points3();

            float weight = FindWeightFromDensities(mVoxel[index1.x].Densities, mVoxel[index2.x].Densities);
            p.x = Lerpf4(mVoxel[index1.x].Point, mVoxel[index2.x].Point, weight);
            weight = FindWeightFromDensities(mVoxel[index1.y].Densities, mVoxel[index2.y].Densities);
            p.y = Lerpf4(mVoxel[index1.y].Point, mVoxel[index2.y].Point, weight);
            weight = FindWeightFromDensities(mVoxel[index1.z].Densities, mVoxel[index2.z].Densities);
            p.z = Lerpf4(mVoxel[index1.z].Point, mVoxel[index2.z].Point, weight);


            return p;
        }

        private static float FindWeightFromDensities(float4 densities1, float4 densities2)
        {
            float4 massDen = densities1 / (densities1 - densities2);
            return massDen.x;
        }

        public static float4 Lerpf4(in float4 point1,in float4 point2,in float4 weight)
        {



            return (point1 * (1 - weight) + point2 * weight);

        }


    }

}