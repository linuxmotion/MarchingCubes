using UnityEngine;



public class NoiseManager : MonoBehaviour {

    [Header("Noise sampling parameters")]
    [SerializeField]
    private float _Frequency;
    [SerializeField]
    private float _Amplitude;
    [SerializeField]
    private float _Persistence;
    [SerializeField]
    private int _Octave;
    [SerializeField]
    private float _SampleLevel;

    public NoiseParameters Parameterize() {


        return new NoiseParameters(_Frequency, _Amplitude,_Persistence,_Octave,_SampleLevel);
    
    
    }
}

public struct NoiseParameters
{
    public float Frequency;
    public float Amplitude;
    public float Persistence;
    public int Octave;
    public float SampleLevel;

    public NoiseParameters(float frequency, float amplitude, float persistence, int octave, float sampleLevel)
    {
        Frequency = frequency;
        Amplitude = amplitude;
        Persistence = persistence;
        Octave = octave;
        SampleLevel = sampleLevel;
    }

    override public string ToString() {
        return base.ToString() + " - Frequency:" + Frequency + " | Amplitude: " + Amplitude + " | Persistence: " + Persistence + " | Octave: " + Octave + " | SampleLevel: " + SampleLevel + " ";  
    }
}

// Taken from https://gist.github.com/tntmeijs/6a3b4587ff7d38a6fa63e13f9d0ac46d

public class Noise {


    public static float GenerateNoise(in Vector3 point, in int seed, in NoiseParameters noiseParameters)
    {
        //return -point.y + Mathf.Sin(point.x) + Mathf.Sin(point.z);

        // Noise testing seed
        if (seed == 0)
        {
           
            return -point.y;
        
        
        }

        float x = point.x/ noiseParameters.SampleLevel;
        float y = point.y/ noiseParameters.SampleLevel;
        float z = point.z/ noiseParameters.SampleLevel;
        float frequency = noiseParameters.Frequency;
        float amplitude = noiseParameters.Amplitude;
        float persistence = noiseParameters.Persistence;
        int octave = noiseParameters.Octave;

        float noise = 0.0f;

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

        // Use the average of all octaves
        return noise / octave;


    }



}
