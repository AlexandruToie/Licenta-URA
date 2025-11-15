using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System;

[CustomEditor(typeof(PrefabData))]
public class PrefabDataEditor : Editor
{
    private const string SOCKET_TAG = "Socket"; //Make sure your socket objects in the prefab have this tag

    public override void OnInspectorGUI()
    {
        //show default inspector
        DrawDefaultInspector();

        // Get the target object
        PrefabData data = (PrefabData)target;

        // 10 pixels space
        EditorGUILayout.Space(10); 

        // Button to update sockets
        if (GUILayout.Button("Update Sockets from Prefab"))
        {
            UpdateSockets(data);
        }
    }

    private void UpdateSockets(PrefabData data)
    {
        // 1. Verify if prefab is assigned
        if (data.Prefab == null)
        {
            Debug.LogError($"[RoadGen] Prefab is not set for {data.name}!", data);
            return;
        }

        // 2. Empty existing sockets
        data.ConnectionSockets.Clear();

        Debug.Log($"[RoadGen] ===== Start scan {data.Prefab.name} =====");

        // 3. Search for socket objects
        HashSet<Vector2Int> foundSockets = new HashSet<Vector2Int>();

        foreach (Transform child in data.Prefab.GetComponentsInChildren<Transform>(true))
        {
            // Ignore the root object
            if (child == data.Prefab.transform)
            {
             continue;
            }

            if (child.CompareTag(SOCKET_TAG))
            {
                // 4. Calculate socket coordinate
                Vector3 localPos = child.localPosition;
            
                int xCoord = (int)System.Math.Round(localPos.x, MidpointRounding.AwayFromZero);
                int zCoord = (int)System.Math.Round(localPos.z, MidpointRounding.AwayFromZero);
            
                Vector2Int socketCoord = new Vector2Int(xCoord, zCoord);

                Debug.Log($"[RoadGen] !!! SOCKET FOUND: {child.name} !!!");
                Debug.Log($"[RoadGen]       ... Tag: {child.tag}");
                Debug.Log($"[RoadGen]       ... Local Position: {localPos.x}, {localPos.z}");
                Debug.Log($"[RoadGen]       ... Calculated Coordonate: {socketCoord}");
            
                // Ignore [0,0] socket
                if (socketCoord == Vector2Int.zero)
                {
                    Debug.LogWarning($"[RoadGen]       ... Coordonate [0,0] is ignored.");
                    continue;
                }

                // 5. Add to list if unique
                if (foundSockets.Contains(socketCoord))
                {
                    Debug.LogWarning($"[RoadGen]       ... Coordonate {socketCoord} it was added. Ignored.");
                }
                else
                {
                    foundSockets.Add(socketCoord);
                    data.ConnectionSockets.Add(socketCoord);
                    Debug.Log($"[RoadGen]       ... Added to the list.");
                }
            }
        }

        // 6. Mark the ScriptableObject as dirty to save changes
        EditorUtility.SetDirty(data);
        Debug.Log($"[RoadGen] ===== Scan finished for {data.name}. It founded {data.ConnectionSockets.Count} unique sockets. =====", data);
    }
}