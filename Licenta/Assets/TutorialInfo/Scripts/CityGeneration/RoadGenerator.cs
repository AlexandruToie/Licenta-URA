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

    [Header("Visual Adjustments")]
    public float StraightRotationOffset = 90f;
    public float CornerRotationOffset = 180f;
    public float ThreeWayRotationOffset = 0f; 
    public float FourWayRotationOffset = 0f;

    [Header("Generation Settings")]
    [Tooltip("CATE drumuri magistrale sa genereze.")]
    public int MaxRoadsToBuild = 2; 
    
    public int NumberOfPOIs = 10; 
    public int MaxPlacementAttemptsPerPOI = 20;
    public float MinDistanceBetweenPOIs = 40f; 
    public float SpawnEdgePadding = 10f;
    
    [Tooltip("Lungimea drumului drept din fata cladirii.")]
    public int RunwayLength = 5;
    
    [Tooltip("Spatiul liber obligatoriu in jurul POI-urilor (Buffer).")]
    public int POIBufferSize = 2;
    
    public float StepDelay = 0.05f; 

    // --- DATA ---
    private HashSet<Vector2Int> roadMapVirtual = new HashSet<Vector2Int>();
    
    private struct WorldSocket
    {
        public Vector2Int Position;
        public Vector2Int Direction; 
        public PlacedPOI OwnerPOI;
    }
    private List<WorldSocket> allSockets = new List<WorldSocket>();
    private HashSet<Vector2Int> protectedAccessPoints = new HashSet<Vector2Int>(); 
    private HashSet<Vector2Int> poiRestrictedZone = new HashSet<Vector2Int>();

    private class PlacedPOI { public Vector2Int GridPosition; public PrefabData Data; }
    private List<PlacedPOI> placedPOIs = new List<PlacedPOI>();

    private List<GameObject> debugMarkers = new List<GameObject>();

    void Start()
    {
        gridManager = GetComponent<RoadGridManager>();
        pathfinder = new RoadPathfinder(gridManager);
        StartCoroutine(GenerateSequence());
    }

    IEnumerator GenerateSequence()
    {
        // 1. Reset
        roadMapVirtual.Clear();
        allSockets.Clear();
        protectedAccessPoints.Clear();
        poiRestrictedZone.Clear();
        placedPOIs.Clear();
        foreach(var obj in debugMarkers) Destroy(obj);
        debugMarkers.Clear();
        yield return null;

        // 2. Plasare & Configurare
        PlaceAllPOIs();
        yield return new WaitForSeconds(StepDelay);
        IdentifyAllSockets();
        GeneratePOIBuffers(); 

        // 3. GENERARE MULTIPLA
        yield return StartCoroutine(GenerateMultipleSewingRoads());
        
        Debug.Log($"[Generator] Gata! Am generat {MaxRoadsToBuild} drumuri.");
    }

    private void GeneratePOIBuffers()
    {
        foreach(var poi in placedPOIs)
        {
            int startX = poi.GridPosition.x - (poi.Data.Size.x / 2) - POIBufferSize;
            int startY = poi.GridPosition.y - (poi.Data.Size.y / 2) - POIBufferSize;
            int endX = poi.GridPosition.x + (poi.Data.Size.x / 2) + POIBufferSize;
            int endY = poi.GridPosition.y + (poi.Data.Size.y / 2) + POIBufferSize;

            for (int x = startX; x < endX; x++)
            {
                for (int y = startY; y < endY; y++)
                {
                    Vector2Int pos = new Vector2Int(x, y);
                    if (!protectedAccessPoints.Contains(pos))
                    {
                        poiRestrictedZone.Add(pos);
                    }
                }
            }
        }
    }

    IEnumerator GenerateMultipleSewingRoads()
    {
        int roadsBuilt = 0;
        List<PlacedPOI> usedPOIs = new List<PlacedPOI>();

        while (roadsBuilt < MaxRoadsToBuild)
        {
            Debug.Log($"[Generator] Incep Drumul #{roadsBuilt + 1}...");

            // PAS 1: Alegem 2 POI-uri nefolosite (sau cat mai departate)
            PlacedPOI poiA = null;
            PlacedPOI poiB = null;
            float maxDist = -1f;

            foreach(var p1 in placedPOIs)
            {
                if (usedPOIs.Contains(p1)) continue; // Evitam refolosirea imediata
                foreach(var p2 in placedPOIs)
                {
                    if (p1 == p2) continue;
                    if (usedPOIs.Contains(p2)) continue;

                    float dist = Vector2Int.Distance(p1.GridPosition, p2.GridPosition);
                    if (dist > maxDist)
                    {
                        maxDist = dist;
                        poiA = p1;
                        poiB = p2;
                    }
                }
            }

            // Daca nu mai avem perechi unice, ne oprim
            if (poiA == null || poiB == null) 
            {
                Debug.LogWarning("Nu mai am POI-uri libere pentru drumuri noi!");
                break;
            }

            usedPOIs.Add(poiA);
            usedPOIs.Add(poiB);

            // Markere Vizuale (Albastru pt A, Cyan pt B)
            CreateDebugSphere(poiA.GridPosition, Color.blue, $"R{roadsBuilt}_A");
            CreateDebugSphere(poiB.GridPosition, Color.cyan, $"R{roadsBuilt}_B");

            // PAS 2: Calculam Start si End la margine
            Vector2 direction = ((Vector2)(poiB.GridPosition - poiA.GridPosition)).normalized;
            Vector2 center = new Vector2(gridManager.BuildAreaCenter.position.x, gridManager.BuildAreaCenter.position.z);
            float radius = gridManager.BuildRadius - 5f;

            Vector2 edgeStartVec = center - (direction * radius);
            Vector2 edgeEndVec = center + (direction * radius);

            Vector2Int mapStart = new Vector2Int(Mathf.RoundToInt(edgeStartVec.x), Mathf.RoundToInt(edgeStartVec.y));
            Vector2Int mapEnd = new Vector2Int(Mathf.RoundToInt(edgeEndVec.x), Mathf.RoundToInt(edgeEndVec.y));

            // Culori diferite pentru drumuri diferite
            Color startCol = (roadsBuilt == 0) ? Color.green : Color.yellow;
            Color endCol = (roadsBuilt == 0) ? Color.red : Color.magenta;

            CreateDebugSphere(mapStart, startCol, $"R{roadsBuilt}_Start");
            CreateDebugSphere(mapEnd, endCol, $"R{roadsBuilt}_End");

            // === CONSTRUIRE SEGMENTE ===

            // Segment 1: Margine -> POI A
            WorldSocket entrySocketA = GetBestEntrySocket(poiA, mapStart);
            List<Vector2Int> runwayEntryA = BuildRunway(entrySocketA);
            Vector2Int targetPosA = runwayEntryA.Last(); 
            Vector2Int startDir = GetCardinalDirection(entrySocketA.Position - mapStart);
            
            yield return StartCoroutine(RunPathfinderAndBuild(mapStart, targetPosA, startDir));

            // Segment 2: POI A -> POI B
            WorldSocket exitSocketA = GetBestExitSocket(poiA, entrySocketA, poiB.GridPosition);
            List<Vector2Int> runwayExitA = BuildRunway(exitSocketA);
            Vector2Int startPosS2 = runwayExitA.Last();
            Vector2Int dirS2 = exitSocketA.Direction;

            WorldSocket entrySocketB = GetBestEntrySocket(poiB, exitSocketA.Position);
            List<Vector2Int> runwayEntryB = BuildRunway(entrySocketB);
            Vector2Int targetPosS2 = runwayEntryB.Last();

            yield return StartCoroutine(RunPathfinderAndBuild(startPosS2, targetPosS2, dirS2));

            // Segment 3: POI B -> Margine
            WorldSocket exitSocketB = GetBestExitSocket(poiB, entrySocketB, mapEnd);
            List<Vector2Int> runwayExitB = BuildRunway(exitSocketB);
            Vector2Int startPosS3 = runwayExitB.Last();
            Vector2Int dirS3 = exitSocketB.Direction;

            yield return StartCoroutine(RunPathfinderAndBuild(startPosS3, mapEnd, dirS3));

            roadsBuilt++;
            yield return new WaitForSeconds(StepDelay * 5); // Pauza intre drumuri
        }
    }

    // --- Helpers ---

    private WorldSocket GetBestEntrySocket(PlacedPOI poi, Vector2Int fromPos)
    {
        WorldSocket best = allSockets.FirstOrDefault(s => s.OwnerPOI == poi);
        float minDist = float.MaxValue;
        foreach(var s in allSockets)
        {
            if (s.OwnerPOI != poi) continue;
            float d = Vector2Int.Distance(s.Position, fromPos);
            if (d < minDist) { minDist = d; best = s; }
        }
        return best;
    }

    private WorldSocket GetBestExitSocket(PlacedPOI poi, WorldSocket entrySocket, Vector2Int targetPos)
    {
        WorldSocket best = entrySocket; 
        float maxAlignment = -2f; 
        foreach(var s in allSockets)
        {
            if (s.OwnerPOI != poi) continue;
            if (s.Position == entrySocket.Position) continue; 

            Vector2 dirToTarget = ((Vector2)(targetPos - s.Position)).normalized;
            float alignment = Vector2.Dot(s.Direction, dirToTarget);

            if (Vector2.Dot(s.Direction, entrySocket.Direction) < -0.9f) alignment += 2.0f; 
            if (alignment > maxAlignment) { maxAlignment = alignment; best = s; }
        }
        return best;
    }

    private List<Vector2Int> BuildRunway(WorldSocket socket)
    {
        List<Vector2Int> points = new List<Vector2Int>();
        points.Add(socket.Position);
        List<Vector2Int> extension = GetRunwayPixels(socket.Position, socket.Direction, RunwayLength);
        points.AddRange(extension);

        foreach(var p in points)
        {
            if (!roadMapVirtual.Contains(p))
            {
                roadMapVirtual.Add(p);
                UpdateSingleRoadVisual(p, true); 
            }
        }
        return points;
    }

    IEnumerator RunPathfinderAndBuild(Vector2Int start, Vector2Int end, Vector2Int dir)
    {
        // Pasam poiRestrictedZone si protectedAccessPoints
        List<Vector2Int> path = pathfinder.FindPath(start, end, roadMapVirtual, protectedAccessPoints, dir, poiRestrictedZone);
        
        if (path != null)
        {
            foreach (var p in path)
            {
                if (!roadMapVirtual.Contains(p))
                {
                    roadMapVirtual.Add(p);
                    UpdateSingleRoadVisual(p, false);
                }
            }
            foreach (var p in path) RefreshNeighborsVisuals(p);
            RefreshNeighborsVisuals(start);
            RefreshNeighborsVisuals(end);
        }
        else
        {
            Debug.LogError($"[Pathfinder] Nu am gasit drum de la {start} la {end}!");
        }
        yield return null;
    }

    private void UpdateSingleRoadVisual(Vector2Int pos, bool forceStraight)
    {
        gridManager.RemovePrefabAt(pos);

        var socketInfo = allSockets.FirstOrDefault(s => s.Position == pos);
        
        if (forceStraight || socketInfo.OwnerPOI != null)
        {
            float rot = 0;
            if (socketInfo.OwnerPOI != null)
                rot = (socketInfo.Direction.y != 0) ? (0 + StraightRotationOffset) : (90 + StraightRotationOffset);
            else
            {
                bool hasVert = roadMapVirtual.Contains(pos + Vector2Int.up) || roadMapVirtual.Contains(pos + Vector2Int.down);
                rot = hasVert ? (0 + StraightRotationOffset) : (90 + StraightRotationOffset);
            }
            gridManager.PlacePrefab(roadStraight, pos, Quaternion.Euler(0, rot, 0));
            return;
        }

        bool up = roadMapVirtual.Contains(pos + Vector2Int.up);
        bool down = roadMapVirtual.Contains(pos + Vector2Int.down);
        bool left = roadMapVirtual.Contains(pos + Vector2Int.left);
        bool right = roadMapVirtual.Contains(pos + Vector2Int.right);

        CheckNeighborSocket(pos + Vector2Int.up, Vector2Int.down, ref up);
        CheckNeighborSocket(pos + Vector2Int.down, Vector2Int.up, ref down);
        CheckNeighborSocket(pos + Vector2Int.left, Vector2Int.right, ref left);
        CheckNeighborSocket(pos + Vector2Int.right, Vector2Int.left, ref right);

        int count = (up?1:0) + (down?1:0) + (left?1:0) + (right?1:0);
        PrefabData prefab = roadStraight;
        float rY = 0;

        if (count == 4) { prefab = road4Way; rY = 0 + FourWayRotationOffset; }
        else if (count == 3)
        {
            prefab = road3Way;
            if (!up) rY = 90; else if (!down) rY = -90; else if (!left) rY = 0; else rY = 180;
            rY += ThreeWayRotationOffset;
        }
        else if (count == 2)
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
        else
        {
            prefab = roadStraight;
            if (up || down) rY = 0 + StraightRotationOffset; else rY = 90 + StraightRotationOffset;
        }

        gridManager.PlacePrefab(prefab, pos, Quaternion.Euler(0, rY, 0));
    }

    private void CheckNeighborSocket(Vector2Int neighborPos, Vector2Int requiredDir, ref bool connectionFlag)
    {
        var ns = allSockets.FirstOrDefault(s => s.Position == neighborPos);
        if(ns.OwnerPOI != null && ns.Direction == requiredDir) connectionFlag = true;
    }

    private void RefreshNeighborsVisuals(Vector2Int pos)
    {
        var sock = allSockets.FirstOrDefault(s => s.Position == pos);
        if (sock.OwnerPOI != null) return; 

        if(roadMapVirtual.Contains(pos)) UpdateSingleRoadVisual(pos, false);
        UpdateNeighborSafe(pos + Vector2Int.up);
        UpdateNeighborSafe(pos + Vector2Int.down);
        UpdateNeighborSafe(pos + Vector2Int.left);
        UpdateNeighborSafe(pos + Vector2Int.right);
    }

    private void UpdateNeighborSafe(Vector2Int p)
    {
        if(roadMapVirtual.Contains(p))
        {
            var s = allSockets.FirstOrDefault(x => x.Position == p);
            if (s.OwnerPOI == null) UpdateSingleRoadVisual(p, false);
        }
    }

    private List<Vector2Int> GetRunwayPixels(Vector2Int start, Vector2Int dir, int length)
    {
        List<Vector2Int> res = new List<Vector2Int>();
        Vector2Int curr = start;
        for(int i=0; i<length; i++) { curr += dir; res.Add(curr); }
        return res;
    }

    private void PlaceAllPOIs()
    {
        float radius = gridManager.BuildRadius - SpawnEdgePadding;
        Vector2 center = new Vector2(gridManager.BuildAreaCenter.position.x, gridManager.BuildAreaCenter.position.z);

        for (int i = 0; i < NumberOfPOIs; i++)
        {
            PrefabData prefab = poiPrefabs[Random.Range(0, poiPrefabs.Count)];
            for (int k = 0; k < MaxPlacementAttemptsPerPOI; k++)
            {
                Vector2 rnd = Random.insideUnitCircle * radius;
                int rx = Mathf.RoundToInt((center.x + rnd.x) / 2) * 2;
                int ry = Mathf.RoundToInt((center.y + rnd.y) / 2) * 2;
                Vector2Int pos = new Vector2Int(rx, ry);

                if (placedPOIs.Any(p => Vector2Int.Distance(pos, p.GridPosition) < MinDistanceBetweenPOIs)) continue;
                if (gridManager.IsAreaFree(pos, prefab.Size))
                {
                    gridManager.PlacePrefab(prefab, pos, Quaternion.identity);
                    gridManager.MarkAreaOccupied(pos, prefab.Size);
                    placedPOIs.Add(new PlacedPOI { GridPosition = pos, Data = prefab });
                    break;
                }
            }
        }
    }
    
    private void IdentifyAllSockets()
    {
        foreach(var poi in placedPOIs)
        {
            foreach(var local in poi.Data.ConnectionSockets)
            {
                Vector2Int world = poi.GridPosition + local;
                Vector2 dirVec = (Vector2)local;
                Vector2Int dir = GetCardinalDirection(dirVec);
                allSockets.Add(new WorldSocket { Position = world, Direction = dir, OwnerPOI = poi });
                
                List<Vector2Int> runway = GetRunwayPixels(world, dir, RunwayLength);
                foreach(var p in runway) protectedAccessPoints.Add(p);
                protectedAccessPoints.Add(world);
            }
        }
    }

    private Vector2Int GetCardinalDirection(Vector2 v) 
    { 
        if (Mathf.Abs(v.x) > Mathf.Abs(v.y)) return new Vector2Int(v.x > 0 ? 1 : -1, 0); 
        return new Vector2Int(0, v.y > 0 ? 1 : -1); 
    }

    private void CreateDebugSphere(Vector2Int pos, Color col, string name)
    {
        GameObject obj = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        obj.name = name;
        obj.GetComponent<Renderer>().material.color = col;
        obj.transform.position = new Vector3(pos.x, 15f, pos.y);
        obj.transform.localScale = Vector3.one * 5f;
        debugMarkers.Add(obj);
    }
}