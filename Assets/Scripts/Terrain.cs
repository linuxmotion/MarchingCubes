﻿using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;






class Terrain : MonoBehaviour
{


    // A list of all point that are in the terrain mesh that will be created.
    public List<List<Voxel>> VoxelLayers { get; private set; }

    // Terrain detail - Higher is more detail

    [Header("Terrain Creation parameters")]
    [SerializeField]
    private int _Seed;
    [SerializeField]
    private int _MaxWorldSize;
    [SerializeField]
    private Transform _InitialPlayerLocation;

    [Header("Volume Sampling Parameters")]
    [SerializeField]
    private int _Scale;
    [SerializeField]
    private int _SamplingHeight;
    [SerializeField]
    private int _SamplingLength;
    [SerializeField]
    private int _SamplingWidth;


    [SerializeField]
    private int _ISO_Level;



    private Vector3 mOrigin;

    private List<VoxelCell> mVoxelCells;

    private Noise mNoise;



    public void Start()
    {

    }

    public void Initialize()
    {

        mNoise = GetComponent<Noise>();
        mVoxelCells = new List<VoxelCell>();
        mOrigin = _InitialPlayerLocation.position;
        mOrigin.y = 0;
        VoxelLayers = new List<List<Voxel>>(_SamplingHeight);

        _SamplingHeight++;
        _SamplingLength++;
        _SamplingWidth++;


    }
    public void GenerateInitialTerrain()
    {

        GenerateTerrain(mOrigin);

    }
    public void GenerateTerrain(Vector3 generateAround)
    {
        // Generate all the points in the world
        GeneratePoints(generateAround, _Seed);
    }


    private void GeneratePoints(Vector3 generateAround, in int seed)
    {
        // Generate +1 to height since a since it n+1 row of point to make the cube later
        for (int i = 0; i < _SamplingHeight; i++)
        {
            VoxelLayers.Add(GenerateSingleElevation(generateAround, seed));
            generateAround.y += _Scale;
        }

    }

    private List<Voxel> GenerateSingleElevation(Vector3 around, in int seed)
    {

        List<Voxel> points = new List<Voxel>();

        // Loop through each row of points to generate
        for (int row = 0; row < _SamplingLength * _Scale; row++)
        {
            // Loop through each coloumn within a row, generating points for the world
            for (int column = 0; column < _SamplingWidth * _Scale; column++)
            {
                Voxel vox = new Voxel();
                vox.Point = new Vector3(around.x - (_SamplingWidth / 2) + (column / _Scale),
                                        around.y,
                                        around.z - (_SamplingHeight / 2) + (row / _Scale));

                float noise = mNoise.GenerateNoise(vox.Point, seed);
                Debug.Log(noise);

                // move into the range -1  to 1
                vox.Density = -1f + 2 * noise;
                // Add the point to the list
                points.Add(vox);

            }
        }

        return points;

    }




    public void GenerateVoxelSurfaceList()
    {
        // We have a list of point densities arranged in layers
        // so from here we need to find the isoSurface
        //List<Vector3> surfacePoints = FindISOSurface();

        // To determine where the surface lies, we gather 8 voxels that form the nearest 
        // cube arranged on a grid with the player at the center 
        List<Voxel> lowerLayer;
        List<Voxel> upperLayer;
        // Loop through each layer 
        int numLayers = VoxelLayers.Count;
        // for every two layer
        for (int i = 0; i < _SamplingHeight; i++)
        {
            // we need two layers to test if whether the voxel is inside or outside of the surface 

            // but first check to see if we have reached the top of world that was sampled
            if (i + 1 >= numLayers)
                break;

            lowerLayer = VoxelLayers[i];
            upperLayer = VoxelLayers[i + 1];

            // check that our data has at least the correct amount of voxels in each list
            if (lowerLayer.Count != upperLayer.Count)
                throw new UnityException("List count mismatch: Cannot process voxel list - Terrain generation failed");


            // for each layer i need the four points of the cell j, j+1, i + Wr, j+1+wr
            // where j is the cell coulumn number and w is the number of cells in a row
            // the formula is derives as f(c,r) = <(c,r),(c+1,r),(c+1,r+1),(c,r+1)>
            for (int row = 0; row < _SamplingLength - 1; row++)
            {


                for (int column = 0; column < _SamplingWidth - 1; column++)
                {

                    int columnRowOffset = column + _SamplingWidth * row;
                    VoxelCell cell = new VoxelCell(_ISO_Level);
                    cell.mVoxel[0] = lowerLayer[columnRowOffset];  //(c, r)

                    cell.mVoxel[1] = upperLayer[columnRowOffset];  //(c, r)

                    cell.mVoxel[2] = upperLayer[columnRowOffset + 1]; // (c, r+1)
                    cell.mVoxel[3] = lowerLayer[columnRowOffset + 1]; // (c, r+1)

                    cell.mVoxel[4] = lowerLayer[columnRowOffset + _SamplingWidth];  //(c, r+1)
                    cell.mVoxel[5] = upperLayer[columnRowOffset + _SamplingWidth + 1];  // (c+1, r+1)
                    cell.mVoxel[6] = upperLayer[columnRowOffset + _SamplingWidth];  //(c, r+1)
                    cell.mVoxel[7] = lowerLayer[columnRowOffset + _SamplingWidth + 1];  // (c+1, r+1)       

                    cell.CreateEdgeList();
                    if (cell.IsOnSurface())
                        mVoxelCells.Add(cell);

                    // if the current cell is on the surface 
                    // which corners are above the surface and which corners are below the surface

                }

            }

        }



    }

    public List<VoxelCell> GetUnderlyingVoxelCells()
    {

        return mVoxelCells;
    }


}