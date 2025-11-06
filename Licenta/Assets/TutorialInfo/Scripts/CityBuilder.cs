using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CityGenerator : MonoBehaviour
{
    // This struct holds a potential connection point for a new road.
    // It's a "to-do" item for the road builder.
    private struct RoadConnectionPoint
    {
        public Vector3 position; // The *target center* of the next prefab
        public Quaternion rotation; // The direction to build *from* the parent

        public RoadConnectionPoint(Vector3 pos, Quaternion rot)
        {
            this.position = pos;
            this.rotation = rot;
        }
    }

    [Header("Generation Controls")]
    [Tooltip("The total number of road prefabs to place.")]
    public int maxRoadPlacements = 200;
    [Tooltip("How many main roads to connect to the winding paths.")]
    public int numConnectingRoads = 3;

    [Header("Road Prefabs (MUST have PIVOT AT CENTER)")]
    [Tooltip("Set this to the layer your terrain is on (e.g., 'Terrain' or 'Default').")]
    public LayerMask terrainLayer;
    [Tooltip("Set this to the layer ALL your road prefabs are on (e.g., 'Road').")]
    public LayerMask roadLayer; // CRITICAL for overlap checks
    [Tooltip("How close to a connection point we check for existing roads.")]
    public float checkOverlapRadius = 1.0f;
    
    public GameObject roadStraight;
    public GameObject roadTurn90;
    public GameObject road3Way;
    public GameObject roadCross;

    [Header("Prefab Dimensions (Full Length/Width)")]
    [Tooltip("The FULL Z-Length (forward) of your 'roadStraight' prefab.")]
    public float straightLength = 10f;
    [Tooltip("The FULL Z-Length (forward) of your 'roadTurn90' prefab.")]
    public float turnLength = 10f; 
    [Tooltip("The FULL X-Length (sideways) of your 'roadTurn90' prefab.")]
    public float turnWidth = 10f;
    [Tooltip("The FULL Z-Length (forward) of your 'road3Way' prefab.")]
    public float tJunctionLength = 10f;
    [Tooltip("The FULL X-Length (sideways) of your 'road3Way' prefab.")]
    public float tJunctionWidth = 10f; 
    [Tooltip("The FULL Z-Length (forward) of your 'roadCross' prefab.")]
    public float crossLength = 10f; 
    [Tooltip("The FULL X-Length (sideways) of your 'roadCross' prefab.")]
    public float crossWidth = 10f; 

    [Header("Randomness")]
    [Tooltip("The chance (0-1) to place a straight road.")]
    [Range(0f, 1f)]
    public float chanceStraight = 0.7f;
    [Tooltip("The chance (0-1) to place a 90-degree turn.")]
    [Range(0f, 1f)]
    public float chanceTurn = 0.15f;
    [Tooltip("The chance (0-1) to place a 3-way intersection.")]
    [Range(0f, 1f)]
    public float chance3Way = 0.1f;
    // Note: Chance for 4-way cross is (1.0 - straight - turn - 3way)

    [Header("Building Placement")]
    [Tooltip("How far from the road to place houses.")]
    public float houseOffset = 10f;
    [Tooltip("A small vertical offset to prevent prefabs from z-fighting with the terrain.")]
    public float prefabVerticalOffset = 0.05f;
    public GameObject[] AChousePrefabs; 

    // --- Internal Generation Data ---
    private Terrain terrain;
    private WindingPathTerrainGenerator terrainGen;
    
    private Queue<RoadConnectionPoint> connectionQueue = new Queue<RoadConnectionPoint>();
    private List<KeyValuePair<Vector3, Quaternion>> straightRoads = new List<KeyValuePair<Vector3, Quaternion>>();
    private List<Vector3> roadNodes = new List<Vector3>(); // For connector roads
    

    public void Initialize(Terrain terr, WindingPathTerrainGenerator gen)
    {
        this.terrain = terr;
        this.terrainGen = gen;
        
        connectionQueue.Clear();
        straightRoads.Clear();
        roadNodes.Clear();

        if (terrainLayer.value == 0 || roadLayer.value == 0)
        {
            Debug.LogError("CRITICAL ERROR: 'Terrain Layer' or 'Road Layer' is not set in the CityGenerator Inspector! Please assign them.");
            return;
        }

        if (!ValidatePrefabs()) return;

        GenerateRoadNetwork();
        
        // --- FOCUS ON ROADS ---
        // GenerateHouses();
        // ConnectCityToPaths();
    }

    bool ValidatePrefabs()
    {
        if (roadStraight == null || roadTurn90 == null || road3Way == null || roadCross == null)
        {
            Debug.LogError("One or more road prefabs are not assigned!");
            return false;
        }
        if (AChousePrefabs == null || AChousePrefabs.Length == 0)
        {
            Debug.LogWarning("No house prefabs assigned. City will not have houses.");
        }
        return true;
    }

    void GenerateRoadNetwork()
    {
        // 1. Find the exact center
        float worldCenterX = terrain.transform.position.x + terrainGen.centerX;
        float worldCenterZ = terrain.transform.position.z + terrainGen.centerY;
        
        var (startPos, startRot, startSuccess) = AlignToTerrain(new Vector3(worldCenterX, 0, worldCenterZ));
        if (!startSuccess) { Debug.LogError("Could not find center of terrain!"); return; }

        // 2. Place the first "Roundabout" (a 4-way cross) and add its connections
        PlaceRoadPrefab(roadCross, startPos, Quaternion.identity); 

        // 3. Start the growth loop
        int placements = 0;
        while (placements < maxRoadPlacements && connectionQueue.Count > 0)
        {
            RoadConnectionPoint currentPoint = connectionQueue.Dequeue();

            // --- OVERLAP CHECK ---
            // Check if a road is already at the target *center*
            // We use the connection point's *position* as the target center
            if (Physics.CheckSphere(currentPoint.position, checkOverlapRadius, roadLayer))
            {
                continue; // This connection point is already filled, skip it
            }

            // --- BOUNDS CHECK ---
            if (!IsInFlatZone(currentPoint.position))
            {
                continue; // This point is outside the city, skip it
            }

            // --- PLACE A NEW ROAD PIECE ---
            float choice = Random.value;
            GameObject prefabToPlace;
            
            if (choice < chanceStraight)
            {
                prefabToPlace = roadStraight;
            }
            else if (choice < chanceStraight + chanceTurn)
            {
                prefabToPlace = roadTurn90;
            }
            else if (choice < chanceStraight + chanceTurn + chance3Way)
            {
                prefabToPlace = road3Way;
            }
            else
            {
                prefabToPlace = roadCross;
            }
            
            // Place the new prefab, centered on the connectionPoint.position
            PlaceRoadPrefab(prefabToPlace, currentPoint.position, currentPoint.rotation);
            placements++;
        }
        Debug.Log("Road generation complete. Placed " + placements + " segments.");
    }

    /// <summary>
    /// The master function to place, align, and register any road prefab.
    /// ASSUMES ALL PREFABS HAVE A CENTER PIVOT.
    /// </summary>
    void PlaceRoadPrefab(GameObject prefab, Vector3 centerPos, Quaternion incomingRot)
    {
        // 1. Align the *center* point to the terrain
        var (finalPos, finalRot, success) = AlignToTerrain(centerPos);
        if (!success) return; // Fail, raycast missed

        // 2. Place the prefab
        // We combine the terrain's normal (finalRot) with the road's desired direction (incomingRot)
        Quaternion finalPrefabRot = finalRot * incomingRot;
        GameObject newRoad = Instantiate(prefab, finalPos + (finalRot * Vector3.up * prefabVerticalOffset), finalPrefabRot);
        
        // --- FIX --- The problematic line is GONE.
        // The prefab's layer is set in the Inspector, not here.
        
        // 3. Add new connection points to the queue based on prefab type
        
        if (prefab == roadStraight)
        {
            // Add one connection point at the *exit*
            // finalPos = center. finalPrefabRot = direction.
            // The exit is at center + (direction * half_length)
            Vector3 exitPoint = finalPos + (finalPrefabRot * Vector3.forward * (straightLength / 2f));
            connectionQueue.Enqueue(new RoadConnectionPoint(exitPoint, incomingRot));
            
            straightRoads.Add(new KeyValuePair<Vector3, Quaternion>(finalPos, finalPrefabRot));
        }
        else if (prefab == roadTurn90)
        {
            // Randomly turn left or right
            bool turnLeft = (Random.value < 0.5f);
            float angle = turnLeft ? -90f : 90f;
            Quaternion newRotation = incomingRot * Quaternion.Euler(0, angle, 0);

            // Calculate the exit point (assumes center pivot)
            // The exit is at center + (new_direction * half_width)
            Vector3 exitDir = (turnLeft ? Vector3.left : Vector3.right);
            Vector3 exitPoint = finalPos + (finalPrefabRot * exitDir * (turnWidth / 2f));
            
            connectionQueue.Enqueue(new RoadConnectionPoint(exitPoint, newRotation));
        }
        else if (prefab == road3Way)
        {
            // Assumes pivot is center, and "incoming" is the bottom of the T
            
            // Left Branch
            Quaternion rotL = incomingRot * Quaternion.Euler(0, -90, 0);
            Vector3 posL = finalPos + (finalPrefabRot * (Vector3.left * (tJunctionWidth / 2f))); 
            connectionQueue.Enqueue(new RoadConnectionPoint(posL, rotL));
            
            // Right Branch
            Quaternion rotR = incomingRot * Quaternion.Euler(0, 90, 0);
            Vector3 posR = finalPos + (finalPrefabRot * (Vector3.right * (tJunctionWidth / 2f))); 
            connectionQueue.Enqueue(new RoadConnectionPoint(posR, rotR));

            roadNodes.Add(finalPos); 
        }
        else if (prefab == roadCross)
        {
            // Assumes pivot is center. Adds 3 new exits (forward, left, right)
            
            // Forward (the one we didn't come from)
            Vector3 posF = finalPos + (finalPrefabRot * Vector3.forward * (crossLength / 2f));
            connectionQueue.Enqueue(new RoadConnectionPoint(posF, incomingRot));

            // Left
            Quaternion rotL = incomingRot * Quaternion.Euler(0, -90, 0);
            Vector3 posL = finalPos + (finalPrefabRot * (Vector3.left * (crossWidth / 2f))); 
            connectionQueue.Enqueue(new RoadConnectionPoint(posL, rotL));
            
            // Right
            Quaternion rotR = incomingRot * Quaternion.Euler(0, 90, 0);
            Vector3 posR = finalPos + (finalPrefabRot * (Vector3.right * (crossWidth / 2f))); 
            connectionQueue.Enqueue(new RoadConnectionPoint(posR, rotR));

            roadNodes.Add(finalPos);
        }
    }
    
    // Helper to get the primary length (Z-axis) of a prefab
    float GetPrefabLength(GameObject prefab)
    {
        if (prefab == roadStraight) return straightLength;
        if (prefab == roadTurn90) return turnLength;
        if (prefab == road3Way) return tJunctionLength;
        if (prefab == roadCross) return crossLength;
        return 0f;
    }

    void GenerateHouses()
    {
        if (AChousePrefabs == null || AChousePrefabs.Length == 0) return;

        foreach (var roadSegment in straightRoads)
        {
            Vector3 roadPos = roadSegment.Key;
            Quaternion roadRot = roadSegment.Value;

            Vector3 rightDir = roadRot * Vector3.right; 
            
            Vector3 spot1 = roadPos + rightDir * houseOffset;
            Vector3 spot2 = roadPos - rightDir * houseOffset;

            if (IsInFlatZone(spot1))
            {
                var (pos1, rot1, success1) = AlignToTerrain(spot1);
                if (success1)
                {
                    GameObject housePrefab = AChousePrefabs[Random.Range(0, AChousePrefabs.Length)];
                    Instantiate(housePrefab, pos1 + (rot1 * Vector3.up * prefabVerticalOffset), rot1 * Quaternion.LookRotation(-rightDir));
                }
            }

            if (IsInFlatZone(spot2))
            {
                var (pos2, rot2, success2) = AlignToTerrain(spot2);
                if (success2)
                {
                    GameObject housePrefab = AChousePrefabs[Random.Range(0, AChousePrefabs.Length)];
                    Instantiate(housePrefab, pos2 + (rot2 * Vector3.up * prefabVerticalOffset), rot2 * Quaternion.LookRotation(rightDir));
                }
            }
        }
    }

    void ConnectCityToPaths()
    {
        if (terrainGen.paths == null || terrainGen.paths.Count == 0 || roadNodes.Count == 0) return;

        for (int i = 0; i < numConnectingRoads; i++)
        {
            Vector3 startPos = roadNodes[Random.Range(0, roadNodes.Count)];
            
            Vector2[] randomPath = terrainGen.paths[Random.Range(0, terrainGen.paths.Count)];
            Vector2 targetPoint2D = randomPath[Random.Range(0, randomPath.Length)];
            
            Vector3 endPos = new Vector3(
                targetPoint2D.x + terrain.transform.position.x, 
                0, 
                targetPoint2D.y + terrain.transform.position.z); 

            StartCoroutine(BuildConnectorRoad(startPos, endPos));
        }
    }

    IEnumerator BuildConnectorRoad(Vector3 startPos, Vector3 endPos)
    {
        Vector3 currentPos = startPos;
        int maxSteps = 200; 

        for (int i = 0; i < maxSteps; i++)
        {
            float distToTarget = Vector3.Distance(new Vector2(currentPos.x, currentPos.z), new Vector2(endPos.x, endPos.z));

            if (distToTarget < straightLength * 1.5f) // Stop if we're close
            {
                yield break; 
            }

            Vector3 targetDir = (endPos - currentPos).normalized;
            targetDir.y = 0;
            
            // Calculate the center of the segment we're about to place
            Vector3 segmentCenter = currentPos + targetDir * (straightLength / 2f);
            
            // Place the prefab and get its *new* exit point
            Vector3 nextPos = segmentCenter + targetDir * (straightLength / 2f);
            
            // Check for overlap at the *new* position
            if (Physics.CheckSphere(nextPos, checkOverlapRadius, roadLayer))
            {
                yield break; // We ran into another road
            }

            PlaceRoadPrefab(roadStraight, segmentCenter, Quaternion.LookRotation(targetDir));

            // Move to the next connection point
            currentPos = nextPos;
            
            yield return null; 
        }
    }

    (Vector3, Quaternion, bool) AlignToTerrain(Vector3 originalPos, float raycastHeight = 50f)
    {
        Vector3 rayStart = new Vector3(originalPos.x, 1000f, originalPos.z);
        
        if (Physics.Raycast(rayStart, Vector3.down, out RaycastHit hit, 2000f, terrainLayer)) 
        {
            Vector3 finalPos = hit.point;
            Quaternion rotation = Quaternion.FromToRotation(Vector3.up, hit.normal);
            return (finalPos, rotation, true);
        }
        return (Vector3.zero, Quaternion.identity, false); 
    }

    bool IsInFlatZone(Vector3 worldPos)
    {
        if (terrainGen == null) return false;

        float localX = worldPos.x - terrain.transform.position.x;
        float localZ = worldPos.z - terrain.transform.position.z;

        float dist = Vector2.Distance(new Vector2(localX, localZ), new Vector2(terrainGen.centerX, terrainGen.centerY));
        
        return dist <= terrainGen.centerRadius;
    }
}