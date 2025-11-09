using UnityEngine;
using System.Collections.Generic;


public class ThreeWayLongConnector : BaseRoadConnector
{
    [Header("Socket Properties")]
    [Tooltip("The exact local position of the STRAIGHT exit socket.")]
    public Vector3 straightExitPosition = new Vector3(0f, 0, 2f);
    
    [Tooltip("The exact local position of the CORNER exit socket.")]
    public Vector3 cornerExitPosition = new Vector3(1f, 0, 1.5f); 

    [ContextMenu("Calculate Sockets for THIS 3-Way Road")]
    private void CalculateSockets()
    {
        // Clear any old, existing data
        sockets.Clear();

        // Socket 0: Entrance (South)
        // This IS the pivot point (0,0,0) as you said.
        // It faces "backwards" into the connecting road.
        sockets.Add(new ConnectionPoint
        {
            localPosition = Vector3.zero, // (0, 0, 0)
            localRotation = Quaternion.Euler(0, 180, 0) // Faces -Z
        });

        // Socket 1: Exit (Straight / North)
        // Using the *exact* values from the Inspector.
        sockets.Add(new ConnectionPoint
        {
            localPosition = straightExitPosition,
            localRotation = Quaternion.Euler(0, 0, 0) // Faces +Z
        });
        
        // Socket 2: Exit (Corner / East)
        // Using the *exact* values from the Inspector.
        sockets.Add(new ConnectionPoint
        {
            localPosition = cornerExitPosition,
            localRotation = Quaternion.Euler(0, 90, 0) // Faces +X
        });
        
        Debug.Log("Calculated 3 sockets for 3-Way-Long-Road.");
    }
}