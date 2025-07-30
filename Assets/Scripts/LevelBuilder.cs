using UnityEngine;
using EasyButtons;
using System.Collections.Generic;

public class LevelBuilder : MonoBehaviour
{
    [Header("Grid Settings")]
    [SerializeField] private int gridRadius = 3;
    [SerializeField] private float tileSize = 1.73f;
    
    [Header("Minimap")]
    [SerializeField] private int minimapSize = 512;
    [SerializeField] private bool generateMinimap = true;
    
    [Header("Components")]
    [SerializeField] private TileManager tileManager;
    
    private HexGrid hexGrid;
    
    private void Awake()
    {
        ValidateComponents();
    }
    
    private void ValidateComponents()
    {
        if (tileManager == null)
            tileManager = GetComponent<TileManager>();
        
        if (tileManager == null)
        {
            Debug.LogWarning("TileManager component not found. Adding one automatically.");
            tileManager = gameObject.AddComponent<TileManager>();
        }
        
        hexGrid = new HexGrid(tileSize, gridRadius);
    }

    [Button]
    public void BuildLevel()
    {
        // Ensure components are initialized
        if (tileManager == null)
        {
            tileManager = GetComponent<TileManager>();
            if (tileManager == null)
            {
                Debug.LogError("TileManager component not found! Please add a TileManager component to this GameObject.");
                return;
            }
        }
        
        if (hexGrid == null)
        {
            hexGrid = new HexGrid(tileSize, gridRadius);
        }
        
        // Clear existing tiles
        tileManager.ClearTiles();
        
        CreateHexagonalGrid();
        
        if (generateMinimap)
        {
            GenerateMinimap();
        }
    }

    private void CreateHexagonalGrid()
    {
        if (hexGrid == null)
        {
            Debug.LogError("HexGrid is not initialized!");
            return;
        }
        
        if (tileManager == null)
        {
            Debug.LogError("TileManager is not assigned!");
            return;
        }
        
        List<Vector2Int> coordinates = hexGrid.GenerateHexCoordinatesInCircle(transform.position);
        
        foreach (var coord in coordinates)
        {
            Vector3 worldPos = hexGrid.HexToWorldPosition(coord.x, coord.y, transform.position);
            tileManager.CreateTile(worldPos, coord, transform);
        }
    }

    private void GenerateMinimap()
    {
        MinimapGenerator.GenerateMinimapTexture(tileManager.Tiles, minimapSize);
    }

    [Button]
    public void RegenerateMinimap()
    {
        if (tileManager.GetTileCount() > 0)
        {
            GenerateMinimap();
        }
        else
        {
            Debug.LogWarning("No tiles to generate minimap from. Build level first.");
        }
    }
    
    // Public accessors for other scripts
    public List<HexTile> GetAllTiles() => tileManager.Tiles;
    public HexTile GetTileAt(Vector2Int hexCoordinates) => tileManager.GetTileAt(hexCoordinates);
    public List<HexTile> GetTilesByType(TileType tileType) => tileManager.GetTilesByType(tileType);
}
