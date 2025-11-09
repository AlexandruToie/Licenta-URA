using UnityEngine;
using System.Collections.Generic;

public class RoundaboutConnector : BaseRoadConnector
{

    [Header("Socket Properties")]
    [Tooltip("The total length from the pivot (entrance) to the North exit.")]
    public float totalLength = 3f;
    [Tooltip("The distance from the center-line to the East/West exits.")]
    public float halfWidth = 1.5f;
    
    [ContextMenu("Calculate Sockets for THIS Roundabout")]
    private void CalculateSockets()
    {
        // Clear any old, existing data
        sockets.Clear();

        // The middle point Z-position, as you described
        float midPointZ = totalLength / 2.0f; // This will be 1.5

        // Socket 0: Entrance (South)
        // This IS the pivot point (0,0,0) as you said.
        // It faces "backwards" into the connecting road.
        sockets.Add(new ConnectionPoint
        {
            localPosition = Vector3.zero, // (0, 0, 0)
            localRotation = Quaternion.Euler(0, 180, 0) // Faces -Z
        });

        // Socket 1: Exit (North)
        // At (0, 0, totalLength)
        sockets.Add(new ConnectionPoint
        {
            localPosition = new Vector3(0, 0, totalLength),
            localRotation = Quaternion.Euler(0, 0, 0) // Faces +Z
        });

        // Socket 2: Exit (West)
        // At (-halfWidth, 0, midPointZ)
        sockets.Add(new ConnectionPoint
        {
            localPosition = new Vector3(-halfWidth, 0, midPointZ),
            localRotation = Quaternion.Euler(0, -90, 0) // Faces -X
        });

        // Socket 3: Exit (East)
        // At (+halfWidth, 0, midPointZ)
        sockets.Add(new ConnectionPoint
        {
            localPosition = new Vector3(halfWidth, 0, midPointZ),
            localRotation = Quaternion.Euler(0, 90, 0) // Faces +X
        });
        
        Debug.Log("Calculated 4 sockets for Roundabout.");
    }
}