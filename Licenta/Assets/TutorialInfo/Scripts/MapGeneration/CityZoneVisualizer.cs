using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

[ExecuteInEditMode]
public class CityZoneVisualizer : MonoBehaviour
{
    [Header("References")]
    public RoadGridManager GridManager;

    [Header("Zone Radius Configuration (Procedural)")]
    [Tooltip("Maximal and minimal radius limits for the CENTER zone.")]
    public Vector2 CenterRadiusLimits = new Vector2(25f, 30f);
    
    [Tooltip("Maximal and minimal width limits for the SUBURBS zone (added to center).")]
    public Vector2 SuburbWidthLimits = new Vector2(30f, 40f);

    [Header("Current Radius Values (Read Only)")]
    public float CenterZoneRadius = 30f; 
    public float SuburbsZoneRadius = 70f; 

    [Header("Zone Colors")]
    public Color CenterColor = new Color(1f, 0.92f, 0.6f, 1f); // Bej
    public Color SuburbsColor = new Color(0.6f, 1f, 0.6f, 1f); // Ligh Green
    public Color OuterZoneColor = new Color(0.1f, 0.6f, 0.1f, 0.3f); // Dark Green

    [Header("Procedural Industrial Generation")]
    public Color IndustrialColor = new Color(1f, 0.5f, 0f, 0.8f); // Orange
    
    [Tooltip("Numărul minim și maxim de clustere industriale.")]
    public Vector2Int MinMaxIndustrialZones = new Vector2Int(2, 4);
    
    [Tooltip("Lățimea minimă și maximă a unui cluster (în grade).")]
    public Vector2 MinMaxWidthDegrees = new Vector2(30f, 60f);

    [Header("Generated Sectors (Read Only)")]
    public List<Vector2> GeneratedSectors = new List<Vector2>();

    //Automatic Validation
    // This function is called whenever a value is changed in the inspector
    private void OnValidate()
    {
        ValidateZoneRadii();
    }

    // This function ensures the zone radii are within defined limits
    private void ValidateZoneRadii()
    {
        // 1. We make sure the center radius is within limits
        CenterZoneRadius = Mathf.Clamp(CenterZoneRadius, CenterRadiusLimits.x, CenterRadiusLimits.y);

        // 2. Calculate current width of the suburbs zone
        float currentWidth = SuburbsZoneRadius - CenterZoneRadius;

        // 3. We force the width to be within limits
        currentWidth = Mathf.Clamp(currentWidth, SuburbWidthLimits.x, SuburbWidthLimits.y);

        // 4. Recalculate the suburbs radius based on the clamped width
        SuburbsZoneRadius = CenterZoneRadius + currentWidth;
    }

    [ContextMenu("Generate New Random Layout")]
    public void GenerateRandomLayout()
    {
        // 1. Randomize the radius values
        CenterZoneRadius = Random.Range(CenterRadiusLimits.x, CenterRadiusLimits.y);
        
        float addedWidth = Random.Range(SuburbWidthLimits.x, SuburbWidthLimits.y);
        SuburbsZoneRadius = CenterZoneRadius + addedWidth; 

        // We make a final validation
        ValidateZoneRadii();

        // 2. Randomly generate industrial sectors
        GenerateIndustrialSectors();

        // 3. Refresh the scene view to reflect changes
#if UNITY_EDITOR
        SceneView.RepaintAll();
#endif
        Debug.Log($"[ZoneLayout] New Layout! Center: {CenterZoneRadius:F1}m, Suburbs: {SuburbsZoneRadius:F1}m (Width: {SuburbsZoneRadius - CenterZoneRadius:F1}m)");
    }

    private void GenerateIndustrialSectors() // Generates random industrial sectors without overlap
    {
        GeneratedSectors.Clear();
        int count = Random.Range(MinMaxIndustrialZones.x, MinMaxIndustrialZones.y + 1);
        int attempts = 0;

        while (GeneratedSectors.Count < count && attempts < 100)
        {
            attempts++;
            float width = Random.Range(MinMaxWidthDegrees.x, MinMaxWidthDegrees.y);
            float startAngle = Random.Range(0f, 360f);

            if (IsSectorValid(startAngle, width))
            {
                GeneratedSectors.Add(new Vector2(startAngle, width));
            }
        }
    }

    private bool IsSectorValid(float start, float width) // Checks if a sector overlaps with existing ones
    {
        float end = start + width;
        foreach(var sec in GeneratedSectors)
        {
            float s2 = sec.x;
            float e2 = sec.x + sec.y;
            bool overlap = (start < e2 && end > s2); 
            if (overlap) return false;
        }
        return true;
    }

    private void OnDrawGizmos() // Draws the zones and sectors in the editor for visualization
    {
#if UNITY_EDITOR
        if (GridManager == null) return;

        // We make sure the radius are valid
        ValidateZoneRadii();

        Vector3 center = GridManager.BuildAreaCenter.position;
        center.y += 0.5f; 
        float maxRadius = GridManager.BuildRadius;

        // 1. The Outer Zone (Dark Green)
        Handles.color = OuterZoneColor;
        Handles.DrawSolidDisc(center, Vector3.up, maxRadius);

        // 2. The Industrial Sectors (Orange)
        Handles.color = IndustrialColor;
        foreach (var sector in GeneratedSectors)
        {
            float startAngle = sector.x;
            float sweepAngle = sector.y;
            Quaternion rot = Quaternion.AngleAxis(startAngle, Vector3.up);
            Vector3 startDir = rot * Vector3.right;
            Handles.DrawSolidArc(center, Vector3.up, startDir, sweepAngle, maxRadius);
        }

        // 3. The Suburbs Zone (Light Green)
        Handles.color = SuburbsColor;
        Handles.DrawSolidDisc(center, Vector3.up, SuburbsZoneRadius);

        // 4. The Center Zone (Beige)
        Handles.color = CenterColor;
        Handles.DrawSolidDisc(center, Vector3.up, CenterZoneRadius);

        // 5. Outlines
        Handles.color = new Color(0,0,0, 0.5f);
        Handles.DrawWireDisc(center, Vector3.up, maxRadius);
        Handles.DrawWireDisc(center, Vector3.up, SuburbsZoneRadius);
        Handles.DrawWireDisc(center, Vector3.up, CenterZoneRadius);
#endif
    }
}