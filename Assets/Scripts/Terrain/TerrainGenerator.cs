using UnityEngine;

public class TerrainGenerator : MonoBehaviour
{
    public float scale = 50f;
    public float heightMultiplier = 0.5f;
    public int octaves = 5;
    public float persistence = 0.5f;
    public float lacunarity = 2f;

    void Start()
    {
        GenerateTerrain();
    }

    void GenerateTerrain()
    {
        Terrain terrain = GetComponent<Terrain>();
        TerrainData terrainData = terrain.terrainData;

        if (terrainData == null)
        {
            Debug.LogError("No TerrainData found!");
            return;
        }

        int resolution = terrainData.heightmapResolution;
        float[,] heights = new float[resolution, resolution];

        for (int y = 0; y < resolution; y++)
        {
            for (int x = 0; x < resolution; x++)
            {
                heights[y, x] = GetNoiseValue(x, y, resolution);
            }
        }

        terrainData.SetHeights(0, 0, heights);
    }

    float GetNoiseValue(int x, int y, int resolution)
    {
        float amplitude = 1f;
        float frequency = 1f;
        float noiseValue = 0f;
        float maxValue = 0f;

        for (int i = 0; i < octaves; i++)
        {
            float xCoord = (float)x / resolution * scale * frequency;
            float yCoord = (float)y / resolution * scale * frequency;

            noiseValue += Mathf.PerlinNoise(xCoord, yCoord) * amplitude;
            maxValue += amplitude;

            amplitude *= persistence;
            frequency *= lacunarity;
        }

        return (noiseValue / maxValue) * heightMultiplier;
    }
}