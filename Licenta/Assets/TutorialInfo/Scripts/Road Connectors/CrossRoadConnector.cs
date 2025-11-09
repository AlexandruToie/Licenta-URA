using UnityEngine;
using System.Collections.Generic;

public class CrossRoadConnector : BaseRoadConnector
{
    [Header("Road Properties")]
    [Tooltip("The total width of this piece (from West to East).")]
    public float width = 1f;
    [Tooltip("The total length of this piece (from South to North).")]
    public float length = 1f;
    
    [ContextMenu("Calculate Sockets for THIS Cross-Road")]
    private void CalculateSockets()
    {
        // Clear any old, existing data
        sockets.Clear();
        
        float halfWidth = width / 2.0f; // This will be 0.5
        
        // Socket 0: South (Entrance)
        // This IS the pivot point (0,0,0) as you said.
        // It faces "backwards" into the connecting road.
        sockets.Add(new ConnectionPoint
        {
            localPosition = Vector3.zero, // (0, 0, 0)
            localRotation = Quaternion.Euler(0, 180, 0) // Faces -Z
        });

        // Socket 1: North (Exit)
        // This is at the end of the prefab, (0, 0, length)
        sockets.Add(new ConnectionPoint
        {
            localPosition = new Vector3(0, 0, length),
            localRotation = Quaternion.Euler(0, 0, 0) // Faces +Z
        });

        // Socket 2: West (Exit)
        // This is halfway up the prefab (length / 2)
        // and on the far left edge (-halfWidth)
        sockets.Add(new ConnectionPoint
        {
            localPosition = new Vector3(-halfWidth, 0, length / 2.0f),
            localRotation = Quaternion.Euler(0, -90, 0) // Faces -X
        });

        // Socket 3: East (Exit)
        // This is halfway up the prefab (length / 2)
        // and on the far right edge (+halfWidth)
        sockets.Add(new ConnectionPoint
        {
            localPosition = new Vector3(halfWidth, 0, length / 2.0f),
            localRotation = Quaternion.Euler(0, 90, 0) // Faces +X
        });
        
        Debug.Log("Calculated 4 sockets for Cross-Road (South Pivot).");
    }
}