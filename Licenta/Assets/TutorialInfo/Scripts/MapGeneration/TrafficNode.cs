using System.Collections.Generic;
using UnityEngine;

public class TrafficNode
{
    public Vector2Int GridPosition; // Where is it on the grid (ex: 2, 3)
    public Vector3 WorldPosition;   // Where is it in the world (ex: 10, 0, 15)
    
    public List<TrafficNode> Neighbors = new List<TrafficNode>();

    public TrafficNode(Vector2Int gridPos, Vector3 worldPos)
    {
        GridPosition = gridPos;
        WorldPosition = worldPos;
    }
}