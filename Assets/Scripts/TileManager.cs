using UnityEngine;
using System.Collections.Generic;
using EasyButtons;

public class TileManager : MonoBehaviour
{
    [Header("Tile Settings")]
    [SerializeField] private GameObject tilePrefab;
    [SerializeField] private int minHeight = 0;
    [SerializeField] private int maxHeight = 10;
    
    [Header("Noise Settings")]
    [SerializeField] private float noiseScale = 0.1f;
    [SerializeField] private int octaves = 4;
    [SerializeField] private float persistence = 0.5f;
    [SerializeField] private float lacunarity = 2f;
    [SerializeField] private Vector2 noiseOffset = Vector2.zero;
    [SerializeField] private bool useRandomSeed = true;
    [SerializeField] private int seed = 12345;
    
    private List<HexTile> tiles = new List<HexTile>();
    private System.Random randomGenerator;
    
    public List<HexTile> Tiles => tiles;
    
    private void Awake()
    {
        InitializeNoise();
    }
    
    private void InitializeNoise()
    {
        int actualSeed = useRandomSeed ? Random.Range(0, 100000) : seed;
        randomGenerator = new System.Random(actualSeed);
        Debug.Log($"Terrain generation using seed: {actualSeed}");
    }
    
    public void ClearTiles()
    {
        foreach (var tile in tiles)
        {
            if (tile.gameObject != null)
            {
#if UNITY_EDITOR
                DestroyImmediate(tile.gameObject);
#else
                Destroy(tile.gameObject);
#endif
            }
        }
        tiles.Clear();
    }
    
    public HexTile CreateTile(Vector3 position, Vector2Int hexCoordinates, Transform parent)
    {
        if (tilePrefab == null)
        {
            Debug.LogError("Tile prefab is not assigned!");
            return null;
        }
        
        // No rotation needed - new Blender export has correct orientation
        Quaternion rotation = Quaternion.identity;
        GameObject gameObject = Instantiate(tilePrefab, position, rotation, parent);
        
        // Generate height using noise pattern for coherent biomes
        int noiseHeight = GenerateNoiseHeight(position.x, position.z, hexCoordinates.x, hexCoordinates.y);
        TileHeightModifier.SetHeight(gameObject, noiseHeight);
        
        // Determine tile type based on height
        TileType tileType = TileHeightModifier.GetTileTypeFromHeight(noiseHeight);
        
        // Debug first few tiles to check height distribution
        if (tiles.Count < 5)
        {
            Debug.Log($"Tile {tiles.Count}: Height={noiseHeight}, Type={tileType}, Position=({position.x:F2},{position.z:F2})");
        }
        
        // Create and store tile information
        HexTile hexTile = new HexTile(position, hexCoordinates, tileType, noiseHeight, gameObject);
        tiles.Add(hexTile);
        
        return hexTile;
    }
    
    private int GenerateNoiseHeight(float worldX, float worldZ, int hexQ, int hexR)
    {
        float noiseValue = 0f;
        float amplitude = 1f;
        float frequency = noiseScale;
        float maxValue = 0f; // Used for normalizing
        
        // Use hex coordinates to add variation and break symmetry
        float hexVariation = (hexQ * 0.1234f + hexR * 0.5678f) % 1.0f;
        float offsetX = noiseOffset.x + hexVariation;
        float offsetZ = noiseOffset.y + (hexQ + hexR) * 0.01f;
        
        // Apply multiple octaves of noise for more natural terrain
        for (int i = 0; i < octaves; i++)
        {
            float sampleX = (worldX + offsetX) * frequency;
            float sampleZ = (worldZ + offsetZ) * frequency;
            
            float octaveValue = Mathf.PerlinNoise(sampleX, sampleZ);
            // Apply some variation to each octave based on hex coordinates
            float coordModifier = 1.0f + (hexQ % 3 - 1) * 0.1f + (hexR % 3 - 1) * 0.1f;
            octaveValue = Mathf.Pow(octaveValue * coordModifier, 1.1f);
            noiseValue += octaveValue * amplitude;
            
            maxValue += amplitude; // Track maximum possible value
            amplitude *= persistence;
            frequency *= lacunarity;
        }
        
        // Normalize to 0-1 range based on actual possible maximum
        noiseValue = noiseValue / maxValue;
        
        // Apply a curve to create better distribution
        noiseValue = Mathf.Pow(noiseValue, 0.75f);
        noiseValue = Mathf.Clamp01(noiseValue);
        
        int height = Mathf.RoundToInt(noiseValue * (maxHeight - minHeight) + minHeight);
        
        return height;
    }
    
    public HexTile GetTileAt(Vector2Int hexCoordinates)
    {
        return tiles.Find(tile => tile.hexCoordinates == hexCoordinates);
    }
    
    public List<HexTile> GetTilesByType(TileType tileType)
    {
        return tiles.FindAll(tile => tile.tileType == tileType);
    }
    
    public int GetTileCount()
    {
        return tiles.Count;
    }
    
    [Button]
    public void RegenerateNoise()
    {
        InitializeNoise();
    }
}
