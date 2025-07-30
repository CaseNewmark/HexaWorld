using UnityEngine;
using System.Collections.Generic;

public class HexGrid
{
    public float tileSize;
    public int gridRadius;
    
    public HexGrid(float size, int radius)
    {
        tileSize = size;
        gridRadius = radius;
    }
    
    public Vector3 HexToWorldPosition(int q, int r, Vector3 centerPosition)
    {
        float x = tileSize * (3f / 2f * q);
        float z = tileSize * (Mathf.Sqrt(3f) / 2f * q + Mathf.Sqrt(3f) * r);
        return centerPosition + new Vector3(x, 0, z);
    }
    
    public List<Vector2Int> GenerateHexCoordinatesInCircle(Vector3 centerPosition, float radiusMultiplier = 0.9f)
    {
        List<Vector2Int> coordinates = new List<Vector2Int>();
        float maxRadius = gridRadius * tileSize * radiusMultiplier;
        
        for (int q = -gridRadius; q <= gridRadius; q++)
        {
            int r1 = Mathf.Max(-gridRadius, -q - gridRadius);
            int r2 = Mathf.Min(gridRadius, -q + gridRadius);
            
            for (int r = r1; r <= r2; r++)
            {
                Vector3 worldPos = HexToWorldPosition(q, r, centerPosition);
                float distance = Vector3.Distance(new Vector3(worldPos.x, 0, worldPos.z), 
                                                 new Vector3(centerPosition.x, 0, centerPosition.z));
                
                if (distance <= maxRadius)
                {
                    coordinates.Add(new Vector2Int(q, r));
                }
            }
        }
        
        return coordinates;
    }
}
