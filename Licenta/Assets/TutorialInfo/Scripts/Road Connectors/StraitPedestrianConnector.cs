using UnityEngine;
using System.Collections.Generic;

public class StraitPedestrianConnector : BaseRoadConnector
{
    [Header("Road Properties")]
    [Tooltip("The total length of this piece (from South to North).")]
    public float length = 1f; // From pivot to front exit

    [ContextMenu("Calculate Sockets for THIS Strait Pedestrian Road")]
    private void CalculateSockets()
    {
        // Clear any old, existing data
        sockets.Clear();
        
        // Socket 0: Entrance (South)
        // This IS the pivot point (0,0,0) as you said.
        sockets.Add(new ConnectionPoint
        {
            localPosition = Vector3.zero, // (0, 0, 0)
            localRotation = Quaternion.Euler(0, 180, 0) // Faces -Z
        });

        // Socket 1: Exit (North / Front)
        // This is at the end of the prefab, (0, 0, length)
        sockets.Add(new ConnectionPoint
        {
            localPosition = new Vector3(0, 0, length),
            localRotation = Quaternion.Euler(0, 0, 0) // Faces +Z
        });
        
        Debug.Log("Calculated 2 sockets for Strait-Road-Pedestrian.");
    }
}