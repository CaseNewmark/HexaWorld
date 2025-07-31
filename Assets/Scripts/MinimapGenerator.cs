using UnityEngine;
using System.Collections.Generic;

public class MinimapGenerator
{
    public static void GenerateMinimapTexture(List<HexTile> tiles, int minimapSize = 512, string fileName = "minimap.png")
    {
        if (tiles.Count == 0) 
        {
            Debug.LogWarning("No tiles provided for minimap generation!");
            return;
        }

        Debug.Log($"Generating minimap with {tiles.Count} tiles, size: {minimapSize}x{minimapSize}");

        Texture2D minimap = new Texture2D(minimapSize, minimapSize);
        Color[] pixels = new Color[minimapSize * minimapSize];
        
        // Initialize with black background
        Color backgroundColor = Color.black;
        for (int i = 0; i < pixels.Length; i++)
        {
            pixels[i] = backgroundColor;
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
        
        Debug.Log($"Bounds: min({minBounds.x:F2}, {minBounds.z:F2}) max({maxBounds.x:F2}, {maxBounds.z:F2})");
        Debug.Log($"Scale: {scale:F2}, Width: {width:F2}, Height: {height:F2}");
        
        // Calculate appropriate hexagon size based on tile density
        float averageSpacing = scale / Mathf.Sqrt(tiles.Count);
        int hexRadius = Mathf.Max(3, Mathf.FloorToInt((averageSpacing / scale) * minimapSize * 0.6f));
        
        Debug.Log($"Hex radius: {hexRadius}, Average spacing: {averageSpacing:F2}");

        // Draw each tile on the minimap
        int tilesDrawn = 0;
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
            
            // Debug first few tiles
            if (tilesDrawn < 3)
            {
                Debug.Log($"Tile {tilesDrawn}: Type={tile.tileType}, Color={tileColor}, Center=({centerX},{centerY}), WorldPos=({tile.position.x:F2},{tile.position.z:F2})");
            }
            
            // Draw hexagon
            DrawHexagon(pixels, minimapSize, centerX, centerY, hexRadius, tileColor);
            tilesDrawn++;
        }
        
        Debug.Log($"Drew {tilesDrawn} tiles on minimap");
        
        // Count tile types for debugging
        var typeCounts = new System.Collections.Generic.Dictionary<TileType, int>();
        foreach (var tile in tiles)
        {
            if (!typeCounts.ContainsKey(tile.tileType))
                typeCounts[tile.tileType] = 0;
            typeCounts[tile.tileType]++;
        }
        
        foreach (var kvp in typeCounts)
        {
            Debug.Log($"Tile type {kvp.Key}: {kvp.Value} tiles");
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
        
        int pixelsDrawn = 0;
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
                        pixelsDrawn++;
                    }
                }
            }
        }
        
        // Debug first few hexagons
        if (centerX < 50 && centerY < 50)
        {
            Debug.Log($"Drew hexagon at ({centerX},{centerY}) with radius {radius}, color {color}, pixels drawn: {pixelsDrawn}");
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
