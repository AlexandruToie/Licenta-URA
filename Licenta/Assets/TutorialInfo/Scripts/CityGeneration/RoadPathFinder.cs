using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class RoadPathfinder
{
    private RoadGridManager gridManager;

    private const int COST_EXISTING_ROAD = 1;   
    private const int COST_GRASS = 10;          
    private const int PENALTY_TURN = 50000;        

    public RoadPathfinder(RoadGridManager manager)
    {
        this.gridManager = manager;
    }

    // Am adaugat parametrul 'obstacles' la final
    public List<Vector2Int> FindPath(Vector2Int startPos, Vector2Int targetPos, HashSet<Vector2Int> existingRoads, HashSet<Vector2Int> protectedZones, Vector2Int startDir, HashSet<Vector2Int> obstacles)
    {
        List<Node> openSet = new List<Node>();
        HashSet<Vector2Int> closedSet = new HashSet<Vector2Int>();

        openSet.Add(new Node(startPos, null, 0, 0, startDir));

        while (openSet.Count > 0)
        {
            Node currentNode = openSet
                .OrderBy(n => n.GCost)
                .ThenByDescending(n => n.ArrivalDirection == (n.Parent != null ? n.Parent.ArrivalDirection : startDir))
                .First();

            if (currentNode.Position == targetPos) return RetracePath(currentNode);

            openSet.Remove(currentNode);
            closedSet.Add(currentNode.Position);

            foreach (Vector2Int neighborPos in GetNeighbors(currentNode.Position))
            {
                if (closedSet.Contains(neighborPos)) continue;

                bool isTarget = (neighborPos == targetPos);
                bool isExistingRoad = existingRoads != null && existingRoads.Contains(neighborPos);

                // 1. Grid Limits & Buildings
                if (!gridManager.IsAreaFree(neighborPos, Vector2Int.one) && !isTarget && !isExistingRoad) continue;
                
                // 2. Protected Zones (Sockets)
                if (protectedZones != null && protectedZones.Contains(neighborPos) && !isTarget) continue;

                // 3. OBSTACOLE (BUFFER POI) - Aici e noutatea
                // Daca e in zona tampon si nu e drum existent, e interzis
                if (obstacles != null && obstacles.Contains(neighborPos) && !isTarget && !isExistingRoad) continue;

                Vector2Int moveDir = neighborPos - currentNode.Position;

                // 4. Parallel Road Spacing
                if (!isExistingRoad && !isTarget)
                {
                    if (IsHardBlockedByParallelRoads(neighborPos, moveDir, existingRoads)) continue;
                }

                // Cost Calculation
                int newGCost = currentNode.GCost;
                if (isExistingRoad) newGCost += COST_EXISTING_ROAD; 
                else newGCost += COST_GRASS;

                if (currentNode.ArrivalDirection != Vector2Int.zero && moveDir != currentNode.ArrivalDirection)
                {
                    newGCost += PENALTY_TURN;
                }

                Node neighborNode = openSet.FirstOrDefault(n => n.Position == neighborPos);
                if (neighborNode == null || newGCost < neighborNode.GCost)
                {
                    if (neighborNode == null)
                    {
                        neighborNode = new Node(neighborPos, currentNode, newGCost, 0, moveDir);
                        openSet.Add(neighborNode);
                    }
                    else
                    {
                        neighborNode.GCost = newGCost;
                        neighborNode.Parent = currentNode;
                        neighborNode.ArrivalDirection = moveDir;
                    }
                }
            }
        }
        return null;
    }

    private bool IsHardBlockedByParallelRoads(Vector2Int pos, Vector2Int moveDir, HashSet<Vector2Int> existingRoads)
    {
        if (existingRoads == null) return false;

        if (moveDir.y != 0) // Vertical movement -> Check Left/Right
        {
            if (CheckSide(pos, Vector2Int.left, 1, existingRoads)) return true;
            if (CheckSide(pos, Vector2Int.left, 2, existingRoads)) return true;
            if (CheckSide(pos, Vector2Int.right, 1, existingRoads)) return true;
            if (CheckSide(pos, Vector2Int.right, 2, existingRoads)) return true;
        }
        else if (moveDir.x != 0) // Horizontal movement -> Check Up/Down
        {
            if (CheckSide(pos, Vector2Int.up, 1, existingRoads)) return true;
            if (CheckSide(pos, Vector2Int.up, 2, existingRoads)) return true;
            if (CheckSide(pos, Vector2Int.down, 1, existingRoads)) return true;
            if (CheckSide(pos, Vector2Int.down, 2, existingRoads)) return true;
        }
        return false;
    }

    private bool CheckSide(Vector2Int center, Vector2Int dir, int dist, HashSet<Vector2Int> roads)
    {
        Vector2Int checkPos = center + (dir * dist);
        return roads.Contains(checkPos);
    }

    private List<Vector2Int> RetracePath(Node endNode)
    {
        List<Vector2Int> path = new List<Vector2Int>();
        Node curr = endNode;
        while (curr != null) { path.Add(curr.Position); curr = curr.Parent; }
        path.Reverse();
        return path;
    }

    private List<Vector2Int> GetNeighbors(Vector2Int pos)
    {
        return new List<Vector2Int> { pos + Vector2Int.up, pos + Vector2Int.right, pos + Vector2Int.down, pos + Vector2Int.left };
    }

    private class Node
    {
        public Vector2Int Position;
        public Node Parent;
        public int GCost;
        public int FCost; 
        public Vector2Int ArrivalDirection;
        public Node(Vector2Int pos, Node parent, int g, int h, Vector2Int dir) { Position = pos; Parent = parent; GCost = g; FCost = 0; ArrivalDirection = dir; }
    }
}