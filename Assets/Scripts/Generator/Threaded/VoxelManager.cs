using System;
using System.Collections;
using Unity.Burst.Intrinsics;
using Unity.Profiling;
using UnityEngine;

namespace Assets.Scripts.Threaded
{

    public struct Voxel
    {

        public Vector3 Point;
        public float Density;
        public Voxel(Vector3 vect, float den) { Point = vect; Density = den; }

    }

    public class VoxelCell
    {
        public const int SIZE = 8;
        public const int NUM_POSSIBLE_TRIANGLES = 5;

        public Voxel[] mVoxel = new Voxel[SIZE];
        private byte mEdgeList = 0;
        private int ISOLevel;
        public int mNumberTriangles { get; private set; }
        public Vector3[] mTriangleVertices;
        public int[] mTriangleIndex;

        // We have up to five triangle that get connected per cube
        private Vector3[] mVertexConnections;

        public int[] CASENUMBERTOTRIANGLES = new int[256]{
                  0 , 1, 1, 2, 1, 2, 2, 3, 1, 2, 2, 3, 2, 3, 3, 2, 1, 2, 2, 3, 2, 3, 3, 4, 2, 3, 3, 4, 3, 4, 4, 3,
                  1, 2, 2, 3, 2, 3, 3, 4, 2, 3, 3, 4, 3, 4, 4, 3, 2, 3, 3, 2, 3, 4, 4, 3, 3, 4, 4, 3, 4, 5, 5, 2,
                  1, 2, 2, 3, 2, 3, 3, 4, 2, 3, 3, 4, 3, 4, 4, 3, 2, 3, 3, 4, 3, 4, 4, 5, 3, 4, 4, 5, 4, 5, 5, 4,
                  2, 3, 3, 4, 3, 4, 2, 3, 3, 4, 4, 5, 4, 5, 3, 2, 3, 4, 4, 3, 4, 5, 3, 2, 4, 5, 5, 4, 5, 2, 4, 1,
                  1, 2, 2, 3, 2, 3, 3, 4, 2, 3, 3, 4, 3, 4, 4, 3, 2, 3, 3, 4, 3, 4, 4, 5, 3, 2, 4, 3, 4, 3, 5, 2,
                  2, 3, 3, 4, 3, 4, 4, 5, 3, 4, 4, 5, 4, 5, 5, 4, 3, 4, 4, 3, 4, 5, 5, 4, 4, 3, 5, 2, 5, 4, 2, 1,
                  2, 3, 3, 4, 3, 4, 4, 5, 3, 4, 4, 5, 2, 3, 3, 2, 3, 4, 4, 5, 4, 5, 5, 2, 4, 3, 5, 4, 3, 2, 4, 1,
                  3, 4, 4, 5, 4, 5, 3, 4, 4, 5, 5, 2, 3, 4, 2, 1, 2, 3, 3, 2, 3, 4, 2, 1, 3, 2, 4, 1, 2, 1, 1, 0 };

        readonly static Vector3[,] EDGECONNECTIONLIST = new Vector3[,] {
       {new Vector3(-1,-1,-1), new Vector3(-1,-1,-1), new Vector3(-1,-1,-1), new Vector3(-1,-1,-1), new Vector3(-1,-1,-1) },
       {new Vector3(0,8,3), new Vector3(-1,-1,-1), new Vector3(-1,-1,-1), new Vector3(-1,-1,-1), new Vector3(-1,-1,-1) },
       {new Vector3(0,1,9), new Vector3(-1,-1,-1), new Vector3(-1,-1,-1), new Vector3(-1,-1,-1), new Vector3(-1,-1,-1) },
       {new Vector3(1,8,3), new Vector3(9,8,1), new Vector3(-1,-1,-1), new Vector3(-1,-1,-1), new Vector3(-1,-1,-1) },
       {new Vector3(1,2,10), new Vector3(-1,-1,-1), new Vector3(-1,-1,-1), new Vector3(-1,-1,-1), new Vector3(-1,-1,-1) },
       {new Vector3(0,8,3), new Vector3(1,2,10), new Vector3(-1,-1,-1), new Vector3(-1,-1,-1), new Vector3(-1,-1,-1) },
       {new Vector3(9,2,10), new Vector3(0,2,9), new Vector3(-1,-1,-1), new Vector3(-1,-1,-1), new Vector3(-1,-1,-1) },
       {new Vector3(2,8,3), new Vector3(2,10,8), new Vector3(10,9,8), new Vector3(-1,-1,-1), new Vector3(-1,-1,-1) },
       {new Vector3(3,11,2), new Vector3(-1,-1,-1), new Vector3(-1,-1,-1), new Vector3(-1,-1,-1), new Vector3(-1,-1,-1) },
       {new Vector3(0,11,2), new Vector3(8,11,0), new Vector3(-1,-1,-1), new Vector3(-1,-1,-1), new Vector3(-1,-1,-1) },
       {new Vector3(1,9,0), new Vector3(2,3,11), new Vector3(-1,-1,-1), new Vector3(-1,-1,-1), new Vector3(-1,-1,-1) },
       {new Vector3(1,11,2), new Vector3(1,9,11), new Vector3(9,8,11), new Vector3(-1,-1,-1), new Vector3(-1,-1,-1) },
       {new Vector3(3,10,1), new Vector3(11,10,3), new Vector3(-1,-1,-1), new Vector3(-1,-1,-1), new Vector3(-1,-1,-1) },
       {new Vector3(0,10,1), new Vector3(0,8,10), new Vector3(8,11,10), new Vector3(-1,-1,-1), new Vector3(-1,-1,-1) },
       {new Vector3(3,9,0), new Vector3(3,11,9), new Vector3(11,10,9), new Vector3(-1,-1,-1), new Vector3(-1,-1,-1) },
       {new Vector3(9,8,10), new Vector3(10,8,11), new Vector3(-1,-1,-1), new Vector3(-1,-1,-1), new Vector3(-1,-1,-1) },
       {new Vector3(4,7,8), new Vector3(-1,-1,-1), new Vector3(-1,-1,-1), new Vector3(-1,-1,-1), new Vector3(-1,-1,-1) },
       {new Vector3(4,3,0), new Vector3(7,3,4), new Vector3(-1,-1,-1), new Vector3(-1,-1,-1), new Vector3(-1,-1,-1) },
       {new Vector3(0,1,9), new Vector3(8,4,7), new Vector3(-1,-1,-1), new Vector3(-1,-1,-1), new Vector3(-1,-1,-1) },
       {new Vector3(4,1,9), new Vector3(4,7,1), new Vector3(7,3,1), new Vector3(-1,-1,-1), new Vector3(-1,-1,-1) },
       {new Vector3(1,2,10), new Vector3(8,4,7), new Vector3(-1,-1,-1), new Vector3(-1,-1,-1), new Vector3(-1,-1,-1) },
       {new Vector3(3,4,7), new Vector3(3,0,4), new Vector3(1,2,10), new Vector3(-1,-1,-1), new Vector3(-1,-1,-1) },
       {new Vector3(9,2,10), new Vector3(9,0,2), new Vector3(8,4,7), new Vector3(-1,-1,-1), new Vector3(-1,-1,-1) },
       {new Vector3(2,10,9), new Vector3(2,9,7), new Vector3(2,7,3), new Vector3(7,9,4), new Vector3(-1,-1,-1) },
       {new Vector3(8,4,7), new Vector3(3,11,2), new Vector3(-1,-1,-1), new Vector3(-1,-1,-1), new Vector3(-1,-1,-1) },
       {new Vector3(11,4,7), new Vector3(11,2,4), new Vector3(2,0,4), new Vector3(-1,-1,-1), new Vector3(-1,-1,-1) },
       {new Vector3(9,0,1), new Vector3(8,4,7), new Vector3(2,3,11), new Vector3(-1,-1,-1), new Vector3(-1,-1,-1) },
       {new Vector3(4,7,11), new Vector3(9,4,11), new Vector3(9,11,2), new Vector3(9,2,1), new Vector3(-1,-1,-1) },
       {new Vector3(3,10,1), new Vector3(3,11,10), new Vector3(7,8,4), new Vector3(-1,-1,-1), new Vector3(-1,-1,-1) },
       {new Vector3(1,11,10), new Vector3(1,4,11), new Vector3(1,0,4), new Vector3(7,11,4), new Vector3(-1,-1,-1) },
       {new Vector3(4,7,8), new Vector3(9,0,11), new Vector3(9,11,10), new Vector3(11,0,3), new Vector3(-1,-1,-1) },
       {new Vector3(4,7,11), new Vector3(4,11,9), new Vector3(9,11,10), new Vector3(-1,-1,-1), new Vector3(-1,-1,-1) },
       {new Vector3(9,5,4), new Vector3(-1,-1,-1), new Vector3(-1,-1,-1), new Vector3(-1,-1,-1), new Vector3(-1,-1,-1) },
       {new Vector3(9,5,4), new Vector3(0,8,3), new Vector3(-1,-1,-1), new Vector3(-1,-1,-1), new Vector3(-1,-1,-1) },
       {new Vector3(0,5,4), new Vector3(1,5,0), new Vector3(-1,-1,-1), new Vector3(-1,-1,-1), new Vector3(-1,-1,-1) },
       {new Vector3(8,5,4), new Vector3(8,3,5), new Vector3(3,1,5), new Vector3(-1,-1,-1), new Vector3(-1,-1,-1) },
       {new Vector3(1,2,10), new Vector3(9,5,4), new Vector3(-1,-1,-1), new Vector3(-1,-1,-1), new Vector3(-1,-1,-1) },
       {new Vector3(3,0,8), new Vector3(1,2,10), new Vector3(4,9,5), new Vector3(-1,-1,-1), new Vector3(-1,-1,-1) },
       {new Vector3(5,2,10), new Vector3(5,4,2), new Vector3(4,0,2), new Vector3(-1,-1,-1), new Vector3(-1,-1,-1) },
       {new Vector3(2,10,5), new Vector3(3,2,5), new Vector3(3,5,4), new Vector3(3,4,8), new Vector3(-1,-1,-1) },
       {new Vector3(9,5,4), new Vector3(2,3,11), new Vector3(-1,-1,-1), new Vector3(-1,-1,-1), new Vector3(-1,-1,-1) },
       {new Vector3(0,11,2), new Vector3(0,8,11), new Vector3(4,9,5), new Vector3(-1,-1,-1), new Vector3(-1,-1,-1) },
       {new Vector3(0,5,4), new Vector3(0,1,5), new Vector3(2,3,11), new Vector3(-1,-1,-1), new Vector3(-1,-1,-1) },
       {new Vector3(2,1,5), new Vector3(2,5,8), new Vector3(2,8,11), new Vector3(4,8,5), new Vector3(-1,-1,-1) },
       {new Vector3(10,3,11), new Vector3(10,1,3), new Vector3(9,5,4), new Vector3(-1,-1,-1), new Vector3(-1,-1,-1) },
       {new Vector3(4,9,5), new Vector3(0,8,1), new Vector3(8,10,1), new Vector3(8,11,10), new Vector3(-1,-1,-1) },
       {new Vector3(5,4,0), new Vector3(5,0,11), new Vector3(5,11,10), new Vector3(11,0,3), new Vector3(-1,-1,-1) },
       {new Vector3(5,4,8), new Vector3(5,8,10), new Vector3(10,8,11), new Vector3(-1,-1,-1), new Vector3(-1,-1,-1) },
       {new Vector3(9,7,8), new Vector3(5,7,9), new Vector3(-1,-1,-1), new Vector3(-1,-1,-1), new Vector3(-1,-1,-1) },
       {new Vector3(9,3,0), new Vector3(9,5,3), new Vector3(5,7,3), new Vector3(-1,-1,-1), new Vector3(-1,-1,-1) },
       {new Vector3(0,7,8), new Vector3(0,1,7), new Vector3(1,5,7), new Vector3(-1,-1,-1), new Vector3(-1,-1,-1) },
       {new Vector3(1,5,3), new Vector3(3,5,7), new Vector3(-1,-1,-1), new Vector3(-1,-1,-1), new Vector3(-1,-1,-1) },
       {new Vector3(9,7,8), new Vector3(9,5,7), new Vector3(10,1,2), new Vector3(-1,-1,-1), new Vector3(-1,-1,-1) },
       {new Vector3(10,1,2), new Vector3(9,5,0), new Vector3(5,3,0), new Vector3(5,7,3), new Vector3(-1,-1,-1) },
       {new Vector3(8,0,2), new Vector3(8,2,5), new Vector3(8,5,7), new Vector3(10,5,2), new Vector3(-1,-1,-1) },
       {new Vector3(2,10,5), new Vector3(2,5,3), new Vector3(3,5,7), new Vector3(-1,-1,-1), new Vector3(-1,-1,-1) },
       {new Vector3(7,9,5), new Vector3(7,8,9), new Vector3(3,11,2), new Vector3(-1,-1,-1), new Vector3(-1,-1,-1) },
       {new Vector3(9,5,7), new Vector3(9,7,2), new Vector3(9,2,0), new Vector3(2,7,11), new Vector3(-1,-1,-1) },
       {new Vector3(2,3,11), new Vector3(0,1,8), new Vector3(1,7,8), new Vector3(1,5,7), new Vector3(-1,-1,-1) },
       {new Vector3(11,2,1), new Vector3(11,1,7), new Vector3(7,1,5), new Vector3(-1,-1,-1), new Vector3(-1,-1,-1) },
       {new Vector3(9,5,8), new Vector3(8,5,7), new Vector3(10,1,3), new Vector3(10,3,11), new Vector3(-1,-1,-1) },
       {new Vector3(5,7,0), new Vector3(5,0,9), new Vector3(7,11,0), new Vector3(1,0,10), new Vector3(11,10,0) },
       {new Vector3(11,10,0), new Vector3(11,0,3), new Vector3(10,5,0), new Vector3(8,0,7), new Vector3(5,7,0) },
       {new Vector3(11,10,5), new Vector3(7,11,5), new Vector3(-1,-1,-1), new Vector3(-1,-1,-1), new Vector3(-1,-1,-1) },
       {new Vector3(10,6,5), new Vector3(-1,-1,-1), new Vector3(-1,-1,-1), new Vector3(-1,-1,-1), new Vector3(-1,-1,-1) },
       {new Vector3(0,8,3), new Vector3(5,10,6), new Vector3(-1,-1,-1), new Vector3(-1,-1,-1), new Vector3(-1,-1,-1) },
       {new Vector3(9,0,1), new Vector3(5,10,6), new Vector3(-1,-1,-1), new Vector3(-1,-1,-1), new Vector3(-1,-1,-1) },
       {new Vector3(1,8,3), new Vector3(1,9,8), new Vector3(5,10,6), new Vector3(-1,-1,-1), new Vector3(-1,-1,-1) },
       {new Vector3(1,6,5), new Vector3(2,6,1), new Vector3(-1,-1,-1), new Vector3(-1,-1,-1), new Vector3(-1,-1,-1) },
       {new Vector3(1,6,5), new Vector3(1,2,6), new Vector3(3,0,8), new Vector3(-1,-1,-1), new Vector3(-1,-1,-1) },
       {new Vector3(9,6,5), new Vector3(9,0,6), new Vector3(0,2,6), new Vector3(-1,-1,-1), new Vector3(-1,-1,-1) },
       {new Vector3(5,9,8), new Vector3(5,8,2), new Vector3(5,2,6), new Vector3(3,2,8), new Vector3(-1,-1,-1) },
       {new Vector3(2,3,11), new Vector3(10,6,5), new Vector3(-1,-1,-1), new Vector3(-1,-1,-1), new Vector3(-1,-1,-1) },
       {new Vector3(11,0,8), new Vector3(11,2,0), new Vector3(10,6,5), new Vector3(-1,-1,-1), new Vector3(-1,-1,-1) },
       {new Vector3(0,1,9), new Vector3(2,3,11), new Vector3(5,10,6), new Vector3(-1,-1,-1), new Vector3(-1,-1,-1) },
       {new Vector3(5,10,6), new Vector3(1,9,2), new Vector3(9,11,2), new Vector3(9,8,11), new Vector3(-1,-1,-1) },
       {new Vector3(6,3,11), new Vector3(6,5,3), new Vector3(5,1,3), new Vector3(-1,-1,-1), new Vector3(-1,-1,-1) },
       {new Vector3(0,8,11), new Vector3(0,11,5), new Vector3(0,5,1), new Vector3(5,11,6), new Vector3(-1,-1,-1) },
       {new Vector3(3,11,6), new Vector3(0,3,6), new Vector3(0,6,5), new Vector3(0,5,9), new Vector3(-1,-1,-1) },
       {new Vector3(6,5,9), new Vector3(6,9,11), new Vector3(11,9,8), new Vector3(-1,-1,-1), new Vector3(-1,-1,-1) },
       {new Vector3(5,10,6), new Vector3(4,7,8), new Vector3(-1,-1,-1), new Vector3(-1,-1,-1), new Vector3(-1,-1,-1) },
       {new Vector3(4,3,0), new Vector3(4,7,3), new Vector3(6,5,10), new Vector3(-1,-1,-1), new Vector3(-1,-1,-1) },
       {new Vector3(1,9,0), new Vector3(5,10,6), new Vector3(8,4,7), new Vector3(-1,-1,-1), new Vector3(-1,-1,-1) },
       {new Vector3(10,6,5), new Vector3(1,9,7), new Vector3(1,7,3), new Vector3(7,9,4), new Vector3(-1,-1,-1) },
       {new Vector3(6,1,2), new Vector3(6,5,1), new Vector3(4,7,8), new Vector3(-1,-1,-1), new Vector3(-1,-1,-1) },
       {new Vector3(1,2,5), new Vector3(5,2,6), new Vector3(3,0,4), new Vector3(3,4,7), new Vector3(-1,-1,-1) },
       {new Vector3(8,4,7), new Vector3(9,0,5), new Vector3(0,6,5), new Vector3(0,2,6), new Vector3(-1,-1,-1) },
       {new Vector3(7,3,9), new Vector3(7,9,4), new Vector3(3,2,9), new Vector3(5,9,6), new Vector3(2,6,9) },
       {new Vector3(3,11,2), new Vector3(7,8,4), new Vector3(10,6,5), new Vector3(-1,-1,-1), new Vector3(-1,-1,-1) },
       {new Vector3(5,10,6), new Vector3(4,7,2), new Vector3(4,2,0), new Vector3(2,7,11), new Vector3(-1,-1,-1) },
       {new Vector3(0,1,9), new Vector3(4,7,8), new Vector3(2,3,11), new Vector3(5,10,6), new Vector3(-1,-1,-1) },
       {new Vector3(9,2,1), new Vector3(9,11,2), new Vector3(9,4,11), new Vector3(7,11,4), new Vector3(5,10,6) },
       {new Vector3(8,4,7), new Vector3(3,11,5), new Vector3(3,5,1), new Vector3(5,11,6), new Vector3(-1,-1,-1) },
       {new Vector3(5,1,11), new Vector3(5,11,6), new Vector3(1,0,11), new Vector3(7,11,4), new Vector3(0,4,11) },
       {new Vector3(0,5,9), new Vector3(0,6,5), new Vector3(0,3,6), new Vector3(11,6,3), new Vector3(8,4,7) },
       {new Vector3(6,5,9), new Vector3(6,9,11), new Vector3(4,7,9), new Vector3(7,11,9), new Vector3(-1,-1,-1) },
       {new Vector3(10,4,9), new Vector3(6,4,10), new Vector3(-1,-1,-1), new Vector3(-1,-1,-1), new Vector3(-1,-1,-1) },
       {new Vector3(4,10,6), new Vector3(4,9,10), new Vector3(0,8,3), new Vector3(-1,-1,-1), new Vector3(-1,-1,-1) },
       {new Vector3(10,0,1), new Vector3(10,6,0), new Vector3(6,4,0), new Vector3(-1,-1,-1), new Vector3(-1,-1,-1) },
       {new Vector3(8,3,1), new Vector3(8,1,6), new Vector3(8,6,4), new Vector3(6,1,10), new Vector3(-1,-1,-1) },
       {new Vector3(1,4,9), new Vector3(1,2,4), new Vector3(2,6,4), new Vector3(-1,-1,-1), new Vector3(-1,-1,-1) },
       {new Vector3(3,0,8), new Vector3(1,2,9), new Vector3(2,4,9), new Vector3(2,6,4), new Vector3(-1,-1,-1) },
       {new Vector3(0,2,4), new Vector3(4,2,6), new Vector3(-1,-1,-1), new Vector3(-1,-1,-1), new Vector3(-1,-1,-1) },
       {new Vector3(8,3,2), new Vector3(8,2,4), new Vector3(4,2,6), new Vector3(-1,-1,-1), new Vector3(-1,-1,-1) },
       {new Vector3(10,4,9), new Vector3(10,6,4), new Vector3(11,2,3), new Vector3(-1,-1,-1), new Vector3(-1,-1,-1) },
       {new Vector3(0,8,2), new Vector3(2,8,11), new Vector3(4,9,10), new Vector3(4,10,6), new Vector3(-1,-1,-1) },
       {new Vector3(3,11,2), new Vector3(0,1,6), new Vector3(0,6,4), new Vector3(6,1,10), new Vector3(-1,-1,-1) },
       {new Vector3(6,4,1), new Vector3(6,1,10), new Vector3(4,8,1), new Vector3(2,1,11), new Vector3(8,11,1) },
       {new Vector3(9,6,4), new Vector3(9,3,6), new Vector3(9,1,3), new Vector3(11,6,3), new Vector3(-1,-1,-1) },
       {new Vector3(8,11,1), new Vector3(8,1,0), new Vector3(11,6,1), new Vector3(9,1,4), new Vector3(6,4,1) },
       {new Vector3(3,11,6), new Vector3(3,6,0), new Vector3(0,6,4), new Vector3(-1,-1,-1), new Vector3(-1,-1,-1) },
       {new Vector3(6,4,8), new Vector3(11,6,8), new Vector3(-1,-1,-1), new Vector3(-1,-1,-1), new Vector3(-1,-1,-1) },
       {new Vector3(7,10,6), new Vector3(7,8,10), new Vector3(8,9,10), new Vector3(-1,-1,-1), new Vector3(-1,-1,-1) },
       {new Vector3(0,7,3), new Vector3(0,10,7), new Vector3(0,9,10), new Vector3(6,7,10), new Vector3(-1,-1,-1) },
       {new Vector3(10,6,7), new Vector3(1,10,7), new Vector3(1,7,8), new Vector3(1,8,0), new Vector3(-1,-1,-1) },
       {new Vector3(10,6,7), new Vector3(10,7,1), new Vector3(1,7,3), new Vector3(-1,-1,-1), new Vector3(-1,-1,-1) },
       {new Vector3(1,2,6), new Vector3(1,6,8), new Vector3(1,8,9), new Vector3(8,6,7), new Vector3(-1,-1,-1) },
       {new Vector3(2,6,9), new Vector3(2,9,1), new Vector3(6,7,9), new Vector3(0,9,3), new Vector3(7,3,9) },
       {new Vector3(7,8,0), new Vector3(7,0,6), new Vector3(6,0,2), new Vector3(-1,-1,-1), new Vector3(-1,-1,-1) },
       {new Vector3(7,3,2), new Vector3(6,7,2), new Vector3(-1,-1,-1), new Vector3(-1,-1,-1), new Vector3(-1,-1,-1) },
       {new Vector3(2,3,11), new Vector3(10,6,8), new Vector3(10,8,9), new Vector3(8,6,7), new Vector3(-1,-1,-1) },
       {new Vector3(2,0,7), new Vector3(2,7,11), new Vector3(0,9,7), new Vector3(6,7,10), new Vector3(9,10,7) },
       {new Vector3(1,8,0), new Vector3(1,7,8), new Vector3(1,10,7), new Vector3(6,7,10), new Vector3(2,3,11) },
       {new Vector3(11,2,1), new Vector3(11,1,7), new Vector3(10,6,1), new Vector3(6,7,1), new Vector3(-1,-1,-1) },
       {new Vector3(8,9,6), new Vector3(8,6,7), new Vector3(9,1,6), new Vector3(11,6,3), new Vector3(1,3,6) },
       {new Vector3(0,9,1), new Vector3(11,6,7), new Vector3(-1,-1,-1), new Vector3(-1,-1,-1), new Vector3(-1,-1,-1) },
       {new Vector3(7,8,0), new Vector3(7,0,6), new Vector3(3,11,0), new Vector3(11,6,0), new Vector3(-1,-1,-1) },
       {new Vector3(7,11,6), new Vector3(-1,-1,-1), new Vector3(-1,-1,-1), new Vector3(-1,-1,-1), new Vector3(-1,-1,-1) },
       {new Vector3(7,6,11), new Vector3(-1,-1,-1), new Vector3(-1,-1,-1), new Vector3(-1,-1,-1), new Vector3(-1,-1,-1) },
       {new Vector3(3,0,8), new Vector3(11,7,6), new Vector3(-1,-1,-1), new Vector3(-1,-1,-1), new Vector3(-1,-1,-1) },
       {new Vector3(0,1,9), new Vector3(11,7,6), new Vector3(-1,-1,-1), new Vector3(-1,-1,-1), new Vector3(-1,-1,-1) },
       {new Vector3(8,1,9), new Vector3(8,3,1), new Vector3(11,7,6), new Vector3(-1,-1,-1), new Vector3(-1,-1,-1) },
       {new Vector3(10,1,2), new Vector3(6,11,7), new Vector3(-1,-1,-1), new Vector3(-1,-1,-1), new Vector3(-1,-1,-1) },
       {new Vector3(1,2,10), new Vector3(3,0,8), new Vector3(6,11,7), new Vector3(-1,-1,-1), new Vector3(-1,-1,-1) },
       {new Vector3(2,9,0), new Vector3(2,10,9), new Vector3(6,11,7), new Vector3(-1,-1,-1), new Vector3(-1,-1,-1) },
       {new Vector3(6,11,7), new Vector3(2,10,3), new Vector3(10,8,3), new Vector3(10,9,8), new Vector3(-1,-1,-1) },
       {new Vector3(7,2,3), new Vector3(6,2,7), new Vector3(-1,-1,-1), new Vector3(-1,-1,-1), new Vector3(-1,-1,-1) },
       {new Vector3(7,0,8), new Vector3(7,6,0), new Vector3(6,2,0), new Vector3(-1,-1,-1), new Vector3(-1,-1,-1) },
       {new Vector3(2,7,6), new Vector3(2,3,7), new Vector3(0,1,9), new Vector3(-1,-1,-1), new Vector3(-1,-1,-1) },
       {new Vector3(1,6,2), new Vector3(1,8,6), new Vector3(1,9,8), new Vector3(8,7,6), new Vector3(-1,-1,-1) },
       {new Vector3(10,7,6), new Vector3(10,1,7), new Vector3(1,3,7), new Vector3(-1,-1,-1), new Vector3(-1,-1,-1) },
       {new Vector3(10,7,6), new Vector3(1,7,10), new Vector3(1,8,7), new Vector3(1,0,8), new Vector3(-1,-1,-1) },
       {new Vector3(0,3,7), new Vector3(0,7,10), new Vector3(0,10,9), new Vector3(6,10,7), new Vector3(-1,-1,-1) },
       {new Vector3(7,6,10), new Vector3(7,10,8), new Vector3(8,10,9), new Vector3(-1,-1,-1), new Vector3(-1,-1,-1) },
       {new Vector3(6,8,4), new Vector3(11,8,6), new Vector3(-1,-1,-1), new Vector3(-1,-1,-1), new Vector3(-1,-1,-1) },
       {new Vector3(3,6,11), new Vector3(3,0,6), new Vector3(0,4,6), new Vector3(-1,-1,-1), new Vector3(-1,-1,-1) },
       {new Vector3(8,6,11), new Vector3(8,4,6), new Vector3(9,0,1), new Vector3(-1,-1,-1), new Vector3(-1,-1,-1) },
       {new Vector3(9,4,6), new Vector3(9,6,3), new Vector3(9,3,1), new Vector3(11,3,6), new Vector3(-1,-1,-1) },
       {new Vector3(6,8,4), new Vector3(6,11,8), new Vector3(2,10,1), new Vector3(-1,-1,-1), new Vector3(-1,-1,-1) },
       {new Vector3(1,2,10), new Vector3(3,0,11), new Vector3(0,6,11), new Vector3(0,4,6), new Vector3(-1,-1,-1) },
       {new Vector3(4,11,8), new Vector3(4,6,11), new Vector3(0,2,9), new Vector3(2,10,9), new Vector3(-1,-1,-1) },
       {new Vector3(10,9,3), new Vector3(10,3,2), new Vector3(9,4,3), new Vector3(11,3,6), new Vector3(4,6,3) },
       {new Vector3(8,2,3), new Vector3(8,4,2), new Vector3(4,6,2), new Vector3(-1,-1,-1), new Vector3(-1,-1,-1) },
       {new Vector3(0,4,2), new Vector3(4,6,2), new Vector3(-1,-1,-1), new Vector3(-1,-1,-1), new Vector3(-1,-1,-1) },
       {new Vector3(1,9,0), new Vector3(2,3,4), new Vector3(2,4,6), new Vector3(4,3,8), new Vector3(-1,-1,-1) },
       {new Vector3(1,9,4), new Vector3(1,4,2), new Vector3(2,4,6), new Vector3(-1,-1,-1), new Vector3(-1,-1,-1) },
       {new Vector3(8,1,3), new Vector3(8,6,1), new Vector3(8,4,6), new Vector3(6,10,1), new Vector3(-1,-1,-1) },
       {new Vector3(10,1,0), new Vector3(10,0,6), new Vector3(6,0,4), new Vector3(-1,-1,-1), new Vector3(-1,-1,-1) },
       {new Vector3(4,6,3), new Vector3(4,3,8), new Vector3(6,10,3), new Vector3(0,3,9), new Vector3(10,9,3) },
       {new Vector3(10,9,4), new Vector3(6,10,4), new Vector3(-1,-1,-1), new Vector3(-1,-1,-1), new Vector3(-1,-1,-1) },
       {new Vector3(4,9,5), new Vector3(7,6,11), new Vector3(-1,-1,-1), new Vector3(-1,-1,-1), new Vector3(-1,-1,-1) },
       {new Vector3(0,8,3), new Vector3(4,9,5), new Vector3(11,7,6), new Vector3(-1,-1,-1), new Vector3(-1,-1,-1) },
       {new Vector3(5,0,1), new Vector3(5,4,0), new Vector3(7,6,11), new Vector3(-1,-1,-1), new Vector3(-1,-1,-1) },
       {new Vector3(11,7,6), new Vector3(8,3,4), new Vector3(3,5,4), new Vector3(3,1,5), new Vector3(-1,-1,-1) },
       {new Vector3(9,5,4), new Vector3(10,1,2), new Vector3(7,6,11), new Vector3(-1,-1,-1), new Vector3(-1,-1,-1) },
       {new Vector3(6,11,7), new Vector3(1,2,10), new Vector3(0,8,3), new Vector3(4,9,5), new Vector3(-1,-1,-1) },
       {new Vector3(7,6,11), new Vector3(5,4,10), new Vector3(4,2,10), new Vector3(4,0,2), new Vector3(-1,-1,-1) },
       {new Vector3(3,4,8), new Vector3(3,5,4), new Vector3(3,2,5), new Vector3(10,5,2), new Vector3(11,7,6) },
       {new Vector3(7,2,3), new Vector3(7,6,2), new Vector3(5,4,9), new Vector3(-1,-1,-1), new Vector3(-1,-1,-1) },
       {new Vector3(9,5,4), new Vector3(0,8,6), new Vector3(0,6,2), new Vector3(6,8,7), new Vector3(-1,-1,-1) },
       {new Vector3(3,6,2), new Vector3(3,7,6), new Vector3(1,5,0), new Vector3(5,4,0), new Vector3(-1,-1,-1) },
       {new Vector3(6,2,8), new Vector3(6,8,7), new Vector3(2,1,8), new Vector3(4,8,5), new Vector3(1,5,8) },
       {new Vector3(9,5,4), new Vector3(10,1,6), new Vector3(1,7,6), new Vector3(1,3,7), new Vector3(-1,-1,-1) },
       {new Vector3(1,6,10), new Vector3(1,7,6), new Vector3(1,0,7), new Vector3(8,7,0), new Vector3(9,5,4) },
       {new Vector3(4,0,10), new Vector3(4,10,5), new Vector3(0,3,10), new Vector3(6,10,7), new Vector3(3,7,10) },
       {new Vector3(7,6,10), new Vector3(7,10,8), new Vector3(5,4,10), new Vector3(4,8,10), new Vector3(-1,-1,-1) },
       {new Vector3(6,9,5), new Vector3(6,11,9), new Vector3(11,8,9), new Vector3(-1,-1,-1), new Vector3(-1,-1,-1) },
       {new Vector3(3,6,11), new Vector3(0,6,3), new Vector3(0,5,6), new Vector3(0,9,5), new Vector3(-1,-1,-1) },
       {new Vector3(0,11,8), new Vector3(0,5,11), new Vector3(0,1,5), new Vector3(5,6,11), new Vector3(-1,-1,-1) },
       {new Vector3(6,11,3), new Vector3(6,3,5), new Vector3(5,3,1), new Vector3(-1,-1,-1), new Vector3(-1,-1,-1) },
       {new Vector3(1,2,10), new Vector3(9,5,11), new Vector3(9,11,8), new Vector3(11,5,6), new Vector3(-1,-1,-1) },
       {new Vector3(0,11,3), new Vector3(0,6,11), new Vector3(0,9,6), new Vector3(5,6,9), new Vector3(1,2,10) },
       {new Vector3(11,8,5), new Vector3(11,5,6), new Vector3(8,0,5), new Vector3(10,5,2), new Vector3(0,2,5) },
       {new Vector3(6,11,3), new Vector3(6,3,5), new Vector3(2,10,3), new Vector3(10,5,3), new Vector3(-1,-1,-1) },
       {new Vector3(5,8,9), new Vector3(5,2,8), new Vector3(5,6,2), new Vector3(3,8,2), new Vector3(-1,-1,-1) },
       {new Vector3(9,5,6), new Vector3(9,6,0), new Vector3(0,6,2), new Vector3(-1,-1,-1), new Vector3(-1,-1,-1) },
       {new Vector3(1,5,8), new Vector3(1,8,0), new Vector3(5,6,8), new Vector3(3,8,2), new Vector3(6,2,8) },
       {new Vector3(1,5,6), new Vector3(2,1,6), new Vector3(-1,-1,-1), new Vector3(-1,-1,-1), new Vector3(-1,-1,-1) },
       {new Vector3(1,3,6), new Vector3(1,6,10), new Vector3(3,8,6), new Vector3(5,6,9), new Vector3(8,9,6) },
       {new Vector3(10,1,0), new Vector3(10,0,6), new Vector3(9,5,0), new Vector3(5,6,0), new Vector3(-1,-1,-1) },
       {new Vector3(0,3,8), new Vector3(5,6,10), new Vector3(-1,-1,-1), new Vector3(-1,-1,-1), new Vector3(-1,-1,-1) },
       {new Vector3(10,5,6), new Vector3(-1,-1,-1), new Vector3(-1,-1,-1), new Vector3(-1,-1,-1), new Vector3(-1,-1,-1) },
       {new Vector3(11,5,10), new Vector3(7,5,11), new Vector3(-1,-1,-1), new Vector3(-1,-1,-1), new Vector3(-1,-1,-1) },
       {new Vector3(11,5,10), new Vector3(11,7,5), new Vector3(8,3,0), new Vector3(-1,-1,-1), new Vector3(-1,-1,-1) },
       {new Vector3(5,11,7), new Vector3(5,10,11), new Vector3(1,9,0), new Vector3(-1,-1,-1), new Vector3(-1,-1,-1) },
       {new Vector3(10,7,5), new Vector3(10,11,7), new Vector3(9,8,1), new Vector3(8,3,1), new Vector3(-1,-1,-1) },
       {new Vector3(11,1,2), new Vector3(11,7,1), new Vector3(7,5,1), new Vector3(-1,-1,-1), new Vector3(-1,-1,-1) },
       {new Vector3(0,8,3), new Vector3(1,2,7), new Vector3(1,7,5), new Vector3(7,2,11), new Vector3(-1,-1,-1) },
       {new Vector3(9,7,5), new Vector3(9,2,7), new Vector3(9,0,2), new Vector3(2,11,7), new Vector3(-1,-1,-1) },
       {new Vector3(7,5,2), new Vector3(7,2,11), new Vector3(5,9,2), new Vector3(3,2,8), new Vector3(9,8,2) },
       {new Vector3(2,5,10), new Vector3(2,3,5), new Vector3(3,7,5), new Vector3(-1,-1,-1), new Vector3(-1,-1,-1) },
       {new Vector3(8,2,0), new Vector3(8,5,2), new Vector3(8,7,5), new Vector3(10,2,5), new Vector3(-1,-1,-1) },
       {new Vector3(9,0,1), new Vector3(5,10,3), new Vector3(5,3,7), new Vector3(3,10,2), new Vector3(-1,-1,-1) },
       {new Vector3(9,8,2), new Vector3(9,2,1), new Vector3(8,7,2), new Vector3(10,2,5), new Vector3(7,5,2) },
       {new Vector3(1,3,5), new Vector3(3,7,5), new Vector3(-1,-1,-1), new Vector3(-1,-1,-1), new Vector3(-1,-1,-1) },
       {new Vector3(0,8,7), new Vector3(0,7,1), new Vector3(1,7,5), new Vector3(-1,-1,-1), new Vector3(-1,-1,-1) },
       {new Vector3(9,0,3), new Vector3(9,3,5), new Vector3(5,3,7), new Vector3(-1,-1,-1), new Vector3(-1,-1,-1) },
       {new Vector3(9,8,7), new Vector3(5,9,7), new Vector3(-1,-1,-1), new Vector3(-1,-1,-1), new Vector3(-1,-1,-1) },
       {new Vector3(5,8,4), new Vector3(5,10,8), new Vector3(10,11,8), new Vector3(-1,-1,-1), new Vector3(-1,-1,-1) },
       {new Vector3(5,0,4), new Vector3(5,11,0), new Vector3(5,10,11), new Vector3(11,3,0), new Vector3(-1,-1,-1) },
       {new Vector3(0,1,9), new Vector3(8,4,10), new Vector3(8,10,11), new Vector3(10,4,5), new Vector3(-1,-1,-1) },
       {new Vector3(10,11,4), new Vector3(10,4,5), new Vector3(11,3,4), new Vector3(9,4,1), new Vector3(3,1,4) },
       {new Vector3(2,5,1), new Vector3(2,8,5), new Vector3(2,11,8), new Vector3(4,5,8), new Vector3(-1,-1,-1) },
       {new Vector3(0,4,11), new Vector3(0,11,3), new Vector3(4,5,11), new Vector3(2,11,1), new Vector3(5,1,11) },
       {new Vector3(0,2,5), new Vector3(0,5,9), new Vector3(2,11,5), new Vector3(4,5,8), new Vector3(11,8,5) },
       {new Vector3(9,4,5), new Vector3(2,11,3), new Vector3(-1,-1,-1), new Vector3(-1,-1,-1), new Vector3(-1,-1,-1) },
       {new Vector3(2,5,10), new Vector3(3,5,2), new Vector3(3,4,5), new Vector3(3,8,4), new Vector3(-1,-1,-1) },
       {new Vector3(5,10,2), new Vector3(5,2,4), new Vector3(4,2,0), new Vector3(-1,-1,-1), new Vector3(-1,-1,-1) },
       {new Vector3(3,10,2), new Vector3(3,5,10), new Vector3(3,8,5), new Vector3(4,5,8), new Vector3(0,1,9) },
       {new Vector3(5,10,2), new Vector3(5,2,4), new Vector3(1,9,2), new Vector3(9,4,2), new Vector3(-1,-1,-1) },
       {new Vector3(8,4,5), new Vector3(8,5,3), new Vector3(3,5,1), new Vector3(-1,-1,-1), new Vector3(-1,-1,-1) },
       {new Vector3(0,4,5), new Vector3(1,0,5), new Vector3(-1,-1,-1), new Vector3(-1,-1,-1), new Vector3(-1,-1,-1) },
       {new Vector3(8,4,5), new Vector3(8,5,3), new Vector3(9,0,5), new Vector3(0,3,5), new Vector3(-1,-1,-1) },
       {new Vector3(9,4,5), new Vector3(-1,-1,-1), new Vector3(-1,-1,-1), new Vector3(-1,-1,-1), new Vector3(-1,-1,-1) },
       {new Vector3(4,11,7), new Vector3(4,9,11), new Vector3(9,10,11), new Vector3(-1,-1,-1), new Vector3(-1,-1,-1) },
       {new Vector3(0,8,3), new Vector3(4,9,7), new Vector3(9,11,7), new Vector3(9,10,11), new Vector3(-1,-1,-1) },
       {new Vector3(1,10,11), new Vector3(1,11,4), new Vector3(1,4,0), new Vector3(7,4,11), new Vector3(-1,-1,-1) },
       {new Vector3(3,1,4), new Vector3(3,4,8), new Vector3(1,10,4), new Vector3(7,4,11), new Vector3(10,11,4) },
       {new Vector3(4,11,7), new Vector3(9,11,4), new Vector3(9,2,11), new Vector3(9,1,2), new Vector3(-1,-1,-1) },
       {new Vector3(9,7,4), new Vector3(9,11,7), new Vector3(9,1,11), new Vector3(2,11,1), new Vector3(0,8,3) },
       {new Vector3(11,7,4), new Vector3(11,4,2), new Vector3(2,4,0), new Vector3(-1,-1,-1), new Vector3(-1,-1,-1) },
       {new Vector3(11,7,4), new Vector3(11,4,2), new Vector3(8,3,4), new Vector3(3,2,4), new Vector3(-1,-1,-1) },
       {new Vector3(2,9,10), new Vector3(2,7,9), new Vector3(2,3,7), new Vector3(7,4,9), new Vector3(-1,-1,-1) },
       {new Vector3(9,10,7), new Vector3(9,7,4), new Vector3(10,2,7), new Vector3(8,7,0), new Vector3(2,0,7) },
       {new Vector3(3,7,10), new Vector3(3,10,2), new Vector3(7,4,10), new Vector3(1,10,0), new Vector3(4,0,10) },
       {new Vector3(1,10,2), new Vector3(8,7,4), new Vector3(-1,-1,-1), new Vector3(-1,-1,-1), new Vector3(-1,-1,-1) },
       {new Vector3(4,9,1), new Vector3(4,1,7), new Vector3(7,1,3), new Vector3(-1,-1,-1), new Vector3(-1,-1,-1) },
       {new Vector3(4,9,1), new Vector3(4,1,7), new Vector3(0,8,1), new Vector3(8,7,1), new Vector3(-1,-1,-1) },
       {new Vector3(4,0,3), new Vector3(7,4,3), new Vector3(-1,-1,-1), new Vector3(-1,-1,-1), new Vector3(-1,-1,-1) },
       {new Vector3(4,8,7), new Vector3(-1,-1,-1), new Vector3(-1,-1,-1), new Vector3(-1,-1,-1), new Vector3(-1,-1,-1) },
       {new Vector3(9,10,8), new Vector3(10,11,8), new Vector3(-1,-1,-1), new Vector3(-1,-1,-1), new Vector3(-1,-1,-1) },
       {new Vector3(3,0,9), new Vector3(3,9,11), new Vector3(11,9,10), new Vector3(-1,-1,-1), new Vector3(-1,-1,-1) },
       {new Vector3(0,1,10), new Vector3(0,10,8), new Vector3(8,10,11), new Vector3(-1,-1,-1), new Vector3(-1,-1,-1) },
       {new Vector3(3,1,10), new Vector3(11,3,10), new Vector3(-1,-1,-1), new Vector3(-1,-1,-1), new Vector3(-1,-1,-1) },
       {new Vector3(1,2,11), new Vector3(1,11,9), new Vector3(9,11,8), new Vector3(-1,-1,-1), new Vector3(-1,-1,-1) },
       {new Vector3(3,0,9), new Vector3(3,9,11), new Vector3(1,2,9), new Vector3(2,11,9), new Vector3(-1,-1,-1) },
       {new Vector3(0,2,11), new Vector3(8,0,11), new Vector3(-1,-1,-1), new Vector3(-1,-1,-1), new Vector3(-1,-1,-1) },
       {new Vector3(3,2,11), new Vector3(-1,-1,-1), new Vector3(-1,-1,-1), new Vector3(-1,-1,-1), new Vector3(-1,-1,-1) },
       {new Vector3(2,3,8), new Vector3(2,8,10), new Vector3(10,8,9), new Vector3(-1,-1,-1), new Vector3(-1,-1,-1) },
       {new Vector3(9,10,2), new Vector3(0,9,2), new Vector3(-1,-1,-1), new Vector3(-1,-1,-1), new Vector3(-1,-1,-1) },
       {new Vector3(2,3,8), new Vector3(2,8,10), new Vector3(0,1,8), new Vector3(1,10,8), new Vector3(-1,-1,-1) },
       {new Vector3(1,10,2), new Vector3(-1,-1,-1), new Vector3(-1,-1,-1), new Vector3(-1,-1,-1), new Vector3(-1,-1,-1) },
       {new Vector3(1,3,8), new Vector3(9,1,8), new Vector3(-1,-1,-1), new Vector3(-1,-1,-1), new Vector3(-1,-1,-1) },
       {new Vector3(0,9,1), new Vector3(-1,-1,-1), new Vector3(-1,-1,-1), new Vector3(-1,-1,-1), new Vector3(-1,-1,-1) },
       {new Vector3(0,3,8), new Vector3(-1,-1,-1), new Vector3(-1,-1,-1), new Vector3(-1,-1,-1), new Vector3(-1,-1,-1) },
       {new Vector3(-1,-1,-1), new Vector3(-1,-1,-1), new Vector3(-1,-1,-1), new Vector3(-1,-1,-1), new Vector3(-1,-1,-1) }
    };




        public VoxelCell(int isoLevel)
        {
            ISOLevel = isoLevel;
        }

        static readonly ProfilerMarker EdgeList = new ProfilerMarker("CreateEdgeList-Edgelist");
        static readonly ProfilerMarker VertexMarker = new ProfilerMarker("CreateEdgeList-VertexConnections");

        /// <summary>
        /// Creates the list of edges that are connected together, always calculates a new edgelist
        /// </summary>
        public void CreateEdgeList()
        {
            EdgeList.Begin();
            if (mEdgeList != 0) mEdgeList = 0;
            for (int i = 0; i < 8; i++) // high acces time
            {

                if (mVoxel[i].Density >= ISOLevel)
                {
                    mEdgeList |= (byte)(0b00000001 << i);
                }

            }

            EdgeList.End();

        }

        public void CreateVertexConnections()
        {
            VertexMarker.Begin();
            int numTri = CASENUMBERTOTRIANGLES[mEdgeList];
            mNumberTriangles = numTri;
            if (numTri > NUM_POSSIBLE_TRIANGLES)
                throw new UnityException("More than the maximum triangles are to be prooduced: Logic error");


            mVertexConnections = new Vector3[numTri];
            for (int i = 0; i < numTri; i++)
            {
                mVertexConnections[i] = EDGECONNECTIONLIST[mEdgeList, i];
            }
            VertexMarker.End();
        }

        public bool IsOnSurface()
        {
            if (mEdgeList == 0)
                return false;
            if (mEdgeList == 255)
                return false;
            return true;
        }
        public bool IsOnOrUnderSurface()
        {
            if (mEdgeList == 0)
                return false;
            return true;
        }


        public Vector3[] GetEdgeTriangle()
        {

            return mVertexConnections;

        }



        public void CalculateMesh()
        {


            // know which edges connect, and how many triangle to generate

            Vector3[] vertices = new Vector3[mNumberTriangles * 3];
            int[] triangles = new int[mNumberTriangles * 3];

            for (int i = 0; i < mNumberTriangles * 3; i += 3)
            {
                Vector3[] triangle = new Vector3[3];

                Vector3 edges = mVertexConnections[i / 3];
                triangle = MakeTriangleFromEdgeVector(edges);
                vertices[i] = triangle[0];
                vertices[i + 1] = triangle[1];
                vertices[i + 2] = triangle[2];

                triangles[i] = i;
                triangles[i + 1] = i + 1;
                triangles[i + 2] = i + 2;


            }
            mTriangleVertices = vertices;
            mTriangleIndex = triangles;

            return;

        }

        private Vector3[] MakeTriangleFromEdgeVector(Vector3 triangle)
        {

            Vector3[] vertices = new Vector3[3];

            vertices[0] = GetVertexFromEdge((int)triangle.x);
            vertices[1] = GetVertexFromEdge((int)triangle.y);
            vertices[2] = GetVertexFromEdge((int)triangle.z);

            return vertices;
        }

        private Vector3 GetVertexFromEdge(int edge)
        {
            int index1 = -1;
            int index2 = -1;

            switch (edge)
            {

                case 0:
                    {
                        // 0,1
                        index1 = 0;
                        index2 = 1;
                    };
                    break;
                case 1:
                    {
                        // lerp vertex 1,2
                        index1 = 1;
                        index2 = 2;
                    };
                    break;
                case 2:
                    {
                        //lerp vertex 2,3
                        index1 = 2;
                        index2 = 3;
                    };
                    break;
                case 3:
                    {
                        // lerp vertex 3,0
                        index1 = 3;
                        index2 = 0;
                    };
                    break;

                case 4:
                    {
                        //lerp 4, 5
                        index1 = 4;
                        index2 = 5;
                    };
                    break;
                case 5:
                    {
                        // lerp 5, 6
                        index1 = 5;
                        index2 = 6;
                    };
                    break;
                case 6:
                    {
                        // lerp 6, 7
                        index1 = 6;
                        index2 = 7;

                    };
                    break;
                case 7:
                    {
                        // lerp 7,4
                        index1 = 7;
                        index2 = 4;
                    };
                    break;
                case 8:
                    {
                        // lerp 0,4
                        index1 = 0;
                        index2 = 4;

                    };
                    break;
                case 9:
                    {
                        // lerp vertices 1,5
                        index1 = 1;
                        index2 = 5;
                    };
                    break;
                case 10:
                    {
                        //lerp 2,6
                        index1 = 2;
                        index2 = 6;
                    };
                    break;
                case 11:
                    {
                        // lerp 3, 7
                        index1 = 3;
                        index2 = 7;
                    };
                    break;
                default:
                    { // 0,1
                        index1 = -1;
                        index2 = -1;
                        break;
                    }



            }

            float weight = mVoxel[index1].Density / (float)(mVoxel[index1].Density - mVoxel[index2].Density);


            return Vector3.Lerp(mVoxel[index1].Point, mVoxel[index2].Point, weight);
        }



        private int[] GetMeshTriangles()
        {
            throw new NotImplementedException();
        }

        private Vector3[] GetMeshVertices()
        {

            throw new NotImplementedException();
        }
    }




}