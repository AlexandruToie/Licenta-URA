using System.Collections.Generic;
using UnityEngine; // Added this just in case, though not strictly needed by this file

/// <summary>
/// Defines the possible turn directions at an intersection.
/// </summary>
public enum TurnDirection
{
    Straight,
    Left,
    Right,
    UTurn
}

/// <summary>
/// Represents a valid connection from one RoadLane to another,
/// typically *inside* an intersection.
/// </summary>
[System.Serializable]
public struct LaneConnection
{
    public RoadLane TargetLane;
    public TurnDirection TurnType;
}

/// <summary>
/// Represents a single lane on a RoadEdge.
/// This is the data structure needed for complex, lane-based AI pathfinding.
/// </summary>
[System.Serializable] // Added this so it can be viewed in the Inspector
public class RoadLane
{
    public float SpeedLimit;
    public bool IsOneWay;
    
    // Defines which lanes this one can connect to in the *next* node
    public List<LaneConnection> AllowedConnections;

    public RoadLane(float speedLimit = 30f, bool isOneWay = false)
    {
        SpeedLimit = speedLimit;
        IsOneWay = isOneWay;
        AllowedConnections = new List<LaneConnection>();
    }
}