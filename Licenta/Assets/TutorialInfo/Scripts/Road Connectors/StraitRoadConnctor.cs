using UnityEngine;
using System.Collections.Generic;

public class StraitRoadConnector : BaseRoadConnector
{
    [Header("Road Properties")]
    [Tooltip("The length of this specific road piece. You said this is 1.")]
    public float length = 1f;

    [ContextMenu("Calculate Sockets for THIS Strait-Road")]
    private void CalculateSockets()
    {
        // Clear any old, existing data
        sockets.Clear();

        sockets.Add(new ConnectionPoint  // Socket 0: The "Entry" point
        {
            localPosition = Vector3.zero,
            localRotation = Quaternion.Euler(0, 180, 0) // Faces -Z
        });

        sockets.Add(new ConnectionPoint // Socket 1: The "Exit" point
        {
            localPosition = new Vector3(0, 0, length),
            localRotation = Quaternion.Euler(0, 0, 0)
        });
        
        Debug.Log("Calculated 2 sockets for Strait-Road.");
    }
}