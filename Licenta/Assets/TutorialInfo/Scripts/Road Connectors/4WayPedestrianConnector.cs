using UnityEngine;
using System.Collections.Generic;

public class PedestrianCrossroadConnector : BaseRoadConnector
{
    [Header("Road Properties")]
    [Tooltip("The total width of this piece (from West to East).")]
    public float width = 1f; // 0.5 left + 0.5 right = 1
    [Tooltip("The total length of this piece (from South to North).")]
    public float length = 1f; // From pivot to front exit

    [ContextMenu("Calculate Sockets for THIS Pedestrian Crossroad")]
    private void CalculateSockets()
    {
        // Clear any old, existing data
        sockets.Clear();

        
        float halfWidth = width / 2.0f; // This will be 0.5
        float halfLength = length / 2.0f; // This will be 0.5
        
        // Socket 0: Entrance (South)
        // This IS the pivot point (0,0,0) as you said.
        sockets.Add(new ConnectionPoint
        {
            localPosition = Vector3.zero, // (0, 0, 0)
            localRotation = Quaternion.Euler(0, 180, 0) // Faces -Z
        });

        // Socket 1: Exit (North)
        // This is at the end of the prefab, (0, 0, length)
        sockets.Add(new ConnectionPoint
        {
            localPosition = new Vector3(0, 0, length),
            localRotation = Quaternion.Euler(0, 0, 0) // Faces +Z
        });

        // Socket 2: Exit (West)
        // This is halfway up the prefab (halfLength)
        // and on the far left edge (-halfWidth)
        sockets.Add(new ConnectionPoint
        {
            localPosition = new Vector3(-halfWidth, 0, halfLength),
            localRotation = Quaternion.Euler(0, -90, 0) // Faces -X
        });

        // Socket 3: Exit (East)
        // This is halfway up the prefab (halfLength)
        // and on the far right edge (+halfWidth)
        sockets.Add(new ConnectionPoint
        {
            localPosition = new Vector3(halfWidth, 0, halfLength),
            localRotation = Quaternion.Euler(0, 90, 0) // Faces +X
        });
        
        Debug.Log("Calculated 4 sockets for 4Way-Pedestrian-Crossroad.");
    }
}