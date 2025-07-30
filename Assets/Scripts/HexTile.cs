using UnityEngine;

[System.Serializable]
public class HexTile
{
    public Vector3 position;
    public Vector2Int hexCoordinates; // q, r coordinates
    public TileType tileType;
    public int height;
    public GameObject gameObject;
    
    public HexTile(Vector3 pos, Vector2Int hexCoords, TileType type, int h, GameObject go)
    {
        position = pos;
        hexCoordinates = hexCoords;
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
