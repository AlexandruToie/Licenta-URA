using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

[RequireComponent(typeof(RoadGridManager))]
public class RoadGenerator : MonoBehaviour
{
    private RoadGridManager gridManager;
    private RoadPathfinder pathfinder;

    [Header("Prefab List")]
    public List<PrefabData> poiPrefabs;
    public PrefabData roadStraight;
    public PrefabData roadCorner;
    public PrefabData road3Way; 
    public PrefabData road4Way; 
    public PrefabData roadDeadEnd;

    [Header("Visual Adjustments")]
    public float StraightRotationOffset = 90f;
    public float CornerRotationOffset = 180f;
    public float ThreeWayRotationOffset = 0f; 
    public float FourWayRotationOffset = 0f;
    public float DeadEndRotationOffset = 0f;

    [Header("Generation Settings")]
    public int NumberOfPOIs = 20; 
    public int MaxPlacementAttemptsPerPOI = 20;
    public float MinDistanceBetweenPOIs = 35f; 
    public float SpawnEdgePadding = 10f;
    
    [Tooltip("The max lenght for in front of the POI.")]
    public int RunwayLength = 4;
    [Tooltip("The free space around the POI that roads cannot enter.")]
    public int POIBufferSize = 2;
    [Tooltip("Minimum distance between intersections.")]
    public float MinIntersectionDistance = 3f; 
    
    public float StepDelay = 0.01f; 
    public bool ShowDebugMarkers = true;

    public event System.Action OnGenerationFinished;

    private HashSet<Vector2Int> roadMapVirtual = new HashSet<Vector2Int>(); // The virtual representation of placed roads
    
    private struct WorldSocket // Information about a POI connection socket in world coordinates
    {
        public Vector2Int Position;
        public Vector2Int Direction; 
        public PlacedPOI OwnerPOI;
    }
    // --- State ---
    private List<WorldSocket> allSockets = new List<WorldSocket>();
    private HashSet<Vector2Int> connectedSockets = new HashSet<Vector2Int>();
    private HashSet<Vector2Int> protectedAccessPoints = new HashSet<Vector2Int>(); 
    private HashSet<Vector2Int> poiRestrictedZone = new HashSet<Vector2Int>();
    private HashSet<Vector2Int> existingIntersections = new HashSet<Vector2Int>();
    private HashSet<Vector2Int> occupiedEdgeTargets = new HashSet<Vector2Int>();

    private class PlacedPOI { public Vector2Int GridPosition; public PrefabData Data; } // Info about a placed POI
    private List<PlacedPOI> placedPOIs = new List<PlacedPOI>(); // List of all placed POIs
    private List<Vector2Int> poiPositionsCache = new List<Vector2Int>();// Cached list of POI positions for pathfinding

    private List<GameObject> debugMarkers = new List<GameObject>();// For debug visualization

    void Start()
    {
        gridManager = GetComponent<RoadGridManager>();
        pathfinder = new RoadPathfinder(gridManager);
        StartCoroutine(GenerateSequence());
    }

    IEnumerator GenerateSequence() // The main generation sequence
    {
        // 1. Reset
        roadMapVirtual.Clear();
        allSockets.Clear();
        connectedSockets.Clear();
        protectedAccessPoints.Clear();
        poiRestrictedZone.Clear();
        existingIntersections.Clear();
        occupiedEdgeTargets.Clear();
        placedPOIs.Clear();
        poiPositionsCache.Clear();
        foreach(var obj in debugMarkers) Destroy(obj);
        debugMarkers.Clear();
        yield return null;

        // 2. Setup
        PlaceAllPOIs();
        yield return new WaitForSeconds(StepDelay);
        IdentifyAllSockets();
        GeneratePOIBuffers(); 

        // 3. Main Generation Loop
        yield return StartCoroutine(GenerateAllConnections());

        // 4. Finalization
        yield return StartCoroutine(SealEmptySockets());
        
        Debug.Log($"[Generator] Gata! Socket-uri conectate/sigilate: {connectedSockets.Count + CountSealedSockets()}/{allSockets.Count}");

        OnGenerationFinished?.Invoke();
    }

    private int CountSealedSockets() // Counts how many sockets have been sealed with dead ends
    {
        return allSockets.Count(s => roadMapVirtual.Contains(s.Position) && !connectedSockets.Contains(s.Position));
    }

    private void GeneratePOIBuffers() // Marks restricted zones around POIs
    {
        foreach(var poi in placedPOIs)
        {
            poiPositionsCache.Add(poi.GridPosition); // Cache POI positions for pathfinding
            int startX = poi.GridPosition.x - (poi.Data.Size.x / 2) - POIBufferSize;
            int startY = poi.GridPosition.y - (poi.Data.Size.y / 2) - POIBufferSize;
            int endX = poi.GridPosition.x + (poi.Data.Size.x / 2) + POIBufferSize;
            int endY = poi.GridPosition.y + (poi.Data.Size.y / 2) + POIBufferSize;

            for (int x = startX; x < endX; x++) // Loop through buffer area
            {
                for (int y = startY; y < endY; y++) // Mark cells as restricted
                {
                    Vector2Int pos = new Vector2Int(x, y);
                    if (!protectedAccessPoints.Contains(pos)) poiRestrictedZone.Add(pos); // Only add if not already a protected access point
                }
            }
        }
    }

    IEnumerator GenerateAllConnections() // Attempts to connect all POI sockets
    {
        Debug.Log("[Generator] Incep generarea independenta...");

        var sortedPOIs = placedPOIs.OrderByDescending(p => p.Data.ConnectionSockets.Count).ToList(); // Sort POIs by number of sockets

        foreach (var poi in sortedPOIs) // Process each POI
        {
            var mySockets = allSockets.Where(s => s.OwnerPOI == poi).ToList(); // Get sockets for this POI

            foreach (var socket in mySockets) // Process each socket
            {
                if (connectedSockets.Contains(socket.Position)) continue; // Skip if already connected

                if (!IsRunwaySpaceFree(socket.Position, socket.Direction, RunwayLength))  // Check runway space
                {
                    continue;
                }

                List<Vector2Int> runway = BuildRunway(socket); // Build runway
                Vector2Int pathStart = runway.Last(); // Start pathfinding from end of runway
                Vector2Int startDir = socket.Direction; // Direction to start pathfinding

                bool pathBuilt = false;
                
                Vector2 center = new Vector2(gridManager.BuildAreaCenter.position.x, gridManager.BuildAreaCenter.position.z); // Center of build area
                Vector2 generalDir = (Vector2)socket.Direction; // General direction of socket
                
                float angle = Random.Range(-15f, 15f) * Mathf.Deg2Rad; // Random angle variation
                Vector2 randomDir = new Vector2(
                    generalDir.x * Mathf.Cos(angle) - generalDir.y * Mathf.Sin(angle),
                    generalDir.x * Mathf.Sin(angle) + generalDir.y * Mathf.Cos(angle)
                ); // Apply rotation to general direction

                Vector2 edgePoint = center + (randomDir.normalized * (gridManager.BuildRadius - 2f)); // Calculate edge point
                Vector2Int mapTarget = new Vector2Int(Mathf.RoundToInt(edgePoint.x), Mathf.RoundToInt(edgePoint.y)); // Convert to grid coordinates

                Vector2Int uniqueTarget = GetUniqueMapTarget(center, generalDir, 30f); // Get unique target on edge

                if (uniqueTarget != Vector2Int.zero) // Attempt pathfinding to unique target
                {
                    if(ShowDebugMarkers) CreateDebugSphere(uniqueTarget, Color.green, "Target_Edge");
                    pathBuilt = TryBuildPath(pathStart, uniqueTarget, startDir, 50);
                }

                if (!pathBuilt) //Fallback to the original point if failed
                {
                    UpdateSingleRoadVisual(pathStart, false); // Ensure start visual is updated
                }
                else
                {
                    connectedSockets.Add(socket.Position); // Mark socket as connected
                }

                yield return new WaitForSeconds(StepDelay);
            }
        }
    }
    IEnumerator SealEmptySockets() // Seals unconnected sockets with dead ends
    {
        Debug.Log("[Generator] Etapa Finala: Sigilare socket-uri neconectate...");

        foreach (var socket in allSockets) 
        {
            if (roadMapVirtual.Contains(socket.Position)) continue; // Skip if already has a road
            int bestLength = 0;
            for (int len = RunwayLength; len >= 1; len--) // Check for maximum possible length
            {
                if (IsRunwaySpaceFree(socket.Position, socket.Direction, len)) // Check if space is free
                {
                    bestLength = len;
                    break;
                }
            }

            if (bestLength == 0) // If no space for full runway, check for at least 1 cell
            {
                if (!roadMapVirtual.Contains(socket.Position)) bestLength = 1; 
            }

            if (bestLength > 0) // Build the dead-end runway
            {
                List<Vector2Int> runwayPoints = new List<Vector2Int>();
                Vector2Int curr = socket.Position;
                runwayPoints.Add(curr); // Include the socket position

                for (int i = 0; i < bestLength; i++)
                {
                    curr += socket.Direction;
                    runwayPoints.Add(curr); // Include each runway cell
                }

                foreach (var p in runwayPoints) // Place roads for the dead-end runway
                {
                    if (!roadMapVirtual.Contains(p))
                    {
                        roadMapVirtual.Add(p);
                        bool isTip = (p == runwayPoints.Last());
                        UpdateSingleRoadVisual(p, !isTip);
                    }
                }
            }

            yield return new WaitForSeconds(StepDelay); // Small delay for visualization
        }
    }

    private Vector2Int GetUniqueMapTarget(Vector2 center, Vector2 direction, float angleVar) // Finds a unique target point on the edge
    {
        int attempts = 0;
        Vector2Int candidate = Vector2Int.zero; // Candidate target point

        while (attempts < 50) // Limit attempts to avoid infinite loops
        {
            float angle = (angleVar > 0) ? Random.Range(-angleVar, angleVar) * Mathf.Deg2Rad : 0;
            Vector2 randomDir = new Vector2(
                direction.x * Mathf.Cos(angle) - direction.y * Mathf.Sin(angle),
                direction.x * Mathf.Sin(angle) + direction.y * Mathf.Cos(angle)
            ); // Apply rotation to direction

            Vector2 edgePoint = center + (randomDir.normalized * (gridManager.BuildRadius - 2f)); // Calculate edge point
            candidate = new Vector2Int(Mathf.RoundToInt(edgePoint.x), Mathf.RoundToInt(edgePoint.y)); //  Convert to grid coordinates

            bool tooClose = false;
            foreach(var other in occupiedEdgeTargets) // Check against existing targets
            {
                if (Vector2Int.Distance(candidate, other) < 3f) 
                {
                    tooClose = true; 
                    break; 
                }
            }

            if (!tooClose) // If unique, return candidate
            {
                occupiedEdgeTargets.Add(candidate);
                return candidate;
            }
            angleVar += 2f; 
            attempts++;
        }
        return Vector2Int.zero;
    }

    bool TryBuildPath(Vector2Int start, Vector2Int end, Vector2Int dir, int maxTurns) // Attempts to build a path using the pathfinder
    {
        List<Vector2Int> path = pathfinder.FindPath(
            start, end, 
            roadMapVirtual, 
            protectedAccessPoints, 
            dir, 
            poiRestrictedZone, 
            poiPositionsCache, 
            existingIntersections,
            MinIntersectionDistance,
            maxTurns
        ); // Find path using the pathfinder

        if (path != null && path.Count > 0)
        {
            foreach (var p in path)
            {
                if (!roadMapVirtual.Contains(p))
                {
                    roadMapVirtual.Add(p);
                    UpdateSingleRoadVisual(p, false);
                }
            }
            foreach (var p in path) RefreshNeighborsVisuals(p); // Update neighbors for visual consistency
            RefreshNeighborsVisuals(start);
            RefreshNeighborsVisuals(end);
            return true;
        }
        return false;
    }

    private bool IsRunwaySpaceFree(Vector2Int start, Vector2Int dir, int length) // Checks if runway space is free
    {
        Vector2Int left = (dir.y != 0) ? Vector2Int.left : Vector2Int.up;
        Vector2Int right = (dir.y != 0) ? Vector2Int.right : Vector2Int.down;
        Vector2Int curr = start;

        for(int i=0; i<length; i++) // Check each cell in the runway
        {
            if (CheckRoad(curr)) return false; 
            if (CheckRoad(curr + left) || CheckRoad(curr + left*2)) return false;
            if (CheckRoad(curr + right) || CheckRoad(curr + right*2)) return false;
            curr += dir;
        }
        return true;
    }
    private bool CheckRoad(Vector2Int p) => roadMapVirtual.Contains(p); // Checks if a road exists at the given position

    private List<Vector2Int> BuildRunway(WorldSocket socket) // Builds the runway for a given socket
    {
        List<Vector2Int> points = new List<Vector2Int>();
        points.Add(socket.Position);
        List<Vector2Int> extension = GetRunwayPixels(socket.Position, socket.Direction, RunwayLength);
        points.AddRange(extension);

        foreach(var p in points) // Place roads along the runway
        {
            if (!roadMapVirtual.Contains(p))
            {
                roadMapVirtual.Add(p);
                UpdateSingleRoadVisual(p, true); 
            }
        }
        MarkSocketUsed(socket.Position); // Mark the socket as used
        return points;
    }

    private void UpdateSingleRoadVisual(Vector2Int pos, bool forceStraight) // Updates the visual representation of a single road cell
    {
        gridManager.RemovePrefabAt(pos); // Clear existing prefab at position
        var socketInfo = allSockets.FirstOrDefault(s => s.Position == pos); // Check if this position is a socket
        
        if (forceStraight || socketInfo.OwnerPOI != null) // Force straight road for sockets or if specified
        {
            float rot = 0;
            if (socketInfo.OwnerPOI != null) // If it's a socket, align with its direction
                rot = (socketInfo.Direction.y != 0) ? (0 + StraightRotationOffset) : (90 + StraightRotationOffset);
            else
            {
                bool hasVert = roadMapVirtual.Contains(pos + Vector2Int.up) || roadMapVirtual.Contains(pos + Vector2Int.down);
                rot = hasVert ? (0 + StraightRotationOffset) : (90 + StraightRotationOffset);
            }
            gridManager.PlacePrefab(roadStraight, pos, Quaternion.Euler(0, rot, 0));
            return;
        }

        // Determine connections to neighboring roads
        bool up = roadMapVirtual.Contains(pos + Vector2Int.up);
        bool down = roadMapVirtual.Contains(pos + Vector2Int.down);
        bool left = roadMapVirtual.Contains(pos + Vector2Int.left);
        bool right = roadMapVirtual.Contains(pos + Vector2Int.right);

        // Also check for connections to POI sockets
        CheckNeighborSocket(pos + Vector2Int.up, Vector2Int.down, ref up);
        CheckNeighborSocket(pos + Vector2Int.down, Vector2Int.up, ref down);
        CheckNeighborSocket(pos + Vector2Int.left, Vector2Int.right, ref left);
        CheckNeighborSocket(pos + Vector2Int.right, Vector2Int.left, ref right);

        int count = (up?1:0) + (down?1:0) + (left?1:0) + (right?1:0);
        PrefabData prefab = roadStraight;
        float rY = 0;

        if (count >= 3) // Update intersections list
        {
            if (!existingIntersections.Contains(pos)) existingIntersections.Add(pos);
        }
        else if (existingIntersections.Contains(pos))
        {
            existingIntersections.Remove(pos);
        }

        if (count == 4) { prefab = road4Way; rY = 0 + FourWayRotationOffset; } // 4-Way Intersection
        else if (count == 3) // 3-Way Intersection
        {
            prefab = road3Way;
            if (!up) rY = 90; else if (!down) rY = -90; else if (!left) rY = 0; else rY = 180;
            rY += ThreeWayRotationOffset;
        }
        else if (count == 2) // Corner or Straight
        {
            if (up && down) { prefab = roadStraight; rY = 0 + StraightRotationOffset; }
            else if (left && right) { prefab = roadStraight; rY = 90 + StraightRotationOffset; }
            else
            {
                prefab = roadCorner;
                if (up && right) rY = 0; else if (right && down) rY = 90; else if (down && left) rY = 180; else if (left && up) rY = 270;
                rY += CornerRotationOffset;
            }
        }
        else if (count == 1) // Dead End
        {
            prefab = roadDeadEnd != null ? roadDeadEnd : roadStraight;
            if (up) rY = 0;
            else if (down) rY = 180;
            else if (left) rY = 270;
            else if (right) rY = 90;
            rY += DeadEndRotationOffset;
        }
        else // No connections, default to straight
        {
            prefab = roadStraight;
            if (up || down) rY = 0 + StraightRotationOffset; else rY = 90 + StraightRotationOffset;
        }
        gridManager.PlacePrefab(prefab, pos, Quaternion.Euler(0, rY, 0));
    }

    // Checks if a neighboring position is a socket that connects in the required direction
    private void CheckNeighborSocket(Vector2Int neighborPos, Vector2Int requiredDir, ref bool connectionFlag) 
    {
        var ns = allSockets.FirstOrDefault(s => s.Position == neighborPos);
        if(ns.OwnerPOI != null && ns.Direction == requiredDir) connectionFlag = true;
    }

    private void RefreshNeighborsVisuals(Vector2Int pos) // Refreshes the visuals of neighboring road cells
    {
        var sock = allSockets.FirstOrDefault(s => s.Position == pos);
        if (sock.OwnerPOI != null) return; 
        if(roadMapVirtual.Contains(pos)) UpdateSingleRoadVisual(pos, false);
        UpdateNeighborSafe(pos + Vector2Int.up);
        UpdateNeighborSafe(pos + Vector2Int.down);
        UpdateNeighborSafe(pos + Vector2Int.left);
        UpdateNeighborSafe(pos + Vector2Int.right);
    }

    private void UpdateNeighborSafe(Vector2Int p) // Safely updates a neighboring road cell if it exists
    {
        if(roadMapVirtual.Contains(p))
        {
            var s = allSockets.FirstOrDefault(x => x.Position == p);
            if (s.OwnerPOI == null) UpdateSingleRoadVisual(p, false);
        }
    }

    private void MarkSocketUsed(Vector2Int pos) { if (!connectedSockets.Contains(pos)) connectedSockets.Add(pos); } // Marks a socket as used

    private List<Vector2Int> GetRunwayPixels(Vector2Int start, Vector2Int dir, int length) // Gets the list of pixels for a runway
    {
        List<Vector2Int> res = new List<Vector2Int>();
        Vector2Int curr = start;
        for(int i=0; i<length; i++) { curr += dir; res.Add(curr); }
        return res;
    }

    private void PlaceAllPOIs() // Places all POIs within the build area
    {
        float radius = gridManager.BuildRadius - SpawnEdgePadding;
        Vector2 center = new Vector2(gridManager.BuildAreaCenter.position.x, gridManager.BuildAreaCenter.position.z);

        for (int i = 0; i < NumberOfPOIs; i++) // Attempt to place each POI
        {
            PrefabData prefab = poiPrefabs[Random.Range(0, poiPrefabs.Count)]; // Randomly select a POI prefab
            for (int k = 0; k < MaxPlacementAttemptsPerPOI; k++) // Attempt placement
            {
                Vector2 rnd = Random.insideUnitCircle * radius; // Random position within circle
                int rx = Mathf.RoundToInt((center.x + rnd.x) / 2) * 2; // Ensure even coordinates
                int ry = Mathf.RoundToInt((center.y + rnd.y) / 2) * 2; // Ensure even coordinates
                Vector2Int pos = new Vector2Int(rx, ry); // Candidate position

                if (placedPOIs.Any(p => Vector2Int.Distance(pos, p.GridPosition) < MinDistanceBetweenPOIs)) continue; // Check distance to other POIs
                if (gridManager.IsAreaFree(pos, prefab.Size)) // Check if area is free
                {
                    gridManager.PlacePrefab(prefab, pos, Quaternion.identity);
                    gridManager.MarkAreaOccupied(pos, prefab.Size);
                    placedPOIs.Add(new PlacedPOI { GridPosition = pos, Data = prefab });
                    if(ShowDebugMarkers) CreatePOIGlow(pos, Color.magenta); 
                    break;
                }
            }
        }
    }
    
    private void IdentifyAllSockets() // Identifies all connection sockets from placed POIs
    {
        foreach(var poi in placedPOIs) // Process each placed POI
        {
            foreach(var local in poi.Data.ConnectionSockets) // Process each socket
            {
                Vector2Int world = poi.GridPosition + local;
                Vector2 dirVec = (Vector2)local;
                Vector2Int dir = GetCardinalDirection(dirVec); // Convert to cardinal direction
                allSockets.Add(new WorldSocket { Position = world, Direction = dir, OwnerPOI = poi }); 
                
                List<Vector2Int> runway = GetRunwayPixels(world, dir, RunwayLength); // Mark protected access points
                foreach(var p in runway) protectedAccessPoints.Add(p); // Add runway points to protected access points
                protectedAccessPoints.Add(world); // Also add the socket position itself
            }
        }
    }

    private Vector2Int GetCardinalDirection(Vector2 v) // Converts a vector to a cardinal direction
    { 
        if (Mathf.Abs(v.x) > Mathf.Abs(v.y)) return new Vector2Int(v.x > 0 ? 1 : -1, 0); // Horizontal dominant
        return new Vector2Int(0, v.y > 0 ? 1 : -1); 
    }

    private void CreatePOIGlow(Vector2Int pos, Color color) // Creates a glowing marker for a POI for testing
    {
        GameObject beacon = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        beacon.name = "DEBUG_GLOW_" + pos;
        Destroy(beacon.GetComponent<Collider>());
        beacon.transform.position = new Vector3(pos.x, 10f, pos.y); 
        beacon.transform.localScale = new Vector3(2f, 10f, 2f); 
        Renderer r = beacon.GetComponent<Renderer>();
        r.material = new Material(Shader.Find("Standard"));
        r.material.color = new Color(color.r, color.g, color.b, 0.5f); 
        r.material.EnableKeyword("_EMISSION");
        r.material.SetColor("_EmissionColor", color * 2f);
        debugMarkers.Add(beacon);
    }

    private void CreateDebugSphere(Vector2Int pos, Color col, string name) // Creates a debug sphere at the given position
    {
        GameObject obj = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        obj.name = name;
        obj.GetComponent<Renderer>().material.color = col;
        obj.transform.position = new Vector3(pos.x, 15f, pos.y);
        obj.transform.localScale = Vector3.one * 5f;
        debugMarkers.Add(obj);
    }

    private void CreateErrorText(Vector2Int pos, string message) // Creates a debug text label at the given position
    {
        GameObject errorObj = new GameObject("Error_Label");
        errorObj.transform.position = new Vector3(pos.x, 5f, pos.y);
        TextMesh tm = errorObj.AddComponent<TextMesh>();
        tm.text = message;
        tm.characterSize = 0.5f;
        tm.fontSize = 20;
        tm.color = Color.red;
        tm.anchor = TextAnchor.MiddleCenter;
        errorObj.transform.rotation = Quaternion.Euler(90, 0, 0);
        debugMarkers.Add(errorObj);
    }
    public bool IsCellRoad(Vector2Int pos) // Public method to check if a cell has a road
    {
        return roadMapVirtual.Contains(pos);
    }
}