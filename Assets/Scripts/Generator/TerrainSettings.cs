using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Assertions;

class TerrainSettings : MonoBehaviour
{


    [Header("Terrain Creation parameters")]
    [SerializeField]
    private int _Seed;
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
}

public struct TerrainParameters
{

    public int Seed;
    public Transform InitialPlayerLocation;
    public int Scale;
    public int SamplingHeight;
    public int SamplingLength;
    public int SamplingWidth;
    public int ISO_Level;

    public TerrainParameters(int seed, Transform initialPlayerLocation, int scale, int samplingHeight, int samplingLength, int samplingWidth, int iSO_Level)
    {
        Seed = seed;
        InitialPlayerLocation = initialPlayerLocation;
        Scale = scale;
        SamplingHeight = samplingHeight;
        SamplingLength = samplingLength;
        SamplingWidth = samplingWidth;
        ISO_Level = iSO_Level;
    }
}
