using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Assertions;

class TerrainSettings : MonoBehaviour
{


    [Header("Terrain Creation parameters")]
 
    [SerializeField]
    private Transform _InitialPlayerLocation;
    [Header("Volume Sampling Parameters")]
    [SerializeField]
    private int _SubSampleLevel;
    [SerializeField]
    private int _SamplingHeight;
    [SerializeField]
    private int _SamplingLength;
    [SerializeField]
    private int _SamplingWidth;
    [SerializeField]
    private int _ISO_Level;
    [SerializeField]
    public int __SurfaceLevel;
    [SerializeField]
    public int _BedrockLevel;

    public TerrainParameters Parameterize()
    {

        return new TerrainParameters(_InitialPlayerLocation.position, _SubSampleLevel, _SamplingHeight, _SamplingLength, _SamplingWidth, _ISO_Level, __SurfaceLevel, _BedrockLevel);

    }
    public Transform GetPlayerTransform()
    {
        return _InitialPlayerLocation;
    }

}

/// <summary>
/// A structure that controls the length, width and height of a chunk. It also controls the subsampling scale and what value determines a surface 
/// </summary>
public struct TerrainParameters
{

    public Vector3 Origin;
    public int Scale;
    public int SamplingHeight;
    public int SamplingLength;
    public int SamplingWidth;
    public int ISO_Level;
    public int SurfaceLevel;
    public int BedrockLevel;

    public TerrainParameters( Vector3 origin, int scale, int samplingHeight, int samplingLength, int samplingWidth, int iSO_Level, int surfaceLevel, int bedrockLevel)
    {
        Origin = origin;
        Scale = scale;
        SamplingHeight = samplingHeight;
        SamplingLength = samplingLength;
        SamplingWidth = samplingWidth;
        ISO_Level = iSO_Level;
        SurfaceLevel = surfaceLevel;
        BedrockLevel = bedrockLevel;
    }

    public override bool Equals(object obj)
    {
        return obj is TerrainParameters parameters &&
            Origin == parameters.Origin && EqualsExecptOrigin(parameters);
            
    }
    public bool EqualsExecptOrigin(object obj)
    {
        return obj is TerrainParameters parameters &&
            Scale == parameters.Scale &&
            SamplingHeight == parameters.SamplingHeight
            && SamplingLength == parameters.SamplingLength &&
            SamplingWidth == parameters.SamplingWidth &&
            ISO_Level == parameters.ISO_Level &&
            SurfaceLevel == parameters.SurfaceLevel &&
            BedrockLevel == parameters.BedrockLevel
            ;
    }

    public override int GetHashCode()
    {
        return base.GetHashCode();
    }

    public override string ToString()
    {
        return base.ToString() +
            " | Origin: " + Origin +
            " | Scale: " + Scale +
            " | SamplingHeight: " + SamplingHeight +
            " | SamplingWidth: " + SamplingWidth +  
            " | SamplingLength: " + SamplingLength +
            " | ISO_Level: " + ISO_Level;
    }

    public static bool operator ==(TerrainParameters lhs, TerrainParameters rhs)
    {

        return lhs.Equals(rhs);
    }

    public static bool operator !=(TerrainParameters lhs, TerrainParameters rhs)
    {


        return !lhs.Equals(rhs);


    }
}
