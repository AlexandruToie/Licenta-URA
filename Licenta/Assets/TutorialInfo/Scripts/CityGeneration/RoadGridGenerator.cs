using System.Collections.Generic;
using UnityEngine;

public class RoadGridManager : MonoBehaviour
{
    [Header("Build Area Settings")]
    [Tooltip("Transformer that defines the center of the build area.")]
    public Transform BuildAreaCenter;

    [Tooltip("The main terrain in the scene.")]
    public Terrain MainTerrain; 

    [Tooltip("Radius of the build area around the center.")]
    public float BuildRadius = 200f;

    [Tooltip("Height offset when spawning prefabs.")]
    public float SpawnHeightOffset = 1f;

    private float sampledFlatHeight = 0f;

    private class GridCell
    {
        public PrefabData PlacedPrefabData;
        public GameObject PlacedInstance;
    }
    private Dictionary<Vector2Int, GridCell> gridData = new Dictionary<Vector2Int, GridCell>();
    public void InitializeTerrainHeight()
    {
        if (MainTerrain == null || BuildAreaCenter == null)
        {
            Debug.LogError("[RoadGen] ATENTION: MainTerrain or BuildAreaCenter is not assigned!");
            return;
        }

        Vector3 centerPos = BuildAreaCenter.position;

        sampledFlatHeight = MainTerrain.SampleHeight(centerPos);
        
        Debug.Log($"[RoadGen] The high of the terrain was found at: {sampledFlatHeight}");
    }

    public bool IsAreaFree(Vector2Int position, Vector2Int size)
    {
        for (int x = 0; x < size.x; x++)
        {
            for (int y = 0; y < size.y; y++)
            {
                Vector2Int cellCoord = new Vector2Int(position.x + x, position.y + y);
                if (gridData.ContainsKey(cellCoord) || !IsCellInsideBuildArea(cellCoord))
                {
                    return false; 
                }
            }
        }
        return true; 
    }

    public void PlacePrefab(PrefabData data, Vector2Int position, Quaternion rotation)
    {

        float heightY = sampledFlatHeight;

        Vector3 worldPosition = new Vector3(position.x, heightY + SpawnHeightOffset, position.y);

        GameObject instance = Instantiate(data.Prefab, worldPosition, rotation);
        
        GridCell cell = new GridCell
        {
            PlacedPrefabData = data,
            PlacedInstance = instance
        };

        for (int x = 0; x < data.Size.x; x++)
        {
            for (int y = 0; y < data.Size.y; y++)
            {
                Vector2Int cellCoord = new Vector2Int(position.x + x, position.y + y);
                gridData[cellCoord] = cell; 
            }
        }
    }

    #region Modified Functions
    public void RemovePrefabAt(Vector2Int position)
    {
        if (gridData.TryGetValue(position, out GridCell cell))
        {
            Destroy(cell.PlacedInstance);
            
            List<Vector2Int> keysToRemove = new List<Vector2Int>();
            foreach (var pair in gridData)
            {
                if (pair.Value.PlacedInstance == cell.PlacedInstance)
                {
                    keysToRemove.Add(pair.Key);
                }
            }
            
            foreach (var key in keysToRemove)
            {
                gridData.Remove(key);
            }
        }
    }

    public GameObject GetPrefabAt(Vector2Int position)
    {
        if (gridData.TryGetValue(position, out GridCell cell))
        {
            return cell.PlacedInstance;
        }
        return null;
    }
    #endregion

    private bool IsCellInsideBuildArea(Vector2Int cellCoord)
    {
        if (BuildAreaCenter == null)
        {
            return false;
        }

        Vector2 pos = new Vector2(cellCoord.x, cellCoord.y);
        Vector3 center3D = BuildAreaCenter.position;
        Vector2 center = new Vector2(center3D.x, center3D.z);
        
        return Vector2.Distance(pos, center) < BuildRadius;
    }
    
    private void OnDrawGizmosSelected()
    {
        if (BuildAreaCenter != null)
        {
            float yPos = Application.isPlaying ? sampledFlatHeight : BuildAreaCenter.position.y;
            
            Vector3 center = new Vector3(BuildAreaCenter.position.x, yPos, BuildAreaCenter.position.z);
            Gizmos.color = Color.cyan;
            Vector3 from = center + new Vector3(BuildRadius, 0, 0);
            for (int i = 1; i <= 36; i++)
            {
                float angle = i * 10f * Mathf.Deg2Rad;
                Vector3 to = center + new Vector3(Mathf.Cos(angle) * BuildRadius, 0, Mathf.Sin(angle) * BuildRadius);
                Gizmos.DrawLine(from, to);
                from = to;
            }
        }
    }
}