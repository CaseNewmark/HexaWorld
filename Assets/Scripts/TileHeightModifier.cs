using UnityEngine;

public class TileHeightModifier : MonoBehaviour
{
    public static void SetHeight(GameObject gameObject, int height)
    {
        MeshFilter meshFilter = gameObject.GetComponent<MeshFilter>();
        if (meshFilter == null || meshFilter.sharedMesh == null) return;
        
        // Create an instance of the mesh to avoid modifying the original asset
        Mesh mesh = Object.Instantiate(meshFilter.sharedMesh);
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
    
    public static TileType GetTileTypeFromHeight(int height)
    {
        // Adjusted thresholds for better distribution with higher height values
        if (height <= 8) return TileType.Water;
        if (height <= 10) return TileType.Beach;
        if (height <= 13) return TileType.Grassland;
        if (height <= 16) return TileType.Forest;
        if (height <= 19) return TileType.Hills;
        return TileType.Mountain;
    }
}
