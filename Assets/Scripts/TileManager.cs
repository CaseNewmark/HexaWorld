using UnityEngine;
using System.Collections.Generic;

public class TileManager : MonoBehaviour
{
    [Header("Tile Settings")]
    [SerializeField] private GameObject tilePrefab;
    [SerializeField] private int minHeight = 0;
    [SerializeField] private int maxHeight = 10;
    
    private List<HexTile> tiles = new List<HexTile>();
    
    public List<HexTile> Tiles => tiles;
    
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
        
        // Rotate 90 degrees around X-axis to lay flat, then 30 degrees around Y for proper hex orientation
        Quaternion rotation = Quaternion.Euler(-90f, 30f, 0f);
        GameObject gameObject = Instantiate(tilePrefab, position, rotation, parent);
        
        // Set random height for each tile
        int randomHeight = Random.Range(minHeight, maxHeight + 1);
        TileHeightModifier.SetHeight(gameObject, randomHeight);
        
        // Determine tile type based on height
        TileType tileType = TileHeightModifier.GetTileTypeFromHeight(randomHeight);
        
        // Create and store tile information
        HexTile hexTile = new HexTile(position, hexCoordinates, tileType, randomHeight, gameObject);
        tiles.Add(hexTile);
        
        return hexTile;
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
}
