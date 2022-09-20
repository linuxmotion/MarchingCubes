﻿using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;




public class MarchingCubes : MonoBehaviour
{


    Terrain mTerrain;

    [SerializeField]
    GameObject _PointPrefab;
    [SerializeField]
    private bool _UseDensityAsHeight;
    [SerializeField]
    private bool _CheckTestCases;
    [SerializeField]
    private int _CheckCaseNumber;
    [SerializeField]
    private bool _UseSpaceFill;
    [SerializeField]
    private bool _UseEdgeFill;
    [SerializeField]
    private bool _UseWeightedEdges;


    // Use this for initialization
    void Start()
    {

        MeshRenderer meshRenderer = gameObject.AddComponent<MeshRenderer>();
        meshRenderer.sharedMaterial = new Material(Shader.Find("Standard"));
        MeshFilter meshFilter = gameObject.AddComponent<MeshFilter>();
        Mesh mesh = new Mesh();
        // Setup the initial parameter for terrain generation
        mTerrain = GetComponent<Terrain>();
        mTerrain.Initialize();

        if (_CheckTestCases)
        {
            mesh = VoxelCellUnitTest.GetTestMesh(_CheckCaseNumber);
            meshFilter.mesh = mesh;
            return;

        }

        mTerrain.GenerateInitialTerrain();
        mTerrain.GenerateVoxelSurfaceList();

        if (_UseSpaceFill)
            SpaceFill();
        else
        {

            // Fill the edge of the iso surface with balls rather than a smooth triangulated surface
            if (_UseEdgeFill)
            {
                EdgeFill();

            }
            else
            {
                // fill the surface as a triangulated mesh
                ISOFill();

            }




        }

    }

    private void EdgeFill()
    {
        var uvc = mTerrain.GetUnderlyingVoxelCells();

        foreach (VoxelCell vc in uvc)
        {

            for (int i = 0; i < 1; i++)
            {
                Vector3 vect = new Vector3();
                vect = vc.mVoxel[i].Point;
                GameObject point = Instantiate(_PointPrefab);
                point.transform.position = vect;
                point.SetActive(true);
            }

        }
    }

    private void ISOFill()
    {


        List<Voxel> voxel;

        throw new NotImplementedException();
    }


    private void SpaceFill()
    {
        //Debug.Break();   
        List<Voxel> layer;

        // loop through each layer
        for (int i = 0; i < mTerrain.VoxelLayers.Count; i++)
        {
            layer = mTerrain.VoxelLayers[i];
            foreach (Voxel pointDen in layer)
            {
                // Debug.Log(vox.Point);
                GameObject point = Instantiate(_PointPrefab);
                point.transform.position = pointDen.Point;
                if (_UseDensityAsHeight)
                {
                    point.transform.localScale = Vector3.one / 2;
                    point.transform.position = new Vector3(pointDen.Point.x, pointDen.Density, pointDen.Point.z);
                }
                else
                {
                    //point.transform.localScale = Vector3.one * vox.Density;
                    if (pointDen.Density >= 0f)
                    {
                        point.SetActive(true);
                        // point.transform.localScale = Vector3.zero;
                    }
                    else
                    {
                        point.SetActive(false);
                    }
                }


            }
        }
    }

    // Update is called once per frame
    void Update()
    {
        //mTerrain = new Terrain(PlayerLocation.position);
    }
}