using System.Collections.Generic;
using UnityEngine;



/// <summary>
/// Represents a "Node" in the road graph, i.e., an intersection or dead end.
/// Based on the data structure described in Part II of the design doc.
/// </summary>
[System.Serializable]
public class RoadNode
{
    public int ID;
    public Vector3 Position;
    
    [System.NonSerialized] 
    public List<RoadEdge> ConnectedEdges = new List<RoadEdge>();

    // We can add more metadata here later, like IntersectionType
    public RoadNode(int id, Vector3 position)
    {
        this.ID = id;
        this.Position = position;
    }
}