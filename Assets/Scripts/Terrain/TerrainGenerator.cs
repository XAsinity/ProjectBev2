using UnityEngine;

public class TerrainGenerator : MonoBehaviour
{
    public int width = 256;
    public int height = 256;
    public float scale = 20f;

    void Start()
    {
        GenerateTerrain();
    }

    void GenerateTerrain()
    {
        Terrain terrain = GetComponent<Terrain>();
        terrain.terrainData = CreateTerrainData();
    }

    TerrainData CreateTerrainData()
    {
        TerrainData terrainData = new TerrainData();
        terrainData.heightmapResolution = width + 1;
        terrainData.size = new Vector3(width, 50, height);
        terrainData.SetHeights(0, 0, GenerateHeights());
        return terrainData;
    }

    float[,] GenerateHeights()
    {
        float[,] heights = new float[width, height];

        for (int x = 0; x < width; x++)
        {
            for (int z = 0; z < height; z++)
            {
                heights[x, z] = CalculateHeight(x, z);
            }
        }

        return heights;
    }

    float CalculateHeight(int x, int z)
    {
        float xCoord = (float)x / width * scale;
        float zCoord = (float)z / height * scale;
        return Mathf.PerlinNoise(xCoord, zCoord);
    }
}