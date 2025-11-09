using UnityEngine;
using System.Collections.Generic;

public abstract class BaseRoadConnector : MonoBehaviour
{
    // A simple class to store one socket
    [System.Serializable]
    public class ConnectionPoint
    {
        public Vector3 localPosition;
        public Quaternion localRotation;
    }

    [Header("Calculated Sockets")]
    public List<ConnectionPoint> sockets = new List<ConnectionPoint>();

    public ConnectionPoint FindEntrySocket() // For pieces with a single entry
    {
        foreach (var socket in sockets)
        {
            if (Mathf.Approximately(socket.localRotation.eulerAngles.y, 180f))
                return socket;
        }
        // Fallback for pieces like roundabouts that might not have a 180-deg entry
        if (sockets.Count > 0) return sockets[0]; 
        return null;
    }
    public ConnectionPoint GetExitSocket() // For pieces with a single exit
    {
        foreach (var socket in sockets)
        {
            // Return the first socket that is NOT the entry
            if (!Mathf.Approximately(socket.localRotation.eulerAngles.y, 180f))
                return socket;
        }
        return null;
    }

    public List<ConnectionPoint> GetExitSockets() // For pieces with multiple exits (e.g., T-junctions)
    {
        List<ConnectionPoint> exits = new List<ConnectionPoint>();
        foreach (var socket in sockets)
        {
            // Add all sockets that are NOT the entry
            if (!Mathf.Approximately(socket.localRotation.eulerAngles.y, 180f))
                exits.Add(socket);
        }
        return exits;
    }
}