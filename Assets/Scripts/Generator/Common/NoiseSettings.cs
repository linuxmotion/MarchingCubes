using System;
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
