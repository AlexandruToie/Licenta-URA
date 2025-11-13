using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Represents an "Edge" in the road graph, i.e., a road segment
/// connecting two RoadNodes.
/// </summary>
[System.Serializable]
public class RoadEdge
{
    public int ID;

    [System.NonSerialized]
    public RoadNode StartNode;
    
    [System.NonSerialized]
    public RoadNode EndNode;
    public float Length;

    // Later, you'll add lane data here for AI pathfinding
    // public List<RoadLane> Lanes; 

    public RoadEdge(int id, RoadNode start, RoadNode end)
    {
        this.ID = id;
        this.StartNode = start;
        this.EndNode = end;
        this.Length = Vector3.Distance(start.Position, end.Position);
    }
}