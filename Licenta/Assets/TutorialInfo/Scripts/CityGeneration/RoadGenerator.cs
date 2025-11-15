using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(RoadGridManager))] // We make sure there's a RoadGridManager on the same GameObject
public class RoadGenerator : MonoBehaviour
{
    [Tooltip("The gird manager.")]
    private RoadGridManager gridManager;

    [Header("Prefab List")]
    [Tooltip("List of Point of Interest (POI) prefabs to place.")]
    public List<PrefabData> poiPrefabs;
    
    // (We will add more prefab lists later for roads, intersections, etc.)

    [Header("Settings for POI Placement")]
    [Tooltip("Number of POIs to attempt to place on the map.")]
    public int NumberOfPOIs = 30;
    
    [Tooltip("How many attempts to find a placement for each POI before giving up.")]
    public int MaxPlacementAttemptsPerPOI = 10;
    
    // A list to keep track of placed POIs
    private List<PlacedPOI> placedPOIs = new List<PlacedPOI>();

    // One simple class to track placed POIs
    private class PlacedPOI
    {
        public Vector2Int GridPosition;
        public PrefabData Data;
    }

    void Start()
    {
        // We acquire the grid manager reference
        gridManager = GetComponent<RoadGridManager>();
        gridManager.InitializeTerrainHeight();
        StartGeneration();
    }

    void StartGeneration()
    {
        PlaceAllPOIs();
    }

    private void PlaceAllPOIs()
    {
        Debug.Log("[RoadGen] Work Phase 1: Placing POIs...");
        
        // We get the build area parameters
        float radius = gridManager.BuildRadius;
        Vector3 center3D = gridManager.BuildAreaCenter.position;
        Vector2Int center = new Vector2Int((int)center3D.x, (int)center3D.z);

        int placedCount = 0;
        for (int i = 0; i < NumberOfPOIs; i++)
        {
            // 1. We pick a random POI prefab
            PrefabData randomPOI = poiPrefabs[Random.Range(0, poiPrefabs.Count)];

            // 2. We try to place it
            bool placed = false;
            for (int attempt = 0; attempt < MaxPlacementAttemptsPerPOI; attempt++)
            {
                // 3. We pick a random point in the build area
                Vector2 randomPoint = Random.insideUnitCircle * radius;
                
                // We convert to grid coordinates
                Vector2Int randomCoord = new Vector2Int(
                    Mathf.RoundToInt(center.x + randomPoint.x),
                    Mathf.RoundToInt(center.y + randomPoint.y)
                );

                // 4. We check if the area is free
                if (gridManager.IsAreaFree(randomCoord, randomPOI.Size))
                {
                    // 5. We place the prefab
                    Quaternion rotation = Quaternion.identity; // Deocamdată, fără rotație
                    gridManager.PlacePrefab(randomPOI, randomCoord, rotation);

                    // 6. We record the placed POI
                    placedPOIs.Add(new PlacedPOI { GridPosition = randomCoord, Data = randomPOI });
                    
                    placed = true;
                    placedCount++;
                    break; // We exit the attempt loop
                }
            }
            
            if (!placed)
            {
                Debug.LogWarning($"[RoadGen] We coudent find {randomPOI.name} after {MaxPlacementAttemptsPerPOI} tryes.");
            }
        }
        
        Debug.Log($"[RoadGen] Fase 1 over: We placed {placedCount} / {NumberOfPOIs} POI-uri.");
    }
}