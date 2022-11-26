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
    [SerializeField][Range(-1,1)]
    private float _ISO_Level;
    [SerializeField]
    public int __SurfaceLevel;
    [SerializeField]
    public int __SeaLevel;
    [SerializeField]
    public int _BedrockLevel;

    public TerrainParameters Parameterize()
    {

        return new TerrainParameters(
            _InitialPlayerLocation.position,
            _SubSampleLevel,
            _SamplingHeight,
            _SamplingLength,
            _SamplingWidth,
            _ISO_Level,
            __SurfaceLevel,
            _BedrockLevel,
            __SeaLevel
            );

    }
    public Transform GetPlayerTransform()
    {
        return _InitialPlayerLocation;
    }

}

/// <summary>
/// A structure that controls the length, width and height of a chunk. It also controls the subsampling scale and what value determines a surface.
/// Values are set in the unity editor and are then passed into this structure for use
/// </summary>
public struct TerrainParameters
{

    public Vector3 Origin;
    public int Scale;
    public int SamplingHeight;
    public int SamplingLength;
    public int SamplingWidth;
    public float ISO_Level;
    public int SurfaceLevel;
    public int SeaLevel;
    public int BedrockLevel;

    public TerrainParameters(Vector3 origin, int scale, int samplingHeight, int samplingLength, int samplingWidth, float iSO_Level, int surfaceLevel, int bedrockLevel, int seaLevel)
    {
        Origin = origin;
        Scale = scale;
        SamplingHeight = samplingHeight;
        SamplingLength = samplingLength;
        SamplingWidth = samplingWidth;
        ISO_Level = iSO_Level;
        SurfaceLevel = surfaceLevel;
        BedrockLevel = bedrockLevel;
        SeaLevel = seaLevel;
    }

    public override bool Equals(object obj)
    {
        return
             EqualsExecptOrigin(obj) && Origin == ((TerrainParameters)obj).Origin;

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
            BedrockLevel == parameters.BedrockLevel &&
            SeaLevel == parameters.SeaLevel
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

    /// <summary>
    /// Check to see if the object passed is equal to current parameters except fopr the origin 
    /// of the parameter
    /// </summary>
    /// <param name="lhs"></param>
    /// <param name="rhs"></param>
    /// <returns></returns>
    public static bool operator ==(TerrainParameters lhs, TerrainParameters rhs)
    {

        return lhs.EqualsExecptOrigin(rhs);
    }

    public static bool operator !=(TerrainParameters lhs, TerrainParameters rhs)
    {


        return !lhs.EqualsExecptOrigin(rhs);


    }
}
