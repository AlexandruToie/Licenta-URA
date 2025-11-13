using UnityEngine;
using System.Collections.Generic;

[System.Serializable]
public class IntersectionPrefabEntry
{
    /// <summary>
    /// The number of connected edges this prefab is for.
    /// 1 = Cul-de-sac
    /// 3 = T-Junction
    /// 4 = Crossroads
    /// </summary>
    public int ConnectionCount;
    
    /// <summary>
    /// The prefab to spawn (must have IntersectionSocket components).
    /// </summary>
    public GameObject Prefab;
}

[CreateAssetMenu(fileName = "IntersectionDatabase", menuName = "Roads/Intersection Database", order = 1)]
public class IntersectionDatabase : ScriptableObject
{
    public List<IntersectionPrefabEntry> IntersectionPrefabs;

    /// <summary>
    /// Finds the correct prefab based on the number of roads
    /// connecting to an intersection.
    /// </summary>
    public GameObject GetPrefabForConnectionCount(int count)
    {
        foreach (var entry in IntersectionPrefabs)
        {
            if (entry.ConnectionCount == count)
            {
                return entry.Prefab;
            }
        }
        
        Debug.LogWarning($"No prefab found for connection count: {count}");
        return null;
    }
}