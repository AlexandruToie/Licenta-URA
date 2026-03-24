using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TrafficManager : MonoBehaviour
{
    [Header("References")]
    public RoadGenerator roadGenerator;     
    public RoadGridManager gridManager;     
    public BuildingGenerator buildingGenerator; 
    public Camera mainCamera; 
    
    public Terrain cityTerrain; 

    [Header("Car Settings")]
    public List<GameObject> carPrefabs; 
    public int maxCars = 50;
    public float spawnInterval = 2f;
    public float verticalOffset = 0.05f; 

    [Header("Spawn Logic")]
    public float minSpawnDistance = 30f;
    public float maxSpawnDistance = 150f;

    [Header("Road Detection (Strict)")]
    public LayerMask RoadLayer;
    public float detectionRadius = 0.25f; 
    
    [Header("Debug")]
    public bool showGizmos = true;

    private Dictionary<Vector2Int, TrafficNode> trafficGraph = new Dictionary<Vector2Int, TrafficNode>();
    private List<GameObject> activeCars = new List<GameObject>();
    private List<Vector2Int> roundaboutCenters = new List<Vector2Int>();

    void Start() //Initialize the traffic system after buildings are generated
    {
        if (buildingGenerator != null)
        {
            buildingGenerator.OnBuildingsGenerated += InitializeTrafficSystem;
        }
    }

    private void OnDestroy() // Unsubscribe from event
    {
        if (buildingGenerator != null) buildingGenerator.OnBuildingsGenerated -= InitializeTrafficSystem;
    }

    void InitializeTrafficSystem() // Called when buildings are generated
    {
        StartCoroutine(BuildGraphAndStartTraffic());
    }

    IEnumerator BuildGraphAndStartTraffic() // Wait a bit for physics to settle
    {
        //Debug.Log("[Traffic] I wait a bit for physics to settle...");
        yield return new WaitForSeconds(0.5f); 

        BuildTrafficGraph();
        StartCoroutine(SpawnRoutine());
    }

    void BuildTrafficGraph() // Scan the area and build the traffic graph
    {
        trafficGraph.Clear();

        if (roadGenerator != null)
        {
            roundaboutCenters = roadGenerator.GeneratedRoundabouts;
        }

        int radius = 100;
        if (gridManager != null) radius = Mathf.CeilToInt(gridManager.BuildRadius);
        
        Vector2Int centerMap = Vector2Int.zero; 
        if (gridManager != null)
            //The center of the build area
            centerMap = new Vector2Int(Mathf.RoundToInt(gridManager.BuildAreaCenter.position.x), Mathf.RoundToInt(gridManager.BuildAreaCenter.position.z)); 

            for (int x = -radius; x <= radius; x++)
            {
                for (int y = -radius; y <= radius; y++)
                {
                    Vector2Int pos = centerMap + new Vector2Int(x, y);
                    bool isRoad = false;

                    // 1. Virtual Verifycation (RoadGenerator)
                    if (roadGenerator.IsCellRoad(pos)) 
                    {
                        isRoad = true;
                    }
                    // 2. Physical Verification (Physics.CheckSphere)
                    else 
                    {
                        float tHeight = GetTerrainHeightAt(pos.x, pos.y);
                        Vector3 checkPos = new Vector3(pos.x, tHeight + 0.2f, pos.y);
                        if (Physics.CheckSphere(checkPos, detectionRadius, RoadLayer))
                        {
                            isRoad = true;
                        }
                    }
                    if (isRoad) 
                    {
                        float finalY = GetTerrainHeightAt(pos.x, pos.y);
                        trafficGraph.Add(pos, new TrafficNode(pos, new Vector3(pos.x, finalY, pos.y)));
                    }
                }
            }


            foreach (var kvp in trafficGraph)
            {
                Vector2Int pos = kvp.Key;
                TrafficNode node = kvp.Value;
                Vector2Int roundaboutCenter = GetRoundaboutCenterForNode(pos);

                if (roundaboutCenter != Vector2Int.zero)
                {
                    ConnectRoundaboutNode(node, pos, roundaboutCenter);
                }
                else
                {
                    ConnectIfRoad(node, pos + Vector2Int.up);
                    ConnectIfRoad(node, pos + Vector2Int.down);
                    ConnectIfRoad(node, pos + Vector2Int.left);
                    ConnectIfRoad(node, pos + Vector2Int.right);
                }
            }
            //Debug.Log($"[Traffic] Done! {trafficGraph.Count} roads nodes are created.");
        }

        void OnDrawGizmos() // Visualize the traffic graph in the Scene view
        {
            if (!showGizmos || trafficGraph == null) return;

            foreach (var node in trafficGraph.Values) // Draw each node
            {
                Gizmos.color = new Color(0, 0, 1, 0.5f); // Semi-transparent blue for nodes
                Gizmos.DrawSphere(node.WorldPosition, 0.15f); // Node position for traffic
                Gizmos.color = Color.yellow; // Yellow for connections
                foreach (var neighbor in node.Neighbors) // Draw connections to neighbors
                {

                    Vector3 direction = (neighbor.WorldPosition - node.WorldPosition) * 0.8f;
                    Gizmos.DrawRay(node.WorldPosition, direction);
                }
            }
        }
    

        float GetTerrainHeightAt(int x, int z) // Sample terrain height with offset
        {
            if (cityTerrain != null)
            {
                return cityTerrain.transform.position.y + cityTerrain.SampleHeight(new Vector3(x, 0, z)) + verticalOffset;
            }
            return verticalOffset;
        }

    Vector2Int GetRoundaboutCenterForNode(Vector2Int nodePos) // Check if node is part of a roundabout
    {
        foreach(var center in roundaboutCenters)
        {
            if (Mathf.Abs(nodePos.x - center.x) <= 1 && Mathf.Abs(nodePos.y - center.y) <= 1)
            {
                if (nodePos == center) return Vector2Int.zero; 
                return center;
            }
        }
        return Vector2Int.zero;
    }

    void ConnectRoundaboutNode(TrafficNode node, Vector2Int pos, Vector2Int center) // Connect nodes in a roundabout
    {
        int dx = pos.x - center.x;
        int dy = pos.y - center.y;
        Vector2Int nextStep = Vector2Int.zero;
        Vector2Int exitStep = Vector2Int.zero;
        
        // Entraces
        if (dx == -1 && dy == -1) nextStep = new Vector2Int(0, -1); 
        else if (dx == 1 && dy == -1) nextStep = new Vector2Int(1, 0); 
        else if (dx == 1 && dy == 1)  nextStep = new Vector2Int(0, 1);  
        else if (dx == -1 && dy == 1) nextStep = new Vector2Int(-1, 0); 

        // Exits
        else if (dx == 0 && dy == -1) { nextStep = new Vector2Int(1, -1); exitStep = new Vector2Int(0, -2); }
        else if (dx == 1 && dy == 0)  { nextStep = new Vector2Int(1, 1);  exitStep = new Vector2Int(2, 0); }
        else if (dx == 0 && dy == 1)  { nextStep = new Vector2Int(-1, 1); exitStep = new Vector2Int(0, 2); }
        else if (dx == -1 && dy == 0) { nextStep = new Vector2Int(-1, -1); exitStep = new Vector2Int(-2, 0); }

        Vector2Int nextPos = center + nextStep;
        if (trafficGraph.ContainsKey(nextPos)) node.Neighbors.Add(trafficGraph[nextPos]); // Always connect to next in roundabout

        if (exitStep != Vector2Int.zero)
        {
            Vector2Int exitPos = center + exitStep;
            if (trafficGraph.ContainsKey(exitPos)) node.Neighbors.Add(trafficGraph[exitPos]); // Connect to exit if applicable
        }
    }

    void ConnectIfRoad(TrafficNode currentNode, Vector2Int neighborPos) // Connect to neighbor if it exists
    {
        if (trafficGraph.ContainsKey(neighborPos)) currentNode.Neighbors.Add(trafficGraph[neighborPos]);
    }

    IEnumerator SpawnRoutine() // Periodically attempt to spawn cars
    {
        while (true)
        {
            yield return new WaitForSeconds(spawnInterval);
            activeCars.RemoveAll(c => c == null);
            if (activeCars.Count < maxCars) TrySpawnCarAtHouse();
        }
    }

    void TrySpawnCarAtHouse() // Spawn cars near inhabited houses
    {
        if (buildingGenerator == null || buildingGenerator.InhabitedHousePositions.Count == 0) return;
        List<Vector2Int> validHouses = new List<Vector2Int>();
        Vector3 camPos = mainCamera != null ? mainCamera.transform.position : Vector3.zero;

        foreach (Vector2Int housePos in buildingGenerator.InhabitedHousePositions)
        {
            float dist = Vector2.Distance(new Vector2(camPos.x, camPos.z), housePos);
            if (dist > minSpawnDistance && dist < maxSpawnDistance) validHouses.Add(housePos);
        }

        if (validHouses.Count == 0) return;
        Vector2Int chosenHouse = validHouses[Random.Range(0, validHouses.Count)];
        TrafficNode startNode = FindNearestRoad(chosenHouse);
        if (startNode != null) SpawnCar(startNode);
    }

    TrafficNode FindNearestRoad(Vector2Int housePos) // Check adjacent cells for nearest road
    {
        Vector2Int[] dirs = { Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right };
        foreach (var dir in dirs)
        {
            Vector2Int checkPos = housePos + dir;
            if (trafficGraph.ContainsKey(checkPos)) return trafficGraph[checkPos];
        }
        return null;
    }

    void SpawnCar(TrafficNode startNode) // Instantiate car and set it on its path
    {
        if (carPrefabs.Count == 0) return;
        GameObject prefab = carPrefabs[Random.Range(0, carPrefabs.Count)];
        GameObject car = Instantiate(prefab, startNode.WorldPosition, Quaternion.identity);
        CarController controller = car.GetComponent<CarController>();
        if (controller != null) controller.Setup(startNode);
        activeCars.Add(car);
    }

    public TrafficNode GetNode(Vector2Int pos) // Public method to access traffic nodes
    {
        return trafficGraph.ContainsKey(pos) ? trafficGraph[pos] : null;
    }
}