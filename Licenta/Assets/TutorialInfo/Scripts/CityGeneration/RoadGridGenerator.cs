using System.Collections.Generic;
using UnityEngine;

public class RoadGridManager : MonoBehaviour
{
    [Header("Zona de Construcție")]
    public Transform BuildAreaCenter;
    public float BuildRadius = 200f;

    [Header("Setări Teren")]
    public LayerMask TerrainLayer; 
    public float SpawnHeightOffset = 0.1f;

    // Dicționar care mapează coordonata gridului -> Celula ocupată
    private class GridCell
    {
        public PrefabData PlacedPrefabData;
        public GameObject PlacedInstance;
        public Vector2Int RootPosition; // Unde e pivotul obiectului
    }
    private Dictionary<Vector2Int, GridCell> gridData = new Dictionary<Vector2Int, GridCell>();

    // Verifică dacă TOATĂ aria (Size) este liberă
    public bool IsAreaFree(Vector2Int position, Vector2Int size)
    {
        // Calculăm colțul stânga-jos (presupunând pivot central sau ajustat)
        // Pentru simplitate, considerăm position ca fiind pivotul.
        // Ajustează startX/Y în funcție de cum sunt setate pivoturile prefab-urilor tale.
        // Aici presupunem pivotul în centru pentru verificare:
        
        int startX = position.x - (size.x / 2);
        int startY = position.y - (size.y / 2);

        // Dacă size e 1x1, bucla rulează o dată. Dacă e 2x2, de 4 ori.
        for (int x = 0; x < size.x; x++)
        {
            for (int y = 0; y < size.y; y++)
            {
                Vector2Int cellCoord = new Vector2Int(startX + x, startY + y);
                
                // 1. E ocupat în grid?
                if (gridData.ContainsKey(cellCoord)) return false;
                
                // 2. E în afara cercului?
                if (!IsCellInsideBuildArea(cellCoord)) return false;
            }
        }
        return true; 
    }

    public void PlacePrefab(PrefabData data, Vector2Int position, Quaternion rotation)
    {
        if (!_isHeightCalculated) CalculateFlatZoneHeight();

        Vector3 worldPosition = new Vector3(position.x, _flatZoneHeight + SpawnHeightOffset, position.y);
        GameObject instance = Instantiate(data.Prefab, worldPosition, rotation);
        
        GridCell cell = new GridCell { 
            PlacedPrefabData = data, 
            PlacedInstance = instance,
            RootPosition = position
        };

        // MARCAM TOATE CELULELE OCUPATE DE ACEST PREFAB
        int startX = position.x - (data.Size.x / 2);
        int startY = position.y - (data.Size.y / 2);

        for (int x = 0; x < data.Size.x; x++)
        {
            for (int y = 0; y < data.Size.y; y++)
            {
                Vector2Int cellCoord = new Vector2Int(startX + x, startY + y);
                if(!gridData.ContainsKey(cellCoord))
                {
                    gridData[cellCoord] = cell; 
                }
            }
        }
    }

    public void MarkAreaOccupied(Vector2Int center, Vector2Int size)
    {
        int startX = center.x - (size.x / 2);
        int startY = center.y - (size.y / 2);

        for (int x = 0; x < size.x; x++)
        {
            for (int y = 0; y < size.y; y++)
            {
                Vector2Int pos = new Vector2Int(startX + x, startY + y);
                if (!gridData.ContainsKey(pos))
                {
                    gridData.Add(pos, new GridCell { PlacedInstance = null, PlacedPrefabData = null }); 
                }
            }
        }
    }

    public void RemovePrefabAt(Vector2Int position)
    {
        if (gridData.TryGetValue(position, out GridCell cell))
        {
            // Distrugem obiectul fizic
            if(cell.PlacedInstance != null) Destroy(cell.PlacedInstance);
            
            // Căutăm TOATE cheile care referă acest obiect (pentru obiecte 2x2, 3x3)
            List<Vector2Int> keysToRemove = new List<Vector2Int>();
            foreach (var pair in gridData) 
            { 
                // Verificăm instanța sau datele
                if (pair.Value == cell || (cell.PlacedInstance != null && pair.Value.PlacedInstance == cell.PlacedInstance)) 
                {
                    keysToRemove.Add(pair.Key); 
                }
            }
            foreach (var key in keysToRemove) { gridData.Remove(key); }
        }
    }

    public GameObject GetPrefabAt(Vector2Int position)
    {
        if (gridData.TryGetValue(position, out GridCell cell)) return cell.PlacedInstance;
        return null;
    }

    // --- Internals ---
    private float _flatZoneHeight = 0f; 
    private bool _isHeightCalculated = false;

    private void CalculateFlatZoneHeight()
    {
        if (BuildAreaCenter == null) return;
        Vector3 centerPos = BuildAreaCenter.position;
        Vector3 rayOrigin = new Vector3(centerPos.x, 1000f, centerPos.z);
        if (Physics.Raycast(rayOrigin, Vector3.down, out RaycastHit hit, 2000f, TerrainLayer))
        {
            _flatZoneHeight = hit.point.y;
            _isHeightCalculated = true;
        }
    }

    private bool IsCellInsideBuildArea(Vector2Int cellCoord)
    {
        if (BuildAreaCenter == null) return false;
        Vector2 pos = new Vector2(cellCoord.x, cellCoord.y);
        Vector3 center3D = BuildAreaCenter.position;
        Vector2 center = new Vector2(center3D.x, center3D.z);
        return Vector2.Distance(pos, center) < BuildRadius;
    }
}