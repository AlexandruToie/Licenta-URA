using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class RoadPathfinder
{
    private RoadGridManager gridManager;

    private const int COST_CROSSING = 50;
    private const int COST_GRASS = 10;          
    private const int PENALTY_TURN = 50000;      

    public RoadPathfinder(RoadGridManager manager) // Constructor
    {
        this.gridManager = manager;
    }

    // Main pathfinding method
    public List<Vector2Int> FindPath(Vector2Int startPos, Vector2Int targetPos, HashSet<Vector2Int> existingRoads, HashSet<Vector2Int> protectedZones, Vector2Int startDir, 
        HashSet<Vector2Int> obstacles, List<Vector2Int> poiPositions, HashSet<Vector2Int> existingIntersections, float minIntersectionDist, int maxTurns)
    {
        List<Node> openSet = new List<Node>(); // Nodes to be evaluated
        HashSet<Vector2Int> closedSet = new HashSet<Vector2Int>(); // Evaluated nodes

        openSet.Add(new Node(startPos, null, 0, 0, startDir, 0)); // Add starting node

        while (openSet.Count > 0) // Main loop
        {
            // Select node with lowest GCost, tie-breaker on ArrivalDirection
            Node currentNode = openSet.OrderBy(n => n.GCost).ThenByDescending(n => n.ArrivalDirection == (n.Parent != null ? n.Parent.ArrivalDirection : startDir)).First();

            if (currentNode.Position == targetPos) return RetracePath(currentNode); // Path found

            openSet.Remove(currentNode); // Move current node to closed set
            closedSet.Add(currentNode.Position); // Mark as evaluated

            foreach (Vector2Int neighborPos in GetNeighbors(currentNode.Position)) // Explore neighbors
            {
                if (closedSet.Contains(neighborPos)) continue; // Already evaluated

                bool isTarget = (neighborPos == targetPos);
                bool isNeighborRoad = existingRoads != null && existingRoads.Contains(neighborPos);
                
                // Check if current node is on a road
                bool isCurrentRoad = existingRoads != null && existingRoads.Contains(currentNode.Position);

                //Rule 3.0: Accesibilitate
                if (!gridManager.IsAreaFree(neighborPos, Vector2Int.one) && !isTarget && !isNeighborRoad) continue;
                if (protectedZones != null && protectedZones.Contains(neighborPos) && !isTarget) continue;
                if (obstacles != null && obstacles.Contains(neighborPos) && !isTarget && !isNeighborRoad) continue;

                Vector2Int moveDir = neighborPos - currentNode.Position;
                //Rule 3.1: No Overlapping Roads
                if (isCurrentRoad && isNeighborRoad && !isTarget) 
                {
                    continue; 
                }

                //Rule 3.2: Paralelism
                if (!isNeighborRoad && !isTarget)
                {
                    if (IsHardBlockedByParallelRoads(neighborPos, moveDir, existingRoads)) continue;
                }

                //Rule 3.4: Intersections Spacing
                if (!isCurrentRoad && isNeighborRoad)
                {
                    // New intersection being created
                    if (!existingIntersections.Contains(neighborPos))
                    {
                        if (IsTooCloseToIntersectionOrthogonal(neighborPos, existingIntersections, minIntersectionDist)) continue;
                        if (IsTooCloseToIntersectionArea(neighborPos, existingIntersections, 1)) continue;
                        if (IsTooCloseToAnyPOI(neighborPos, poiPositions, 4f)) continue;
                    }
                }

                int stepCost = 0; // Cost to move to neighbor
                
                if (isNeighborRoad) stepCost = COST_CROSSING; // Crossing an existing road
                else stepCost = COST_GRASS; // Normal grass tile

                int newGCost = currentNode.GCost + stepCost;

                // Rule 3.3: Penalize Turns
                bool isTurn = (currentNode.ArrivalDirection != Vector2Int.zero && moveDir != currentNode.ArrivalDirection);
                if (isTurn) newGCost += PENALTY_TURN;

                int newTurnCount = currentNode.TurnCount + (isTurn ? 1 : 0); // Update turn count
                if (newTurnCount > maxTurns) continue;

                Node neighborNode = openSet.FirstOrDefault(n => n.Position == neighborPos); // Check if neighbor is already in open set
                if (neighborNode == null || newGCost < neighborNode.GCost) // Better path found
                {
                    if (neighborNode == null) // Not in open set
                    {
                        neighborNode = new Node(neighborPos, currentNode, newGCost, 0, moveDir, newTurnCount);
                        openSet.Add(neighborNode);
                    }
                    else
                    {
                        neighborNode.GCost = newGCost;
                        neighborNode.Parent = currentNode;
                        neighborNode.ArrivalDirection = moveDir;
                        neighborNode.TurnCount = newTurnCount;
                    }
                }
            }
        }
        return null;
    }

    private bool IsTooCloseToIntersectionArea(Vector2Int pos, HashSet<Vector2Int> intersections, int range) // Checks area around pos for intersections
    {
        if (intersections == null) return false; // No intersections to check
        for (int x = -range; x <= range; x++) // Check square area
        {
            for (int y = -range; y <= range; y++)
            {
                if (x == 0 && y == 0) continue;
                Vector2Int check = pos + new Vector2Int(x, y);
                if (intersections.Contains(check)) return true;
            }
        }
        return false;
    }

    private bool IsTooCloseToIntersectionOrthogonal(Vector2Int pos, HashSet<Vector2Int> intersections, float limit) // Checks orthogonal distance to intersections
    {
        if (intersections == null) return false;
        foreach (var inter in intersections)
        {
            int dx = Mathf.Abs(pos.x - inter.x);
            int dy = Mathf.Abs(pos.y - inter.y);
            bool isOrthogonal = (dx == 0) || (dy == 0);
            if (isOrthogonal && (dx + dy < limit)) return true;
        }
        return false;
    }

    private bool IsTooCloseToAnyPOI(Vector2Int pos, List<Vector2Int> pois, float limit) // Checks distance to POIs
    {
        if (pois == null) return false;
        foreach(var p in pois)
        {
            if (Mathf.Abs(pos.x - p.x) + Mathf.Abs(pos.y - p.y) < limit) return true; // Manhattan distance
        }
        return false;
    }

    private bool IsHardBlockedByParallelRoads(Vector2Int pos, Vector2Int moveDir, HashSet<Vector2Int> existingRoads) // Checks for parallel roads blocking the path
    {
        if (existingRoads == null) return false; // No existing roads to check
        if (moveDir.y != 0) // Moving vertically
        {
            if (CheckSide(pos, Vector2Int.left, 1, existingRoads)) return true;
            if (CheckSide(pos, Vector2Int.left, 2, existingRoads)) return true;
            if (CheckSide(pos, Vector2Int.right, 1, existingRoads)) return true;
            if (CheckSide(pos, Vector2Int.right, 2, existingRoads)) return true;
        }
        else if (moveDir.x != 0) // Moving horizontally
        {
            if (CheckSide(pos, Vector2Int.up, 1, existingRoads)) return true;
            if (CheckSide(pos, Vector2Int.up, 2, existingRoads)) return true;
            if (CheckSide(pos, Vector2Int.down, 1, existingRoads)) return true;
            if (CheckSide(pos, Vector2Int.down, 2, existingRoads)) return true;
        }
        return false;
    }

    private bool CheckSide(Vector2Int center, Vector2Int dir, int dist, HashSet<Vector2Int> roads) // Checks if there's a road at a certain offset
    {
        return roads.Contains(center + (dir * dist));
    }

    private List<Vector2Int> RetracePath(Node endNode) // Retraces path from end node to start
    {
        List<Vector2Int> path = new List<Vector2Int>();
        Node curr = endNode;
        while (curr != null) { path.Add(curr.Position); curr = curr.Parent; }
        path.Reverse();
        return path;
    }

    private List<Vector2Int> GetNeighbors(Vector2Int pos) // Returns cardinal neighbors
    {
        return new List<Vector2Int> { pos + Vector2Int.up, pos + Vector2Int.right, pos + Vector2Int.down, pos + Vector2Int.left };
    }

    private class Node // Represents a node in the pathfinding graph
    {
        public Vector2Int Position;
        public Node Parent;
        public int GCost;
        public int FCost; 
        public Vector2Int ArrivalDirection;
        public int TurnCount;

        public Node(Vector2Int pos, Node parent, int g, int h, Vector2Int dir, int turns) // Constructor
        { 
            Position = pos; 
            Parent = parent; 
            GCost = g; 
            FCost = 0; 
            ArrivalDirection = dir; 
            TurnCount = turns;
        }
    }
}