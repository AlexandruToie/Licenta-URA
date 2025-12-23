using System.Collections.Generic;
using UnityEngine;


[CreateAssetMenu(fileName = "PrefabData_", menuName = "Procedural Roads/Prefab Data")]
public class PrefabData : ScriptableObject
{
    public GameObject Prefab;
    public Vector2Int Size = Vector2Int.one;
    public List<Vector2Int> ConnectionSockets = new List<Vector2Int>();
}
