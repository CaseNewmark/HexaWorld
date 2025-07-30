using UnityEngine;
using EasyButtons;
using System.Collections.Generic;
using System;

[System.Serializable]
public class HexTile
{
    public Vector3 position;
    public TileType tileType;
    public int height;
    public GameObject gameObject;
    
    public HexTile(Vector3 pos, TileType type, int h, GameObject go)
    {
        position = pos;
        tileType = type;
        height = h;
        gameObject = go;
    }
}

public enum TileType
{
    Water,      // Height 0-1
    Beach,      // Height 2
    Grassland,  // Height 3-4
    Forest,     // Height 5-6
    Hills,      // Height 7-8
    Mountain    // Height 9+
}

public class LevelBuilder : MonoBehaviour
{
    [SerializeField] private GameObject tilePrefab;
    [SerializeField] private int gridRadius = 3;
    [SerializeField] private float tileSize = 1.73f; // Approximate size for hexagonal spacing
    [SerializeField] private int minHeight = 0;
    [SerializeField] private int maxHeight = 10;
    
    [Header("Minimap")]
    [SerializeField] private int minimapSize = 256;
    [SerializeField] private bool generateMinimap = true;
    
    // Collection to store all tiles
    private List<HexTile> tiles = new List<HexTile>();

    [Button]
    public void BuildLevel()
    {
        var children = new List<GameObject>();
        foreach (Transform child in transform)
        {
            children.Add(child.gameObject);
        }
#if UNITY_EDITOR
        children.ForEach(child => GameObject.DestroyImmediate(child));
#else
        children.ForEach(child => GameObject.Destroy(child.gameObject));
#endif
        
        // Clear existing tiles
        tiles.Clear();
        
        CreateHexagonalGrid();
        
        if (generateMinimap)
        {
            GenerateMinimap();
        }
    }

    private void CreateHexagonalGrid()
    {
        for (int q = -gridRadius; q <= gridRadius; q++)
        {
            int r1 = Mathf.Max(-gridRadius, -q - gridRadius);
            int r2 = Mathf.Min(gridRadius, -q + gridRadius);
            
            for (int r = r1; r <= r2; r++)
            {
                // Calculate actual Euclidean distance from center for true circular boundary
                Vector3 worldPos = HexToWorldPosition(q, r);
                Vector3 centerPos = this.transform.position;
                float distance = Vector3.Distance(new Vector3(worldPos.x, 0, worldPos.z), new Vector3(centerPos.x, 0, centerPos.z));
                float maxRadius = gridRadius * tileSize * 0.9f; // Convert grid radius to world units
                
                // Only create tile if it's within the circular boundary
                if (distance <= maxRadius)
                {
                    CreateTile(worldPos, q, r);
                }
            }
        }
    }

    private Vector3 HexToWorldPosition(int q, int r)
    {
        float x = tileSize * (3f / 2f * q);
        float z = tileSize * (Mathf.Sqrt(3f) / 2f * q + Mathf.Sqrt(3f) * r);
        return this.transform.position + new Vector3(x, 0, z);
    }

    private void CreateTile(Vector3 position, int q, int r)
    {
        // Rotate 90 degrees around X-axis to lay flat, then 30 degrees around Y for proper hex orientation
        Quaternion rotation = Quaternion.Euler(-90f, 30f, 0f);
        var gameObject = Instantiate(tilePrefab, position, rotation, this.transform);
        
        // Set random height for each tile
        int randomHeight = UnityEngine.Random.Range(minHeight, maxHeight + 1);
        SetHeight(gameObject, randomHeight);
        
        // Determine tile type based on height
        TileType tileType = GetTileTypeFromHeight(randomHeight);
        
        // Store tile information
        HexTile hexTile = new HexTile(position, tileType, randomHeight, gameObject);
        tiles.Add(hexTile);
    }

    private TileType GetTileTypeFromHeight(int height)
    {
        if (height <= 1) return TileType.Water;
        if (height == 2) return TileType.Beach;
        if (height <= 4) return TileType.Grassland;
        if (height <= 6) return TileType.Forest;
        if (height <= 8) return TileType.Hills;
        return TileType.Mountain;
    }

    private void SetHeight(GameObject gameObject, int height)
    {
        MeshFilter meshFilter = gameObject.GetComponent<MeshFilter>();
        if (meshFilter == null || meshFilter.sharedMesh == null) return;
        
        // Create an instance of the mesh to avoid modifying the original asset
        Mesh mesh = Instantiate(meshFilter.sharedMesh);
        meshFilter.mesh = mesh;
        
        Vector3[] vertices = mesh.vertices;
        
        if (vertices.Length == 0) return;
        
        Transform transform = gameObject.transform;
        
        // Convert vertices to world space to find the actual lowest point
        Vector3[] worldVertices = new Vector3[vertices.Length];
        for (int i = 0; i < vertices.Length; i++)
        {
            worldVertices[i] = transform.TransformPoint(vertices[i]);
        }
        
        // Find the lowest world Y coordinate
        float minWorldY = float.MaxValue;
        for (int i = 0; i < worldVertices.Length; i++)
        {
            if (worldVertices[i].y < minWorldY)
                minWorldY = worldVertices[i].y;
        }
        
        // Move vertices up in world space, then convert back to local space
        float heightOffset = height * 0.1f;
        for (int i = 0; i < vertices.Length; i++)
        {
            // Check if this vertex is at the lowest world position
            if (Mathf.Approximately(worldVertices[i].y, minWorldY) == false)
            {
                // Move up in world space
                Vector3 worldVertex = worldVertices[i] + Vector3.up * heightOffset;
                // Convert back to local space
                vertices[i] = transform.InverseTransformPoint(worldVertex);
            }
        }
        
        // Apply the modified vertices back to the mesh
        mesh.vertices = vertices;
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
    }

    private void GenerateMinimap()
    {
        if (tiles.Count == 0) return;

        Texture2D minimap = new Texture2D(minimapSize, minimapSize);
        Color[] pixels = new Color[minimapSize * minimapSize];
        
        // Initialize with transparent pixels
        for (int i = 0; i < pixels.Length; i++)
        {
            pixels[i] = Color.clear;
        }

        // Find bounds of the tile area
        Vector3 minBounds = Vector3.positiveInfinity;
        Vector3 maxBounds = Vector3.negativeInfinity;
        
        foreach (var tile in tiles)
        {
            if (tile.position.x < minBounds.x) minBounds.x = tile.position.x;
            if (tile.position.z < minBounds.z) minBounds.z = tile.position.z;
            if (tile.position.x > maxBounds.x) maxBounds.x = tile.position.x;
            if (tile.position.z > maxBounds.z) maxBounds.z = tile.position.z;
        }

        float width = maxBounds.x - minBounds.x;
        float height = maxBounds.z - minBounds.z;
        float scale = Mathf.Max(width, height);

        // Draw each tile on the minimap
        foreach (var tile in tiles)
        {
            // Convert world position to texture coordinates
            float normalizedX = (tile.position.x - minBounds.x) / scale;
            float normalizedZ = (tile.position.z - minBounds.z) / scale;
            
            int pixelX = Mathf.FloorToInt(normalizedX * (minimapSize - 1));
            int pixelY = Mathf.FloorToInt(normalizedZ * (minimapSize - 1));
            
            // Clamp to texture bounds
            pixelX = Mathf.Clamp(pixelX, 0, minimapSize - 1);
            pixelY = Mathf.Clamp(pixelY, 0, minimapSize - 1);
            
            Color tileColor = GetTileColor(tile.tileType);
            
            // Draw a small area for each tile (3x3 pixels)
            for (int dx = -1; dx <= 1; dx++)
            {
                for (int dy = -1; dy <= 1; dy++)
                {
                    int x = pixelX + dx;
                    int y = pixelY + dy;
                    
                    if (x >= 0 && x < minimapSize && y >= 0 && y < minimapSize)
                    {
                        int index = y * minimapSize + x;
                        pixels[index] = tileColor;
                    }
                }
            }
        }

        minimap.SetPixels(pixels);
        minimap.Apply();

        // Save the minimap as a PNG file
        byte[] pngData = minimap.EncodeToPNG();
        string path = Application.dataPath + "/minimap.png";
        System.IO.File.WriteAllBytes(path, pngData);
        
        Debug.Log($"Minimap saved to: {path}");
        
#if UNITY_EDITOR
        UnityEditor.AssetDatabase.Refresh();
#endif
    }

    private Color GetTileColor(TileType tileType)
    {
        switch (tileType)
        {
            case TileType.Water: return new Color(0.2f, 0.4f, 0.8f, 1f);      // Blue
            case TileType.Beach: return new Color(0.9f, 0.8f, 0.6f, 1f);      // Sandy yellow
            case TileType.Grassland: return new Color(0.3f, 0.7f, 0.2f, 1f);  // Green
            case TileType.Forest: return new Color(0.1f, 0.4f, 0.1f, 1f);     // Dark green
            case TileType.Hills: return new Color(0.6f, 0.5f, 0.3f, 1f);      // Brown
            case TileType.Mountain: return new Color(0.7f, 0.7f, 0.7f, 1f);   // Gray
            default: return Color.white;
        }
    }

    [Button]
    public void RegenerateMinimap()
    {
        if (tiles.Count > 0)
        {
            GenerateMinimap();
        }
        else
        {
            Debug.LogWarning("No tiles to generate minimap from. Build level first.");
        }
    }
}
