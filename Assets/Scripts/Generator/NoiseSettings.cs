using System;
using Unity.Mathematics;
using Unity.Profiling;
using UnityEngine;



public class NoiseSettings : MonoBehaviour
{

    [Header("Noise sampling parameters")]
    [SerializeField]
    private int _Seed;
    [SerializeField]
    private float _Frequency;
    [SerializeField]
    private float _Amplitude;
    [SerializeField]
    private float _Persistence;
    [SerializeField]
    private int _Octave;
    [SerializeField]
    private float _Scale;

    public NoiseParameters Parameterize()
    {
        return new NoiseParameters(_Seed, _Frequency, _Amplitude, _Persistence, _Octave, _Scale);
    }

}

public struct NoiseParameters
{
    public float Seed;
    public float Frequency;
    public float Amplitude;
    public float Persistence;
    public int Octave;
    public float SampleLevel;

    public NoiseParameters(float seed, float frequency, float amplitude, float persistence, int octave, float sampleLevel)
    {
        Seed = seed;
        Frequency = frequency;
        Amplitude = amplitude;
        Persistence = persistence;
        Octave = octave;
        SampleLevel = sampleLevel;
    }

    public static bool operator ==(NoiseParameters n1, NoiseParameters n2)
    {


        return n1.Equals(n2);
    }
    public static bool operator !=(NoiseParameters n1, NoiseParameters n2)
    {


        return !n1.Equals(n2);
    }

    override public string ToString()
    {
        return base.ToString() +
            " | Frequency:" + Frequency +
            " | Seed: " + Seed +
            " | Amplitude: " + Amplitude + 
            " | Persistence: " + Persistence + 
            " | Octave: " + Octave + 
            " | SampleLevel: " + SampleLevel + 
            " ";
    }

    public override bool Equals(object obj)
    {
        return obj is NoiseParameters parameters &&
            Seed == parameters.Seed &&
               Frequency == parameters.Frequency &&
               Amplitude == parameters.Amplitude &&
               Persistence == parameters.Persistence &&
               Octave == parameters.Octave &&
               SampleLevel == parameters.SampleLevel;
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(Seed, Frequency, Amplitude, Persistence, Octave, SampleLevel);
    }
}

// Taken from https://gist.github.com/tntmeijs/6a3b4587ff7d38a6fa63e13f9d0ac46d

public class Noise
{


    public static float GenerateNoise(in Vector3 point, in NoiseParameters noiseParameters, in int surfaceLevel) {

        float4 vect;
        vect.x = point.x;
        vect.y = point.y;
        vect.z = point.z;
        vect.w = 0;
        return GenerateNoise(vect, noiseParameters, surfaceLevel).x;
    
    }
    static readonly ProfilerMarker NoiseMarker = new ProfilerMarker("GenerateNoise - Marker 1");
    static readonly ProfilerMarker NoiseMarker2 = new ProfilerMarker("GenerateNoise - Marker 2");
    public static float4 GenerateNoise(in float4 point, in NoiseParameters noiseParameters, in int surfaceLevel)
    {
    
        NoiseMarker.Begin();     
        if (point.y < surfaceLevel) {
            NoiseMarker.End(); 
            //return 1;
        }

        float sampleLevel = noiseParameters.SampleLevel;
        float seed = noiseParameters.SampleLevel ;
        float x = point.x / sampleLevel;
        float y = point.y / sampleLevel;
        float z = point.z / sampleLevel;
        float frequency = noiseParameters.Frequency;
        float amplitude = noiseParameters.Amplitude;
        float persistence = noiseParameters.Persistence;
        int octave = noiseParameters.Octave;

        float noise = 0;
   



        for (int i = 0; i < octave; ++i)
        {
            // Get all permutations of noise for each individual axis
            float noiseXY = Mathf.PerlinNoise(x * frequency + seed, y * frequency + seed) * amplitude;
            float noiseXZ = Mathf.PerlinNoise(x * frequency + seed, z * frequency + seed) * amplitude;
            float noiseYZ = Mathf.PerlinNoise(y * frequency + seed, z * frequency + seed) * amplitude;

            // Reverse of the permutations of noise for each individual axis
            float noiseYX = Mathf.PerlinNoise(y * frequency + seed, x * frequency + seed) * amplitude;
            float noiseZX = Mathf.PerlinNoise(z * frequency + seed, x * frequency + seed) * amplitude;
            float noiseZY = Mathf.PerlinNoise(z * frequency + seed, y * frequency + seed) * amplitude;

            // Use the average of the noise functions
            noise += (noiseXY + noiseXZ + noiseYZ + noiseYX + noiseZX + noiseZY) / 6.0f;

            amplitude *= persistence;
            frequency *= 2.0f;
        }


        // This should push the ending value into the range of -1 to 1, more or less, since noise could be slighty
        // below 0 or beyond 1.0
        NoiseMarker.End();
        return -1f + 2 * (noise / octave);


    }



}
