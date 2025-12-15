using System.Collections.Generic;
using UnityEngine;

public class RoadGridManager : MonoBehaviour
{
    [Header("Construction Zone Settings")]
    public Transform BuildAreaCenter;
    public float BuildRadius = 200f;

    [Header("Terrain Settings")]
    public LayerMask TerrainLayer; 
    public float SpawnHeightOffset = 0.1f;

    //Internal Data Structure
    private class GridCell
    {
        public PrefabData PlacedPrefabData;
        public GameObject PlacedInstance;
        public Vector2Int RootPosition; // Position of the prefab's pivot
    }
    private Dictionary<Vector2Int, GridCell> gridData = new Dictionary<Vector2Int, GridCell>();

    // Verify if an area is free for placement
    public bool IsAreaFree(Vector2Int position, Vector2Int size) // Center position and size
    {
        int startX = position.x - (size.x / 2);
        int startY = position.y - (size.y / 2);

        for (int x = 0; x < size.x; x++) // Iterate through area
        {
            for (int y = 0; y < size.y; y++) // Check each cell
            {
                Vector2Int cellCoord = new Vector2Int(startX + x, startY + y);
                
                // 1. Is already occupied?
                if (gridData.ContainsKey(cellCoord)) return false;
                
                // 2. Is inside build area?
                if (!IsCellInsideBuildArea(cellCoord)) return false;
            }
        }
        return true; 
    }

    public void PlacePrefab(PrefabData data, Vector2Int position, Quaternion rotation) // Places prefab at position
    {
        if (!_isHeightCalculated) CalculateFlatZoneHeight(); // Ensure height is calculated

        Vector3 worldPosition = new Vector3(position.x, _flatZoneHeight + SpawnHeightOffset, position.y); // Convert to world position
        GameObject instance = Instantiate(data.Prefab, worldPosition, rotation); // Instantiate prefab
        
        GridCell cell = new GridCell { 
            PlacedPrefabData = data, 
            PlacedInstance = instance,
            RootPosition = position
        };// Create grid cell entry

        // Mark all occupied cells
        int startX = position.x - (data.Size.x / 2);
        int startY = position.y - (data.Size.y / 2);

        for (int x = 0; x < data.Size.x; x++)
        {
            for (int y = 0; y < data.Size.y; y++)
            {
                Vector2Int cellCoord = new Vector2Int(startX + x, startY + y);
                if(!gridData.ContainsKey(cellCoord))
                {
                    gridData[cellCoord] = cell; 
                }
            }
        }
    }

    public void MarkAreaOccupied(Vector2Int center, Vector2Int size) // Marks area as occupied without placing prefab
    {
        int startX = center.x - (size.x / 2);
        int startY = center.y - (size.y / 2);

        for (int x = 0; x < size.x; x++)
        {
            for (int y = 0; y < size.y; y++)
            {
                Vector2Int pos = new Vector2Int(startX + x, startY + y); // Calculate cell position
                if (!gridData.ContainsKey(pos))
                {
                    gridData.Add(pos, new GridCell { PlacedInstance = null, PlacedPrefabData = null }); 
                }
            }
        }
    }

    public void RemovePrefabAt(Vector2Int position) // Removes prefab at position
    {
        if (gridData.TryGetValue(position, out GridCell cell))
        {
            // Destroy the instance if it exists
            if(cell.PlacedInstance != null) Destroy(cell.PlacedInstance);
            
            // Search and remove all occupied cells
            List<Vector2Int> keysToRemove = new List<Vector2Int>();
            foreach (var pair in gridData) 
            { 
                // Verify if the cell belongs to the same prefab instance
                if (pair.Value == cell || (cell.PlacedInstance != null && pair.Value.PlacedInstance == cell.PlacedInstance)) 
                {
                    keysToRemove.Add(pair.Key); 
                }
            }
            foreach (var key in keysToRemove) { gridData.Remove(key); }
        }
    }

    public GameObject GetPrefabAt(Vector2Int position) // Returns prefab instance at position
    {
        if (gridData.TryGetValue(position, out GridCell cell)) return cell.PlacedInstance; 
        return null;
    }

    private float _flatZoneHeight = 0f; // Cached height of the flat build zone
    private bool _isHeightCalculated = false; // Flag to check if height is calculated

    private void CalculateFlatZoneHeight() // Raycasts down to find terrain height
    {
        if (BuildAreaCenter == null) return;
        Vector3 centerPos = BuildAreaCenter.position;
        Vector3 rayOrigin = new Vector3(centerPos.x, 1000f, centerPos.z);
        if (Physics.Raycast(rayOrigin, Vector3.down, out RaycastHit hit, 2000f, TerrainLayer))
        {
            _flatZoneHeight = hit.point.y;
            _isHeightCalculated = true;
        }
    }

    private bool IsCellInsideBuildArea(Vector2Int cellCoord) // Checks if cell is within build radius
    {
        if (BuildAreaCenter == null) return false;
        Vector2 pos = new Vector2(cellCoord.x, cellCoord.y);
        Vector3 center3D = BuildAreaCenter.position;
        Vector2 center = new Vector2(center3D.x, center3D.z);
        return Vector2.Distance(pos, center) < BuildRadius;
    }
}