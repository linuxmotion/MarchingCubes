using Unity.Mathematics;
using Unity.Profiling;
using UnityEngine;
// Taken from https://gist.github.com/tntmeijs/6a3b4587ff7d38a6fa63e13f9d0ac46d

public class Noise
{



    static readonly ProfilerMarker NoiseMarker = new ProfilerMarker("GenerateNoise - Marker 1");
    static readonly ProfilerMarker NoiseMarker2 = new ProfilerMarker("GenerateNoise - Marker 2");

    public static float GenerateNoise(in Vector3 point, in NoiseParameters noiseParameters, in TerrainParameters terrainParameters)
    {
        return GenerateNoise(new float4(point.x, point.y, point.z, 0), noiseParameters, terrainParameters).x;
    }
    public static float4 GenerateNoise(in float4 point, in NoiseParameters noiseParameters, in TerrainParameters terrainParameters)
    {

        NoiseMarker.Begin();
        if (point.y < terrainParameters.SurfaceLevel)
        {
            NoiseMarker.End();
            return 1;
        }

        float sampleLevel = noiseParameters.SampleLevel;
        float seed = noiseParameters.SampleLevel;
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


        float tNoise = -1f + 2 * (noise / octave);

        //if (y < terrainParamaters.SeaLevel && tNoise < 0) {
        //    return new float4(0, .5f, 0, 0);
        //}

        // This should push the ending value into the range of -1 to 1, more or less, since noise could be slighty
        // below 0 or beyond 1.0
        NoiseMarker.End();
        return new float4(tNoise,0,0,0);


    }



}
