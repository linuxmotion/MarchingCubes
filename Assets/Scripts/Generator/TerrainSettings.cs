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

    public TerrainParameters Parameterize() {

        return new TerrainParameters(_Seed, _InitialPlayerLocation.position, _Scale, _SamplingHeight, _SamplingLength, _SamplingWidth, _ISO_Level);
    
    }
    public Transform GetPlayerTransform() {
        return _InitialPlayerLocation;
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

    public override bool Equals(object obj)
    {
        return obj is TerrainParameters parameters && 
            Seed == parameters.Seed && 
            Origin == parameters.Origin && 
            Scale == parameters.Scale && 
            SamplingHeight == parameters.SamplingHeight
            && SamplingLength == parameters.SamplingLength && 
            SamplingWidth == parameters.SamplingWidth && 
            ISO_Level == parameters.ISO_Level;        
    } 
    public  bool EqualsExecptOrigin(object obj)
    {
        return obj is TerrainParameters parameters && 
            Seed == parameters.Seed && 
            Scale == parameters.Scale && 
            SamplingHeight == parameters.SamplingHeight
            && SamplingLength == parameters.SamplingLength && 
            SamplingWidth == parameters.SamplingWidth && 
            ISO_Level == parameters.ISO_Level;        
    }

    public override int GetHashCode()
    {
        return base.GetHashCode();
    }

    public override string ToString()
    {
        return base.ToString() +  
            " | Seed: " +  Seed + 
            " | Origin: " + Origin + 
            " | Scale: " + Scale + 
            " | SamplingHeight: "+ SamplingHeight +
            " | SamplingWidth: "  +SamplingWidth +
            " | SamplingLength: " + SamplingLength + 
            " | ISO_Level: "+ ISO_Level;
    }

    public static bool operator ==(TerrainParameters lhs, TerrainParameters rhs) {

        return lhs.Equals(rhs);
    }

    public static bool operator !=(TerrainParameters lhs, TerrainParameters rhs) {


        return !lhs.Equals(rhs);

    
    }
}
