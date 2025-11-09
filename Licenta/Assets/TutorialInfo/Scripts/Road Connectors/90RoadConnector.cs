using UnityEngine;
using System.Collections.Generic;

public class CornerRoad90ShortConnector : BaseRoadConnector
{
    [Header("Socket Properties")]
    [Tooltip("The exact local position of the exit socket, as measured from the pivot (entrance).")]
    // We pre-fill this with your numbers: X=0.5, Z=0.5
    public Vector3 exitLocalPosition = new Vector3(0.5f, 0, 0.5f); 
    
    [Tooltip("The direction the exit socket faces (in degrees). 90 = Right, -90 = Left.")]
    public float exitAngle = 90f; // It's a right turn

    [ContextMenu("Calculate Sockets for THIS Short Corner")]
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

        // Socket 1: Exit (East)
        // Using the *exact* values from the Inspector.
        sockets.Add(new ConnectionPoint
        {
            localPosition = exitLocalPosition,
            localRotation = Quaternion.Euler(0, exitAngle, 0)
        });
        
        Debug.Log("Calculated 2 sockets for 90-Road (Short).");
    }
}