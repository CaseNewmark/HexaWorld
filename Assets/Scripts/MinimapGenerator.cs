using UnityEngine;
using System.Collections.Generic;

public class MinimapGenerator
{
    public static void GenerateMinimapTexture(List<HexTile> tiles, int minimapSize = 512, string fileName = "minimap.png")
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
        
        // Calculate appropriate hexagon size based on tile density
        float averageSpacing = scale / Mathf.Sqrt(tiles.Count);
        int hexRadius = Mathf.Max(2, Mathf.FloorToInt((averageSpacing / scale) * minimapSize * 0.4f));

        // Draw each tile on the minimap
        foreach (var tile in tiles)
        {
            // Convert world position to texture coordinates
            float normalizedX = (tile.position.x - minBounds.x) / scale;
            float normalizedZ = (tile.position.z - minBounds.z) / scale;
            
            int centerX = Mathf.FloorToInt(normalizedX * (minimapSize - 1));
            int centerY = Mathf.FloorToInt(normalizedZ * (minimapSize - 1));
            
            // Clamp to texture bounds
            centerX = Mathf.Clamp(centerX, hexRadius, minimapSize - hexRadius - 1);
            centerY = Mathf.Clamp(centerY, hexRadius, minimapSize - hexRadius - 1);
            
            Color tileColor = GetTileColor(tile.tileType);
            
            // Draw hexagon
            DrawHexagon(pixels, minimapSize, centerX, centerY, hexRadius, tileColor);
        }

        minimap.SetPixels(pixels);
        minimap.Apply();

        // Save the minimap as a PNG file
        byte[] pngData = minimap.EncodeToPNG();
        string path = Application.dataPath + "/" + fileName;
        System.IO.File.WriteAllBytes(path, pngData);
        
        Debug.Log($"Minimap saved to: {path}");
        
#if UNITY_EDITOR
        UnityEditor.AssetDatabase.Refresh();
#endif
        
        // Clean up
        Object.DestroyImmediate(minimap);
    }

    private static void DrawHexagon(Color[] pixels, int textureSize, int centerX, int centerY, int radius, Color color)
    {
        // Calculate hexagon vertices (flat-top orientation)
        Vector2[] vertices = new Vector2[6];
        
        for (int i = 0; i < 6; i++)
        {
            float angle = (i * 60f - 30f) * Mathf.Deg2Rad; // Start at -30 degrees for flat-top
            vertices[i] = new Vector2(
                centerX + radius * Mathf.Cos(angle),
                centerY + radius * Mathf.Sin(angle)
            );
        }
        
        // Find bounding box
        int minX = Mathf.FloorToInt(vertices[0].x);
        int maxX = Mathf.CeilToInt(vertices[0].x);
        int minY = Mathf.FloorToInt(vertices[0].y);
        int maxY = Mathf.CeilToInt(vertices[0].y);
        
        for (int i = 1; i < 6; i++)
        {
            minX = Mathf.Min(minX, Mathf.FloorToInt(vertices[i].x));
            maxX = Mathf.Max(maxX, Mathf.CeilToInt(vertices[i].x));
            minY = Mathf.Min(minY, Mathf.FloorToInt(vertices[i].y));
            maxY = Mathf.Max(maxY, Mathf.CeilToInt(vertices[i].y));
        }
        
        // Clamp to texture bounds
        minX = Mathf.Max(0, minX);
        maxX = Mathf.Min(textureSize - 1, maxX);
        minY = Mathf.Max(0, minY);
        maxY = Mathf.Min(textureSize - 1, maxY);
        
        // Fill hexagon using point-in-polygon test
        for (int y = minY; y <= maxY; y++)
        {
            for (int x = minX; x <= maxX; x++)
            {
                if (IsPointInHexagon(new Vector2(x, y), vertices))
                {
                    int index = y * textureSize + x;
                    if (index >= 0 && index < pixels.Length)
                    {
                        pixels[index] = color;
                    }
                }
            }
        }
    }
    
    private static bool IsPointInHexagon(Vector2 point, Vector2[] vertices)
    {
        bool inside = false;
        int j = vertices.Length - 1;
        
        for (int i = 0; i < vertices.Length; i++)
        {
            if (((vertices[i].y > point.y) != (vertices[j].y > point.y)) &&
                (point.x < (vertices[j].x - vertices[i].x) * (point.y - vertices[i].y) / (vertices[j].y - vertices[i].y) + vertices[i].x))
            {
                inside = !inside;
            }
            j = i;
        }
        
        return inside;
    }

    private static Color GetTileColor(TileType tileType)
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
}
