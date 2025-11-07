using System.Collections.Generic;
using UnityEngine;

public class CityGenerator : MonoBehaviour 
{
    [Header("Growth Parameters")]
    public int minStepsToIntersection = 5;
    public int maxStepsToIntersection = 10;
    public int maxGrowthIterations = 5;

    [Header("Object Prefabs")]
    public GameObject roadPrefab;
    public GameObject intersectionPrefab;
    public GameObject buildingPrefab;

    [Header("Road & Intersection Meshes")]
    public float roadPrefabTileLength = 1.0f;
    public float intersectionRadius = 2.5f; 
    public float intersectionClearance = 10f;

    // --- DELETED: The "Road Physics" fields are gone ---

    [Header("Building Placement")]
    public float buildingOffset = 2f;
    public float buildingSpacing = 5f;
    [Range(0, 90)]
    public float maxBuildingSlope = 30f; 
    [Tooltip("Layer mask for checking if a building spot is occupied.")]
    public LayerMask buildingLayer; 
    public float buildingCheckRadius = 3f;
    
    [Header("Terrain Constraints")]
    public bool constrainToFlatZone = true; 

    // --- Private ---
    private Terrain terrain;
    private WindingPathTerrainGenerator terrainGenerator;
    
    private Queue<RoadWalker> walkerQueue = new Queue<RoadWalker>();
    private List<Vector3> placedIntersectionPoints = new List<Vector3>();
    private List<RoadSegment> allRoadSegments = new List<RoadSegment>();

    #region Helper Classes
    private class RoadWalker
    {
        public Vector3 startPosition; 
        public Quaternion rotation;   
        public int stepsToGrow;    
        public int iterationsLeft; 

        public RoadWalker(Vector3 pos, Quaternion rot, int steps, int iter)
        {
            this.startPosition = pos;
            this.rotation = rot;
            this.stepsToGrow = steps;
            this.iterationsLeft = iter;
        }
    }
    
    private class RoadSegment
    {
        public Vector3 start;
        public Vector3 end;
        // We no longer store the "ideal" direction,
        // we calculate the *real* one.
        
        public RoadSegment(Vector3 start, Vector3 end) 
        { 
            this.start = start; 
            this.end = end;
        }
    }
    #endregion


    public void Initialize(Terrain terrain, WindingPathTerrainGenerator generator)
    {
        Debug.Log("CityGenerator received 'Initialize' call from TerrainGenerator.");

        this.terrain = terrain;
        this.terrainGenerator = generator;

        if (this.terrain == null || this.terrainGenerator == null)
        {
            Debug.LogError("CityGenerator is missing Terrain or TerrainGenerator reference!");
            return;
        }
        
        if (roadPrefabTileLength <= 0.01f)
        {
            Debug.LogError("'Road Prefab Tile Length' is 0. Please set it to the length of your road mesh in the Inspector.");
            return;
        }
        
        Vector3 terrainPos = terrain.transform.position;
        float cityCenterX = terrainGenerator.centerX;
        float cityCenterZ = terrainGenerator.centerY;
        Vector3 worldSpaceCenter = terrainPos + new Vector3(cityCenterX, 0, cityCenterZ);
        worldSpaceCenter.y = terrain.SampleHeight(worldSpaceCenter);
        this.transform.position = worldSpaceCenter;
        
        Debug.Log($"CityGenerator starting at: {this.transform.position}");

        GrowCity();
    }


    /// <summary>
    /// This is the "walker" logic, now with math-based path-checking.
    /// </summary>
    private void GrowCity()
    {
        ClearCity();

        Vector3 startPos = this.transform.position;
        placedIntersectionPoints.Add(startPos);
        
        int startSteps;
        int maxIter = maxGrowthIterations;

        startSteps = Random.Range(minStepsToIntersection, maxStepsToIntersection + 1);
        walkerQueue.Enqueue(new RoadWalker(startPos, Quaternion.Euler(0, 0, 0), startSteps, maxIter));
        
        startSteps = Random.Range(minStepsToIntersection, maxStepsToIntersection + 1);
        walkerQueue.Enqueue(new RoadWalker(startPos, Quaternion.Euler(0, 90, 0), startSteps, maxIter));
        
        startSteps = Random.Range(minStepsToIntersection, maxStepsToIntersection + 1);
        walkerQueue.Enqueue(new RoadWalker(startPos, Quaternion.Euler(0, 180, 0), startSteps, maxIter));
        
        startSteps = Random.Range(minStepsToIntersection, maxStepsToIntersection + 1);
        walkerQueue.Enqueue(new RoadWalker(startPos, Quaternion.Euler(0, 270, 0), startSteps, maxIter));

        // --- Main Growth Loop ---
        while(walkerQueue.Count > 0)
        {
            RoadWalker walker = walkerQueue.Dequeue();
            
            if (walker.iterationsLeft <= 0) continue;

            // 1. Calculate New Position
            float roadLength = walker.stepsToGrow * roadPrefabTileLength;
            Vector3 roadDirection = walker.rotation * Vector3.forward;
            Vector3 endPos = walker.startPosition + roadDirection * roadLength;

            if (terrain != null) endPos.y = terrain.SampleHeight(endPos);

            // 2. Check if New Position is Valid
            if (constrainToFlatZone && !IsOnFlatZone(endPos))
            {
                continue; // Outside allowed area
            }
            
            if (IsTooCloseToOtherIntersection(endPos, walker.startPosition))
            {
                continue; // Overlaps another intersection
            }

            // --- THIS IS THE FIX ---
            // 3. Check if the PATH is valid (doesn't cross another road)
            if (DoesPathMathematicallyCross(walker.startPosition, endPos))
            {
                continue; // Path is blocked! Kill this walker.
            }
            // -----------------------

            // --- 4. If Valid: Place Intersection & Spawn New Walkers ---
            placedIntersectionPoints.Add(endPos);
            allRoadSegments.Add(new RoadSegment(walker.startPosition, endPos));

            int newIterations = walker.iterationsLeft - 1;
            int nextSteps;
            
            // Walker 1: North (Straight)
            nextSteps = Random.Range(minStepsToIntersection, maxStepsToIntersection + 1);
            Quaternion forwardRot = walker.rotation;
            walkerQueue.Enqueue(new RoadWalker(endPos, forwardRot, nextSteps, newIterations));

            // Walker 2: West (Left)
            nextSteps = Random.Range(minStepsToIntersection, maxStepsToIntersection + 1);
            Quaternion leftRot = walker.rotation * Quaternion.Euler(0, -90, 0);
            walkerQueue.Enqueue(new RoadWalker(endPos, leftRot, nextSteps, newIterations));

            // Walker 3: East (Right)
            nextSteps = Random.Range(minStepsToIntersection, maxStepsToIntersection + 1);
            Quaternion rightRot = walker.rotation * Quaternion.Euler(0, 90, 0);
            walkerQueue.Enqueue(new RoadWalker(endPos, rightRot, nextSteps, newIterations));
        }

        InstantiateCity();
    }
    
    
    private bool IsTooCloseToOtherIntersection(Vector3 newPos, Vector3 startPos)
    {
        foreach(Vector3 existingPos in placedIntersectionPoints)
        {
            // Don't check against our *own* starting point
            if (existingPos == startPos) continue;

            if ((newPos - existingPos).sqrMagnitude < (intersectionClearance * intersectionClearance))
            {
                return true; 
            }
        }
        return false; 
    }


    /// <summary>
    /// This function takes the generated data and spawns the prefabs.
    /// </summary>
    private void InstantiateCity()
    {
        GameObject roadParent = new GameObject("Roads");
        GameObject buildingParent = new GameObject("Buildings");
        GameObject intersectionParent = new GameObject("Intersections");
        
        roadParent.transform.SetParent(this.transform);
        buildingParent.transform.SetParent(this.transform);
        intersectionParent.transform.SetParent(this.transform);

        // --- 1. Instantiate Intersections ---
        if (intersectionPrefab != null)
        {
            foreach (Vector3 pos in placedIntersectionPoints)
            {
                Instantiate(intersectionPrefab, pos, Quaternion.identity, intersectionParent.transform);
            }
        }

        // --- 2. Instantiate Roads (Fixed) ---
        if (roadPrefab != null)
        {
            foreach (RoadSegment segment in allRoadSegments)
            {
                Vector3 groundedDirection = (segment.end - segment.start).normalized;
                Quaternion rotation = Quaternion.LookRotation(groundedDirection);

                Vector3 roadStartPos = segment.start + (groundedDirection * intersectionRadius);
                Vector3 roadEndPos = segment.end - (groundedDirection * intersectionRadius);
                float segmentLength = Vector3.Distance(roadStartPos, roadEndPos);

                if (segmentLength < roadPrefabTileLength) continue; 
            
                float distanceCovered = 0f;
                while (distanceCovered < segmentLength)
                {
                    float tileCenter = distanceCovered + (roadPrefabTileLength / 2.0f);
                    if (tileCenter > segmentLength) break;
                    
                    float t = tileCenter / segmentLength;
                    Vector3 tilePos = Vector3.Lerp(roadStartPos, roadEndPos, t);
                    
                    Instantiate(roadPrefab, tilePos, rotation, roadParent.transform);
                    
                    distanceCovered += roadPrefabTileLength;
                }
            }
        }
        
        // --- 3. Instantiate Buildings (FIXED ROTATION) ---
        if (buildingPrefab != null)
        {
            foreach (RoadSegment segment in allRoadSegments)
            {
                float roadLength = Vector3.Distance(segment.start, segment.end);
                
                // --- THIS IS THE FIX ---
                // Get the *actual* direction of the grounded road segment
                Vector3 roadDirection = (segment.end - segment.start).normalized;
                Vector3 rightDir = Vector3.Cross(roadDirection, Vector3.up).normalized;
                // -----------------------

                int buildingsPerSide = Mathf.FloorToInt(roadLength / buildingSpacing);
                
                for (int i = 0; i < buildingsPerSide; i++)
                {
                    float t = (i * buildingSpacing + (buildingSpacing / 2)) / roadLength;
                    Vector3 positionOnRoad = Vector3.Lerp(segment.start, segment.end, t);

                    // --- Right Side ---
                    Vector3 buildingPosRight = positionOnRoad + (rightDir * (buildingOffset + buildingCheckRadius)); 
                    if (terrain != null) buildingPosRight.y = terrain.SampleHeight(buildingPosRight);
                    
                    if (CheckSlope(buildingPosRight, maxBuildingSlope) && !IsSpotOccupied(buildingPosRight))
                    {
                        Quaternion buildingRotRight = Quaternion.LookRotation(-rightDir); // Face the road
                        Instantiate(buildingPrefab, buildingPosRight, buildingRotRight, buildingParent.transform);
                    }

                    // --- Left Side ---
                    Vector3 buildingPosLeft = positionOnRoad - (rightDir * (buildingOffset + buildingCheckRadius));
                    if (terrain != null) buildingPosLeft.y = terrain.SampleHeight(buildingPosLeft);

                    if (CheckSlope(buildingPosLeft, maxBuildingSlope) && !IsSpotOccupied(buildingPosLeft))
                    {
                        Quaternion buildingRotLeft = Quaternion.LookRotation(rightDir); // Face the road
                        Instantiate(buildingPrefab, buildingPosLeft, buildingRotLeft, buildingParent.transform);
                    }
                }
            }
        }
    }

    
    // ---
    // --- HELPER METHODS ---
    // ---

    #region Road Intersection Math (The Fix)

    /// <summary>
    /// Checks our new proposed road against all roads we've already planned.
    /// </summary>
    private bool DoesPathMathematicallyCross(Vector3 newStart, Vector3 newEnd)
    {
        // We only care about 2D (X, Z) coordinates for crossing
        Vector2 p1 = new Vector2(newStart.x, newStart.z);
        Vector2 p2 = new Vector2(newEnd.x, newEnd.z);

        foreach (RoadSegment segment in allRoadSegments)
        {
            Vector2 p3 = new Vector2(segment.start.x, segment.start.z);
            Vector2 p4 = new Vector2(segment.end.x, segment.end.z);

            if (LineSegmentsIntersect(p1, p2, p3, p4))
            {
                // Our new road crosses an existing one. This is invalid.
                return true; 
            }
        }
        
        return false; // Path is clear
    }

    /// <summary>
    /// Standard 2D math to find orientation of ordered triplet (p, q, r).
    /// </summary>
    /// <returns>0 = Collinear, 1 = Clockwise, 2 = Counterclockwise</returns>
    private int GetOrientation(Vector2 p, Vector2 q, Vector2 r)
    {
        float val = (q.y - p.y) * (r.x - q.x) - (q.x - p.x) * (r.y - q.y);
        if (Mathf.Abs(val) < 0.0001f) return 0; // Collinear
        return (val > 0) ? 1 : 2; // Clockwise or Counterclockwise
    }

    /// <summary>
    /// Given that p, q, and r are collinear, check if point q lies on segment 'pr'
    /// </summary>
    private bool OnSegment(Vector2 p, Vector2 q, Vector2 r)
    {
        if (q.x <= Mathf.Max(p.x, r.x) && q.x >= Mathf.Min(p.x, r.x) &&
            q.y <= Mathf.Max(p.y, r.y) && q.y >= Mathf.Min(p.y, r.y))
        {
            return true;
        }
        return false;
    }

    /// <summary>
    /// The main function that returns true if line segment 'p1q1' and 'p2q2' intersect.
    /// </summary>
    private bool LineSegmentsIntersect(Vector2 p1, Vector2 q1, Vector2 p2, Vector2 q2)
    {
        // Find the four orientations needed
        int o1 = GetOrientation(p1, q1, p2);
        int o2 = GetOrientation(p1, q1, q2);
        int o3 = GetOrientation(p2, q2, p1);
        int o4 = GetOrientation(p2, q2, q1);

        // --- General Case ---
        // If (p1, q1, p2) and (p1, q1, q2) have different orientations,
        // and (p2, q2, p1) and (p2, q2, q1) have different orientations.
        if (o1 != o2 && o3 != o4)
        {
            // We need to exclude the case where they just "touch" at an endpoint,
            // as this is a valid intersection, not a crossing.
            if (o1 == 0 || o2 == 0 || o3 == 0 || o4 == 0)
            {
                return false; // They are "touching" at an endpoint
            }
            return true; // They are definitely crossing in the middle
        }

        // --- Special Cases ---
        // These are for when the lines are collinear
        if (o1 == 0 && OnSegment(p1, p2, q1)) return true;
        if (o2 == 0 && OnSegment(p1, q2, q1)) return true;
        if (o3 == 0 && OnSegment(p2, p1, q2)) return true;
        if (o4 == 0 && OnSegment(p2, q1, q2)) return true;

        return false; // Doesn't fall in any of the above cases
    }


    #endregion

    #region Other Helper Methods
    private bool IsSpotOccupied(Vector3 pos)
    {
        if (Physics.CheckSphere(pos, buildingCheckRadius, buildingLayer))
        {
            return true; 
        }
        return false; 
    }
    
    private bool IsOnFlatZone(Vector3 worldPos)
    {
        if (terrainGenerator == null || terrain == null) return true; 

        Vector3 terrainLocalPos = worldPos - terrain.transform.position;
        int terrainX = (int)terrainLocalPos.x;
        int terrainY = (int)terrainLocalPos.z;

        float pathBlend = terrainGenerator.GetPublicPathBlend(terrainX, terrainY);
        if (pathBlend > 0.1f) return true;

        float dist = Vector2.Distance(
            new Vector2(terrainX, terrainY),
            new Vector2(terrainGenerator.centerX, terrainGenerator.centerY)
        );
        
        if (dist <= terrainGenerator.centerRadius * terrainGenerator.centerBlendFactor) return true; 

        return false;
    }

    private bool CheckSlope(Vector3 worldPos, float maxSlope)
    {
        if (terrain == null) return true; 

        TerrainData td = terrain.terrainData;
        Vector3 terrainLocalPos = worldPos - terrain.transform.position;

        float normX = terrainLocalPos.x / td.size.x;
        float normZ = terrainLocalPos.z / td.size.z;

        if (normX < 0 || normX > 1 || normZ < 0 || normZ > 1) return false;

        float slope = td.GetSteepness(normX, normZ); 
        return slope <= maxSlope;
    }

    [ContextMenu("Clear City")]
    private void ClearCity()
    {
        walkerQueue.Clear();
        placedIntersectionPoints.Clear();
        allRoadSegments.Clear();
        
        int childCount = transform.childCount;
        for (int i = childCount - 1; i >= 0; i--)
        {
            if (Application.isEditor && !Application.isPlaying)
            {
                DestroyImmediate(transform.GetChild(i).gameObject);
            }
            else
            {
                Destroy(transform.GetChild(i).gameObject);
            }
        }
    }
    #endregion
}