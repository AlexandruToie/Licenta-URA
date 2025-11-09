using System.Collections.Generic;
using UnityEngine;

public class CityGenerator : MonoBehaviour
{
    [Header("Prefab Categories")]
    [Tooltip("The first piece to place. A crossroad is a good start.")]
    public GameObject startPrefab;
    [Tooltip("e.g., Strait-Road (This is the 'default' straight piece)")]
    public GameObject defaultStraightPrefab;
    [Tooltip("e.g., Strait-Road-Pedestrian (The 'special' piece to mix in)")]
    public GameObject specialStraightPrefab;
    
    [Tooltip("e.g., 90-Road, 90-Long-Road")]
    public List<GameObject> cornerPrefabs;
    [Tooltip("e.g., 3Way-..., 5Way-...")]
    public List<GameObject> threeWayPrefabs;
    [Tooltip("e.g., Cross-Road, 4Way-...")]
    public List<GameObject> crossroadPrefabs;
    [Tooltip("e.g., Roundabout")]
    public List<GameObject> roundaboutPrefabs;

    [Header("Growth Logic")]
    [Tooltip("The MIN number of straight roads to place before an intersection.")]
    public int minStraightSteps = 3;
    [Tooltip("The MAX number of straight roads to place before an intersection.")]
    public int maxStraightSteps = 10;
    
    // --- NEW: Subprogram Logic ---
    [Header("Straight Road 'Subprogram'")]
    [Tooltip("The minimum number of roads between 'special' pieces.")]
    public int minSpacingForSpecial = 5;
    [Tooltip("The chance (0-1) to place a 'special' piece when spacing is met.")]
    [Range(0f, 1f)]
    public float chanceForSpecial = 0.5f;

    
    [Header("Terrain Constraints")]
    [Tooltip("Drag your Terrain Generator script here to get the flat zone info.")]
    public WindingPathTerrainGenerator terrainGenerator;
    [Tooltip("If true, roads will ONLY be built on the flat path/center areas defined in the terrain generator.")]
    public bool constrainToFlatZone = true; 
    
    private Terrain terrain;

    // --- Private ---
    private Stack<SocketAgent> checkpointStack = new Stack<SocketAgent>();
    private HashSet<Vector3> occupiedSocketPositions = new HashSet<Vector3>();
    private List<RoadSegment> placedRoadSegments = new List<RoadSegment>();
    private List<GameObject> spawnedRoads = new List<GameObject>();

    // This is our "Checkpoint" / "Walker"
    private class SocketAgent
    {
        public Vector3 worldPosition;
        public Quaternion worldRotation; // The direction the socket "faces"
        
        // --- NEW: We track spacing in the agent ---
        public int currentStraightSpacing; 
        
        public SocketAgent(Vector3 pos, Quaternion rot, int spacing)
        {
            worldPosition = pos;
            worldRotation = rot;
            currentStraightSpacing = spacing;
        }
    }
    
    // Stores the start/end of a road for math checks
    private class RoadSegment
    {
        public Vector2 p1; // 2D Start
        public Vector2 p2; // 2D End
        public RoadSegment(Vector2 p1, Vector2 p2) { this.p1 = p1; this.p2 = p2; }
    }


    public void Initialize(Terrain terrain, WindingPathTerrainGenerator generator)
    {
        Debug.Log("CityGenerator received 'Initialize' call.");
        
        this.terrain = terrain;
        this.terrainGenerator = generator;

        if (terrain == null || terrainGenerator == null)
        {
            Debug.LogError("CityGenerator was not given a valid Terrain or TerrainGenerator!");
            return;
        }

        if (!ValidatePrefabs()) return;
        
        GenerateCity();
    }

    [ContextMenu("Generate City")]
    private void GenerateCity()
    {
        ClearCity();

        // 1. The Start Point
        Vector3 startPos = terrain.transform.position + 
                           new Vector3(terrainGenerator.centerX, 0, terrainGenerator.centerY);
        startPos.y = terrain.SampleHeight(startPos);
        
        GameObject seedPrefab = Instantiate(startPrefab, startPos, Quaternion.identity);
        spawnedRoads.Add(seedPrefab);
        
        BaseRoadConnector seedConnector = seedPrefab.GetComponent<BaseRoadConnector>();
        
        // Add all of its *exit* sockets as our first "checkpoints"
        Debug.Log($"Spawning 'Start Prefab': {startPrefab.name}. Finding exits...");

        foreach (var socket in seedConnector.GetExitSockets()) 
        {
            Vector3 worldPos = seedPrefab.transform.TransformPoint(socket.localPosition);
            Quaternion worldRot = seedPrefab.transform.rotation * socket.localRotation;
            
            // Start with a spacing of 0
            checkpointStack.Push(new SocketAgent(worldPos, worldRot, 0)); 
            occupiedSocketPositions.Add(RoundVector(worldPos));
        }
        
        var entrySocket = seedConnector.FindEntrySocket();
        if (entrySocket != null)
        {
            Vector3 entryWorldPos = seedPrefab.transform.TransformPoint(entrySocket.localPosition);
            occupiedSocketPositions.Add(RoundVector(entryWorldPos));
        }

        Debug.Log($"Found {checkpointStack.Count} valid exits to explore.");
        
        // 2. The Main Loop (Explorer)
        while (checkpointStack.Count > 0)
        {
            SocketAgent currentCheckpoint = checkpointStack.Pop();
            
            Vector3 currentPos = currentCheckpoint.worldPosition;
            Quaternion currentRot = currentCheckpoint.worldRotation;
            int currentSpacing = currentCheckpoint.currentStraightSpacing; // Get the current spacing

            // 3. Phase 1: Build Straight Roads
            int straightCount = Random.Range(minStraightSteps, maxStraightSteps + 1);
            bool hitWall = false;

            for (int i = 0; i < straightCount; i++)
            {
                // --- NEW: "Subprogram" logic ---
                GameObject prefab;
                if (currentSpacing >= minSpacingForSpecial && Random.value < chanceForSpecial)
                {
                    // Place a "special" road
                    prefab = specialStraightPrefab;
                    currentSpacing = 0; // Reset spacing
                }
                else
                {
                    // Place a "default" road
                    prefab = defaultStraightPrefab;
                    currentSpacing++; // Increment spacing
                }
                // --- End of Subprogram ---
                
                BaseRoadConnector connector = prefab.GetComponent<BaseRoadConnector>();
                BaseRoadConnector.ConnectionPoint entrySocketForStraight = connector.FindEntrySocket();
                
                (Vector3 newPos, Quaternion newRot) = CalculateNewTransform(currentPos, currentRot, entrySocketForStraight);
                
                Vector3 exitPos = newPos + (newRot * connector.GetExitSocket().localPosition);
                if (!IsStraightSegmentValid(currentPos, exitPos, newPos, newRot, connector))
                {
                    hitWall = true; 
                    break; 
                }

                GameObject newRoad = Instantiate(prefab, newPos, newRot);
                spawnedRoads.Add(newRoad);
                
                AddSocketsToOccupiedList(newRoad, connector, entrySocketForStraight);
                placedRoadSegments.Add(new RoadSegment(new Vector2(currentPos.x, currentPos.z), new Vector2(exitPos.x, exitPos.z)));

                // Update "current position" for the *next* loop
                BaseRoadConnector.ConnectionPoint exitSocket = connector.GetExitSocket();
                currentPos = newRoad.transform.TransformPoint(exitSocket.localPosition);
                currentRot = newRoad.transform.rotation * exitSocket.localRotation;
            }
            
            // 4. Phase 2: Build Intersection
            if (hitWall)
            {
                continue; 
            }
            
            GameObject intPrefab = GetRandomIntersectionPrefab();
            if (intPrefab == null) continue;
            
            BaseRoadConnector intConnector = intPrefab.GetComponent<BaseRoadConnector>();
            BaseRoadConnector.ConnectionPoint intEntrySocket = intConnector.FindEntrySocket();

            (Vector3 intPos, Quaternion intRot) = CalculateNewTransform(currentPos, currentRot, intEntrySocket);
            
            if (!IsIntersectionValid(currentPos, intPos, intRot, intConnector, intEntrySocket))
            {
                continue; 
            }
            
            GameObject newIntersection = Instantiate(intPrefab, intPos, intRot);
            spawnedRoads.Add(newIntersection);
            
            // --- NEW: Pass the 'currentSpacing' to the new sockets ---
            AddSocketsToCheckpoints(newIntersection, intConnector, intEntrySocket, currentSpacing);
            
            placedRoadSegments.Add(new RoadSegment(new Vector2(currentPos.x, currentPos.z), new Vector2(intPos.x, intPos.z)));
        }
        
        Debug.Log("City Generation Complete. Explored all paths.");
    }

    #region Helper Functions

    (Vector3, Quaternion) CalculateNewTransform(Vector3 socketPos, Quaternion socketRot, BaseRoadConnector.ConnectionPoint pieceEntrySocket)
    {
        Quaternion newRot = socketRot; 
        Vector3 newPos = socketPos; 
        return (newPos, newRot);
    }
    
    // Check for a straight road
    bool IsStraightSegmentValid(Vector3 entryWorldPos, Vector3 exitWorldPos, Vector3 newPiecePos, Quaternion newPieceRot, BaseRoadConnector connector)
    {
        if (constrainToFlatZone && !IsOnFlatZone(exitWorldPos)) return false;
        if (IsSocketOccupied(exitWorldPos)) return false;
        
        // --- Use the new, correct math check ---
        if (DoesPathMathematicallyCross(new Vector2(entryWorldPos.x, entryWorldPos.z), new Vector2(exitWorldPos.x, exitWorldPos.z))) return false;

        return true;
    }

    // Check for an intersection
    bool IsIntersectionValid(Vector3 entryWorldPos, Vector3 newPiecePos, Quaternion newPieceRot, BaseRoadConnector connector, BaseRoadConnector.ConnectionPoint entrySocket)
    {
        foreach (var socket in connector.GetExitSockets())
        {
            Vector3 exitWorldPos = newPiecePos + (newPieceRot * socket.localPosition);
            
            if (constrainToFlatZone && !IsOnFlatZone(exitWorldPos)) return false; 
            if (IsSocketOccupied(exitWorldPos)) return false;
        }
        
        foreach (var exitSocket in connector.GetExitSockets())
        {
            Vector3 exitWorldPos = newPiecePos + (newPieceRot * exitSocket.localPosition);
            
            // --- Use the new, correct math check ---
            if (DoesPathMathematicallyCross(new Vector2(entryWorldPos.x, entryWorldPos.z), new Vector2(exitWorldPos.x, exitWorldPos.z)))
            {
                return false;
            }
        }
        
        return true;
    }

    GameObject GetRandomIntersectionPrefab()
    {
        List<GameObject> allIntersectionPrefabs = new List<GameObject>();
        allIntersectionPrefabs.AddRange(cornerPrefabs);
        allIntersectionPrefabs.AddRange(threeWayPrefabs);
        allIntersectionPrefabs.AddRange(crossroadPrefabs);
        allIntersectionPrefabs.AddRange(roundaboutPrefabs);
        
        if (allIntersectionPrefabs.Count == 0)
        {
            Debug.LogError("No intersection prefabs defined!");
            return null;
        }
        return allIntersectionPrefabs[Random.Range(0, allIntersectionPrefabs.Count)];
    }

    void AddSocketsToOccupiedList(GameObject piece, BaseRoadConnector connector, BaseRoadConnector.ConnectionPoint entrySocketToIgnore)
    {
        foreach (var socket in connector.sockets)
        {
            if (socket == entrySocketToIgnore) continue;
            
            Vector3 worldPos = piece.transform.TransformPoint(socket.localPosition);
            occupiedSocketPositions.Add(RoundVector(worldPos));
        }
    }
    
    // --- NEW: This function now takes 'spacing' ---
    void AddSocketsToCheckpoints(GameObject piece, BaseRoadConnector connector, BaseRoadConnector.ConnectionPoint entrySocketToIgnore, int currentSpacing)
    {
        foreach (var socket in connector.GetExitSockets()) // <-- Only add EXITS
        {
            if (socket == entrySocketToIgnore) continue;

            Vector3 worldPos = piece.transform.TransformPoint(socket.localPosition);
            Quaternion worldRot = piece.transform.rotation * socket.localRotation;
            
            // When we add a new checkpoint from an intersection, we RESET its spacing to 0
            checkpointStack.Push(new SocketAgent(worldPos, worldRot, 0)); 
            
            occupiedSocketPositions.Add(RoundVector(worldPos));
        }
    }
    
    bool IsSocketOccupied(Vector3 worldPos)
    {
        return occupiedSocketPositions.Contains(RoundVector(worldPos));
    }

    private Vector3 RoundVector(Vector3 v)
    {
        return new Vector3(
            Mathf.Round(v.x * 10f) / 10f,
            Mathf.Round(v.y * 10f) / 10f,
            Mathf.Round(v.z * 10f) / 10f
        );
    }
    
    private bool IsOnFlatZone(Vector3 worldPos)
    {
        if (terrainGenerator == null || terrain == null) return true; 
        Vector3 terrainLocalPos = worldPos - terrain.transform.position;
        int terrainX = (int)terrainLocalPos.x;
        int terrainY = (int)terrainLocalPos.z;

        float pathBlend = terrainGenerator.GetPublicPathBlend(terrainX, terrainY);
        if (pathBlend > 0.1f) return true;

        float dist = Vector2.Distance(new Vector2(terrainX, terrainY), new Vector2(terrainGenerator.centerX, terrainGenerator.centerY));
        if (dist <= terrainGenerator.centerRadius * terrainGenerator.centerBlendFactor) return true; 

        return false;
    }

    // ---
    // --- THIS IS THE FIXED MATH FUNCTION ---
    // ---
    #region Road Intersection Math
    
    private bool DoesPathMathematicallyCross(Vector2 p1, Vector2 p2)
    {
        foreach (RoadSegment segment in placedRoadSegments)
        {
            if (LineSegmentsIntersect(p1, p2, segment.p1, segment.p2))
            {
                return true; // We found a crossing
            }
        }
        return false; // Path is clear
    }

    // This is the new, correct function that handles collinear lines
    private bool LineSegmentsIntersect(Vector2 p1, Vector2 q1, Vector2 p2, Vector2 q2)
    {
        // Find the four orientations needed
        int o1 = GetOrientation(p1, q1, p2);
        int o2 = GetOrientation(p1, q1, q2);
        int o3 = GetOrientation(p2, q2, p1);
        int o4 = GetOrientation(p2, q2, q1);

        // --- This is the ONLY case we care about ---
        // This is the "General Case" where the lines
        // are not on the same line and properly cross in the middle.
        if (o1 != o2 && o3 != o4)
        {
            return true;
        }

        // All other cases (collinear, touching at an end, etc.)
        // are NOT a "crossing" for our purposes.
        return false;
    }
    
    // Finds orientation (0 = collinear, 1 = clockwise, 2 = counter-clockwise)
    private int GetOrientation(Vector2 p, Vector2 q, Vector2 r)
    {
        float val = (q.y - p.y) * (r.x - q.x) - (q.x - p.x) * (r.y - q.y);
        if (Mathf.Abs(val) < 0.0001f) return 0; // collinear
        return (val > 0) ? 1 : 2; // clock or counter-clock wise
    }

    // Checks if point q lies on line segment 'pr'
    private bool OnSegment(Vector2 p, Vector2 q, Vector2 r)
    {
        if (q.x <= Mathf.Max(p.x, r.x) && q.x >= Mathf.Min(p.x, r.x) &&
            q.y <= Mathf.Max(p.y, r.y) && q.y >= Mathf.Min(p.y, r.y))
        {
            return true;
        }
        return false;
    }
    
    #endregion

    private bool ValidatePrefabs()
    {
        if (startPrefab == null) { Debug.LogError("Start Prefab is not set!"); return false; }
        
        // --- NEW: Check for new straight road setup ---
        if (defaultStraightPrefab == null) { Debug.LogError("Default Straight Prefab is not set!"); return false; }
        if (specialStraightPrefab == null) { Debug.LogError("Special Straight Prefab is not set! (Assign one, even if it's the same as the default)"); return false; }
        
        if (cornerPrefabs.Count == 0 && threeWayPrefabs.Count == 0 && crossroadPrefabs.Count == 0 && roundaboutPrefabs.Count == 0)
        { Debug.LogError("You have no 'intersection' prefabs defined!"); return false; }
        return true;
    }

    [ContextMenu("Clear City")]
    private void ClearCity()
    {
        StopAllCoroutines();
        checkpointStack.Clear();
        occupiedSocketPositions.Clear();
        placedRoadSegments.Clear();
        
        foreach (var road in spawnedRoads)
        {
            if (road != null)
            {
                if (Application.isPlaying) Destroy(road);
                else DestroyImmediate(road);
            }
        }
        spawnedRoads.Clear();
    }
    
    #endregion
}