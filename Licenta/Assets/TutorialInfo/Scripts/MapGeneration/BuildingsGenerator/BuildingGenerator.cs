using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class BuildingGenerator : MonoBehaviour
{
    [Header("References")]
    public RoadGenerator roadGenerator;
    public RoadGridManager gridManager;
    public CityZoneVisualizer zoneVisualizer;

    [Header("Building Profiles")]
    public List<BuildingProfile> housePrefabs;
    public List<BuildingProfile> apartmentPrefabs;
    public List<BuildingProfile> factoryPrefabs;
    public List<BuildingProfile> servicePrefabs;

    [Header("Spawn Settings")]
    public float RaycastHeight = 100f;
    public LayerMask TerrainLayer;
    [Range(0, 100)] public int ServiceBuildingChance = 15;
    
    [Header("Spacing & Debug")]
    [Tooltip("Necessar padding (in cells) around buildings to keep them apart.")]
    public int CellPadding = 0; 
    public bool ShowDebugGizmos = true;
    public event System.Action OnBuildingsGenerated;

    private List<Vector2Int> constructionQueue = new List<Vector2Int>();
    private HashSet<Vector2Int> processedCells = new HashSet<Vector2Int>();

    [Header("Traffic Data")]
    public List<Vector2Int> InhabitedHousePositions = new List<Vector2Int>();
    
    // Debug Vizual
    private List<BoundsDebug> debugBounds = new List<BoundsDebug>();
    private struct BoundsDebug { public Vector3 center; public Vector3 size; public Color color; }

    void Start() // Subscribe to road generation event
    {
        if (roadGenerator != null) roadGenerator.OnGenerationFinished += StartBuildingGeneration;
    }

    private void OnDestroy()// Unsubscribe from event
    {
        if (roadGenerator != null) roadGenerator.OnGenerationFinished -= StartBuildingGeneration;
    }

    void StartBuildingGeneration()// Triggered when road generation is complete
    {
        // Debug.Log("[buildingGenerator] I start to generate buildings...");
        // debugBounds.Clear();
        StartCoroutine(GenerateBuildingsRoutine());
    }

    IEnumerator GenerateBuildingsRoutine() // Coroutine for building generation
    {
        ScanForBuildingLots();
        InhabitedHousePositions.Clear();
        yield return null;

        List<Vector2Int> currentQueue = new List<Vector2Int>(constructionQueue);
        
        // Process each lot in the queue
        foreach (Vector2Int lotPos in currentQueue)
        {
            // If the lot is no longer free, skip it
            if (!gridManager.IsAreaFree(lotPos, Vector2Int.one)) continue;

            TryBuildSmart(lotPos);
            
            if (currentQueue.IndexOf(lotPos) % 5 == 0) yield return null;
        }
        //Debug.Log("[BuildingGenerator] Finished generating buildings.");
        OnBuildingsGenerated?.Invoke();
    }

    private void TryBuildSmart(Vector2Int pos) // Try to place a building intelligently
    {
        List<Vector2Int> availableRoadDirs = GetAvailableRoadDirections(pos);
        if (availableRoadDirs.Count == 0) return;

        BuildingType targetType = DecideBuildingType(pos);
        
        //Select candidate buildings sorted by size (largest first)
        List<BuildingProfile> candidates = GetProfilesSortedBySize(targetType);
        if (candidates.Count == 0) return;

        foreach (BuildingProfile profile in candidates)
        {
            foreach (Vector2Int roadDir in availableRoadDirs)
            {
                // Rule number 1: Skip large buildings if the road in front is not straight
                // We calculate the area of the building
                int area = profile.Size.x * profile.Size.y;
                
                if (area > 1) 
                {
                    // If the area is greater than 1, we check if the road segment is straight
                    if (!IsRoadSegmentStraight(pos, roadDir)) 
                    {
                        continue; 
                    }
                }

                // Try to place the building
                if (AttemptPlacement(pos, profile, roadDir)) return;
            }
        }
    }

    // New function to check if the road segment is straight
    private bool IsRoadSegmentStraight(Vector2Int lotPos, Vector2Int roadDir)
    {
        Vector2Int roadPos = lotPos + roadDir; // Poziția drumului

        // Calculate tangent direction
        Vector2Int tangent = new Vector2Int(-roadDir.y, roadDir.x);

        // Verify if there are roads on both sides of the tangent
        bool roadLeft = roadGenerator.IsCellRoad(roadPos - tangent);
        bool roadRight = roadGenerator.IsCellRoad(roadPos + tangent);

        // Road is straight only if it has continuation in BOTH directions on that axis
        return (roadLeft && roadRight);
    }

    private bool AttemptPlacement(Vector2Int centerPos, BuildingProfile profile, Vector2Int roadDir) // Try to place a building at the specified position
    {
        float yRotation = GetRotationFromDirection(roadDir);
        Quaternion rotation = Quaternion.Euler(0, yRotation, 0);

        bool isSideways = (Mathf.Abs(yRotation - 90f) < 0.1f || Mathf.Abs(yRotation - 270f) < 0.1f);
        Vector2Int rotatedSize = isSideways ? new Vector2Int(profile.Size.y, profile.Size.x) : profile.Size;

        if (!IsSpaceStrictlyFree(centerPos, rotatedSize, CellPadding)) 
        {
            if(ShowDebugGizmos) AddDebugBox(centerPos, rotatedSize, Color.red);
            return false;
        }

        float terrainHeight = GetTerrainHeight(centerPos);
        if (terrainHeight == float.MinValue) return false;

        // We send a message to place the building instance
        PlaceBuildingInstance(profile, rotatedSize, centerPos, rotation, terrainHeight, roadDir);

        if(ShowDebugGizmos) AddDebugBox(centerPos, rotatedSize, Color.green);
        return true;
    }

    // Place the building instance in the grid
    private void PlaceBuildingInstance(BuildingProfile profile, Vector2Int occupiedSize, Vector2Int pos, Quaternion rot, float yPos, Vector2Int roadDir) 
    {
        Quaternion finalRot = rot * Quaternion.Euler(profile.RotationOffset);
        
        PrefabData runtimeData = new PrefabData { Prefab = profile.Prefab, Size = occupiedSize };
        gridManager.PlacePrefab(runtimeData, pos, finalRot);
        
        GameObject instance = gridManager.GetPrefabAt(pos);
        if (instance != null)
        {
            Vector3 finalPos = instance.transform.position;
            finalPos.y = yPos; 
            
            //Rule number 2: Adjust building position based on parity and road direction
            
            // 1. We calculate the parity offset
            float offsetX = (occupiedSize.x % 2 == 0) ? -0.5f : 0f;
            float offsetZ = (occupiedSize.y % 2 == 0) ? -0.5f : 0f;
            Vector3 parityOffset = new Vector3(offsetX, 0, offsetZ);

            // 2. We verify the road direction influence
            // Calculate road direction vector
            // Convert 2D road direction to 3D
            Vector3 roadDir3D = new Vector3(roadDir.x, 0, roadDir.y);
            
            // If the offset is negative on an axis, we verify the road direction on that axis and
            // invert it (+0.5) to push the building TOWARDS the road.
            
            // Verify on X
            if (Mathf.Abs(offsetX) > 0)
            {
                if (Vector3.Dot(new Vector3(offsetX, 0, 0), roadDir3D) < 0) parityOffset.x = 0.5f;
            }
            // Verify on Z
            if (Mathf.Abs(offsetZ) > 0)
            {
                if (Vector3.Dot(new Vector3(0, 0, offsetZ), roadDir3D) < 0) parityOffset.z = 0.5f;
            }

            Vector3 localProfileOffset = finalRot * profile.PositionOffset;
            
            // Apply all offsets
            instance.transform.position = finalPos + parityOffset + localProfileOffset;
            instance.transform.localScale = profile.Scale;
        }
        if (profile.Type == BuildingType.House || profile.Type == BuildingType.Apartment)
        {
            InhabitedHousePositions.Add(pos);
        }
    }

    private bool IsSpaceStrictlyFree(Vector2Int centerPos, Vector2Int size, int padding) // Check if the area is free with padding
    {
        int startX = centerPos.x - (size.x / 2);
        int startY = centerPos.y - (size.y / 2);
        int checkStartX = startX - padding;
        int checkStartY = startY - padding;
        int checkSizeX = size.x + (padding * 2);
        int checkSizeY = size.y + (padding * 2);

        for (int x = 0; x < checkSizeX; x++)
        {
            for (int y = 0; y < checkSizeY; y++)
            {
                Vector2Int cellToCheck = new Vector2Int(checkStartX + x, checkStartY + y);
                if (roadGenerator.IsCellRoad(cellToCheck)) return false;
                if (!gridManager.IsAreaFree(cellToCheck, Vector2Int.one)) return false;
            }
        }
        return true;
    }

    // --- Helpers ---
    private void ScanForBuildingLots() // Scan the grid for potential building lots
    {
        constructionQueue.Clear();
        processedCells.Clear();
        int radius = Mathf.CeilToInt(gridManager.BuildRadius);
        Vector2Int center = new Vector2Int(Mathf.RoundToInt(gridManager.BuildAreaCenter.position.x), Mathf.RoundToInt(gridManager.BuildAreaCenter.position.z));
        for (int x = -radius; x <= radius; x++) {
            for (int y = -radius; y <= radius; y++) {
                Vector2Int pos = center + new Vector2Int(x, y);
                if (roadGenerator.IsCellRoad(pos)) {
                    CheckNeighbor(pos + Vector2Int.up); CheckNeighbor(pos + Vector2Int.down);
                    CheckNeighbor(pos + Vector2Int.left); CheckNeighbor(pos + Vector2Int.right);
                }
            }
        }
    }

    private void CheckNeighbor(Vector2Int pos) // Check and add neighbor cell to construction queue
    {
        if (processedCells.Contains(pos)) return;
        processedCells.Add(pos);
        if (!roadGenerator.IsCellRoad(pos) && gridManager.IsAreaFree(pos, Vector2Int.one)) constructionQueue.Add(pos);
    }

    private List<Vector2Int> GetAvailableRoadDirections(Vector2Int pos) // Get road directions adjacent to the position
    {
        List<Vector2Int> dirs = new List<Vector2Int>();
        if (roadGenerator.IsCellRoad(pos + Vector2Int.up)) dirs.Add(Vector2Int.up);
        if (roadGenerator.IsCellRoad(pos + Vector2Int.down)) dirs.Add(Vector2Int.down);
        if (roadGenerator.IsCellRoad(pos + Vector2Int.left)) dirs.Add(Vector2Int.left);
        if (roadGenerator.IsCellRoad(pos + Vector2Int.right)) dirs.Add(Vector2Int.right);
        return dirs.OrderBy(d => System.Guid.NewGuid()).ToList();
    }

    private List<BuildingProfile> GetProfilesSortedBySize(BuildingType type) // Get building profiles sorted by size for the specified type
    {
        List<BuildingProfile> sourceList = null;
        switch (type) {
            case BuildingType.House: sourceList = housePrefabs; break;
            case BuildingType.Apartment: sourceList = apartmentPrefabs; break;
            case BuildingType.Factory: sourceList = factoryPrefabs; break;
            case BuildingType.Office: case BuildingType.Hospital: sourceList = servicePrefabs; break;
        }
        if (sourceList == null) return new List<BuildingProfile>();
        return sourceList.OrderByDescending(p => p.Size.x * p.Size.y).ThenBy(p => System.Guid.NewGuid()).ToList();
    }

    private BuildingType DecideBuildingType(Vector2Int gridPos) // Decide building type based on position
    {
        Vector3 worldPos = new Vector3(gridPos.x, 0, gridPos.y);
        Vector3 centerPos = gridManager.BuildAreaCenter.position;
        float dist = Vector2.Distance(new Vector2(worldPos.x, worldPos.z), new Vector2(centerPos.x, centerPos.z));

        if (dist > zoneVisualizer.CenterZoneRadius && IsInIndustrialSector(worldPos, centerPos)) return BuildingType.Factory;
        if (dist <= zoneVisualizer.CenterZoneRadius) {
            if (Random.Range(0, 100) < ServiceBuildingChance) return BuildingType.Office;
            return BuildingType.Apartment;
        }
        float suburbStart = zoneVisualizer.CenterZoneRadius;
        float suburbEnd = zoneVisualizer.SuburbsZoneRadius;
        if (dist > suburbEnd) return BuildingType.House;
        float progress = Mathf.Clamp01((dist - suburbStart) / (suburbEnd - suburbStart));
        int blockChance = (progress < 0.33f) ? 60 : (progress < 0.66f ? 40 : 0);
        return (Random.Range(0, 100) < blockChance) ? BuildingType.Apartment : BuildingType.House;
    }

    private bool IsInIndustrialSector(Vector3 pos, Vector3 center) // Check if position is in an industrial sector
    {
        Vector3 dir = pos - center;
        float angle = Mathf.Atan2(dir.z, dir.x) * Mathf.Rad2Deg;
        if (angle < 0) angle += 360f;
        foreach (Vector2 sector in zoneVisualizer.GeneratedSectors) {
            float end = sector.x + sector.y;
            float normAngle = angle;
            if (end > 360 && normAngle < end - 360) normAngle += 360;
            if (normAngle >= sector.x && normAngle <= end) return true;
        }
        return false;
    }

    private float GetRotationFromDirection(Vector2Int dir) // Get Y rotation angle from direction vector
    {
        if (dir == Vector2Int.up) return 0f;
        if (dir == Vector2Int.down) return 180f;
        if (dir == Vector2Int.right) return 90f;
        if (dir == Vector2Int.left) return 270f;
        return 0f;
    }

    private float GetTerrainHeight(Vector2Int pos) // Get terrain height at the specified grid position
    {
        Vector3 origin = new Vector3(pos.x, RaycastHeight, pos.y);
        if (Physics.Raycast(origin, Vector3.down, out RaycastHit hit, RaycastHeight * 2, TerrainLayer)) return hit.point.y;
        return float.MinValue;
    }

    private void AddDebugBox(Vector2Int center, Vector2Int size, Color col) // Add a debug box for visualization
    {
        debugBounds.Add(new BoundsDebug { center = new Vector3(center.x, 10f, center.y), size = new Vector3(size.x, 5f, size.y), color = col });
    }

    private void OnDrawGizmos() // Draw debug gizmos in the editor
    {
        if (!ShowDebugGizmos) return;
        foreach (var b in debugBounds) {
            Gizmos.color = new Color(b.color.r, b.color.g, b.color.b, 0.4f); Gizmos.DrawCube(b.center, b.size);
            Gizmos.color = b.color; Gizmos.DrawWireCube(b.center, b.size);
        }
    }
}