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

    public TerrainParameters Paramterize() {

        return new TerrainParameters(_Seed, _InitialPlayerLocation.position, _Scale, _SamplingHeight, _SamplingLength, _SamplingWidth, _ISO_Level);
    
    }
    public Transform GetPlayerTransform() {
        return _InitialPlayerLocation;
    }

    public bool UpdateSettings(TerrainSettings settings)
    {
        if (this != settings)
            return true;

        return false;
        
    }
}

/// <summary>
/// A structure that controls the length, width and height of a chunk. It also controls the subsampling scale and what value determines a surface 
/// </summary>
public struct TerrainParameters
{

    public int Seed;
    public Vector3 Origin;
    public int Scale;
    public int SamplingHeight;
    public int SamplingLength;
    public int SamplingWidth;
    public int ISO_Level;

    public TerrainParameters(int seed, Vector3 origin, int scale, int samplingHeight, int samplingLength, int samplingWidth, int iSO_Level)
    {
        Seed = seed;
        Origin = origin;
        Scale = scale;
        SamplingHeight = samplingHeight;
        SamplingLength = samplingLength;
        SamplingWidth = samplingWidth;
        ISO_Level = iSO_Level;
    }
}
