using UnityEngine;
using UnityEngine.Splines; // <-- Need this!
using System.Collections.Generic;
using System.Linq;

public class RoadGenerator : MonoBehaviour
{
    [Header("Databases")]
    public IntersectionDatabase IntersectionDB;
    public Material RoadMaterial;

    [Header("Road Settings")]
    public float RoadWidth = 8f;
    public float RoadMeshStepSize = 1f;
    public float RoadTextureTileLength = 10f;

    [Header("Graph Data")]
    public List<RoadNode> Nodes = new List<RoadNode>();
    public List<RoadEdge> Edges = new List<RoadEdge>();

    [Header("Debug")]
    public bool RunOnStart = true;
    public Transform NodeParent;
    public Transform EdgeParent; // <-- This variable lives here

    private Dictionary<int, GameObject> _spawnedIntersections = new Dictionary<int, GameObject>();

    void Start()
    {
        if (RunOnStart)
        {
            GenerateCity();
        }
    }

    public void GenerateCity()
    {
        // Clear old data
        _spawnedIntersections.Clear();
        if (NodeParent != null)
        {
            foreach (Transform child in NodeParent) { Destroy(child.gameObject); }
        }
        if (EdgeParent != null)
        {
            foreach (Transform child in EdgeParent) { Destroy(child.gameObject); }
        }
        

        // === PART I: TOPOLOGY GENERATION ===
        GenerateDebugTopology();

        // === PART IV: INTERSECTION HANDLING ===
        InstantiateIntersections();
        
        // === PART III: GEOMETRIC REALIZATION ===
        GenerateRoadMeshes();
    }

    /// <summary>
    /// (Stub Function) Replaced by your real topology algorithm.
    /// This just creates a simple 4-way crossroad for testing.
    /// </summary>
    void GenerateDebugTopology()
    {
        Nodes.Clear();
        Edges.Clear();
        RoadNode center = new RoadNode(0, Vector3.zero);
        RoadNode north = new RoadNode(1, new Vector3(0, 0, 50));
        RoadNode south = new RoadNode(2, new Vector3(0, 0, -50));
        RoadNode east = new RoadNode(3, new Vector3(50, 0, 0));
        RoadNode west = new RoadNode(4, new Vector3(-50, 0, 0));
        Nodes.AddRange(new[] { center, north, south, east, west });
        Edges.Add(new RoadEdge(0, center, north));
        Edges.Add(new RoadEdge(1, center, south));
        Edges.Add(new RoadEdge(2, center, east));
        Edges.Add(new RoadEdge(3, center, west));
        center.ConnectedEdges.AddRange(Edges);
        north.ConnectedEdges.Add(Edges[0]);
        south.ConnectedEdges.Add(Edges[1]);
        east.ConnectedEdges.Add(Edges[2]);
        west.ConnectedEdges.Add(Edges[3]);
    }

    /// <summary>
    /// Reads the graph data and spawns the correct intersection
    /// prefabs based on the node's connection count.
    /// </summary>
    void InstantiateIntersections()
    {
        if (NodeParent == null)
        {
            NodeParent = new GameObject("Intersections").transform;
            NodeParent.parent = this.transform;
        }

        foreach (RoadNode node in Nodes)
        {
            int count = node.ConnectedEdges.Count;
            if (count == 0 || count == 2) continue; 

            GameObject prefab = IntersectionDB.GetPrefabForConnectionCount(count);
            
            if (prefab != null)
            {
                GameObject instance = Instantiate(prefab, node.Position, Quaternion.identity, NodeParent);
                
                // --- Simple Alignment Logic ---
                RoadEdge firstEdge = node.ConnectedEdges[0];
                RoadNode otherNode = (firstEdge.StartNode == node) ? firstEdge.EndNode : firstEdge.StartNode;
                Vector3 edgeDir = (otherNode.Position - node.Position).normalized;
                
                IntersectionSocket firstSocket = instance.GetComponentInChildren<IntersectionSocket>();
                if (firstSocket != null)
                {
                    Quaternion targetRotation = Quaternion.LookRotation(edgeDir, Vector3.up);
                    Quaternion socketOffset = Quaternion.Inverse(firstSocket.transform.localRotation);
                    instance.transform.rotation = targetRotation * socketOffset;
                }

                _spawnedIntersections.Add(node.ID, instance);
            }
        }
    }

    /// <summary>
    /// Implements Part III by connecting intersection sockets with splines
    /// and generating a mesh along them.
    /// </summary>
    void GenerateRoadMeshes()
    {
        if (EdgeParent == null)
        {
            EdgeParent = new GameObject("Roads").transform;
            EdgeParent.parent = this.transform;
        }

        if (RoadMaterial == null)
        {
            Debug.LogError("No Road Material assigned to RoadGenerator!");
            return;
        }

        foreach (RoadEdge edge in Edges)
        {
            RoadNode startNode = edge.StartNode;
            RoadNode endNode = edge.EndNode;

            Transform startSocket = FindSocketToConnect(startNode, endNode);
            Transform endSocket = FindSocketToConnect(endNode, startNode);

            if (startSocket == null) startSocket = GetSocketFallback(startNode);
            if (endSocket == null) endSocket = GetSocketFallback(endNode);
            
            // --- 1. Create the Spline ---
            SplineContainer splineContainer = new GameObject($"Road_{edge.ID}").AddComponent<SplineContainer>();
            splineContainer.transform.SetParent(EdgeParent, false);
            Spline spline = splineContainer.Spline;

            var startKnot = new BezierKnot(
                startSocket.position, 
                Vector3.zero, 
                startSocket.forward * (edge.Length * 0.33f), 
                startSocket.rotation
            );

            var endKnot = new BezierKnot(
                endSocket.position, 
                -endSocket.forward * (edge.Length * 0.33f), 
                Vector3.zero, 
                endSocket.rotation
            );

            spline.Add(startKnot);
            spline.Add(endKnot);

            // --- 2. Create the Mesh ---
            // This is where we call the *other script*
            Mesh roadMesh = RoadMeshGenerator.GenerateRoadMesh(
                spline, 
                RoadWidth, 
                RoadMeshStepSize, 
                RoadTextureTileLength
            );
            
            // --- 3. Add the Mesh to the scene ---
            GameObject meshGO = new GameObject($"RoadMesh_{edge.ID}");
            meshGO.transform.SetParent(EdgeParent, false);
            
            meshGO.AddComponent<MeshFilter>().mesh = roadMesh;
            meshGO.AddComponent<MeshRenderer>().material = RoadMaterial;
        }
    }

    /// <summary>
    /// Helper: Finds the best socket on a spawned intersection
    /// to connect to another node.
    /// </summary>
    private Transform FindSocketToConnect(RoadNode fromNode, RoadNode toNode)
    {
        if (!_spawnedIntersections.ContainsKey(fromNode.ID))
            return null; 

        GameObject intersection = _spawnedIntersections[fromNode.ID];
        IntersectionSocket[] sockets = intersection.GetComponentsInChildren<IntersectionSocket>();

        if (sockets.Length == 0) return null;

        Transform bestSocket = sockets[0].transform;
        float bestDot = -1f;
        Vector3 targetDir = (toNode.Position - fromNode.Position).normalized;

        foreach (var socket in sockets)
        {
            float dot = Vector3.Dot(socket.transform.forward, targetDir);
            if (dot > bestDot)
            {
                bestDot = dot;
                bestSocket = socket.transform;
            }
        }
        return bestSocket;
    }

    /// <summary>
    /// Helper: If a node isn't a spawned intersection (e.g., a curve),
    /// we create a "virtual" socket transform to connect to.
    /// </summary>
    private Transform GetSocketFallback(RoadNode node)
    {
        // This creates a temporary, disposable transform to act as a socket
        GameObject fallBackGO = new GameObject($"FallbackSocket_{node.ID}");
        fallBackGO.transform.position = node.Position;
        
        // This logic makes curves and dead ends work
        if (node.ConnectedEdges.Count == 1)
        {
            RoadEdge edge = node.ConnectedEdges[0];
            RoadNode otherNode = (edge.StartNode == node) ? edge.EndNode : edge.StartNode;
            fallBackGO.transform.rotation = Quaternion.LookRotation(node.Position - otherNode.Position);
        }
        else if (node.ConnectedEdges.Count == 2)
        {
            RoadEdge edgeA = node.ConnectedEdges[0];
            RoadNode neighborA = (edgeA.StartNode == node) ? edgeA.EndNode : edgeA.StartNode;
            Vector3 dirA = (neighborA.Position - node.Position).normalized;

            RoadEdge edgeB = node.ConnectedEdges[1];
            RoadNode neighborB = (edgeB.StartNode == node) ? edgeB.EndNode : edgeB.StartNode;
            Vector3 dirB = (neighborB.Position - node.Position).normalized;

            Vector3 tangent = (dirA - dirB).normalized;
            fallBackGO.transform.rotation = Quaternion.LookRotation(tangent);
        }
        
        // We will destroy this temporary object later
        Destroy(fallBackGO, 0.1f); 
        return fallBackGO.transform;
    }
}