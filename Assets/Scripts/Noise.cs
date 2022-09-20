using UnityEngine;



public class Noise : MonoBehaviour{

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


    // Taken from https://gist.github.com/tntmeijs/6a3b4587ff7d38a6fa63e13f9d0ac46d

    public float GenerateNoise(in Vector3 point, in int seed)
    {
        
        float x = point.x/ _SampleLevel;
        float y = point.y/ _SampleLevel;
        float z = point.z/ _SampleLevel;
        float frequency =  _Frequency;
        float amplitude =  _Amplitude;
        float persistence =  _Persistence;
        int octave = _Octave;

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
