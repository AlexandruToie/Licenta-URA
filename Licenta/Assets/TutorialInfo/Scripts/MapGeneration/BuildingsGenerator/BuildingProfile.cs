using UnityEngine;

public enum BuildingType
{
    House,      
    Apartment,  
    Factory,    
    Office,     
    Hospital,
    Shop    
}

[CreateAssetMenu(fileName = "NewBuilding", menuName = "CityGenerator/Building Profile")]
public class BuildingProfile : ScriptableObject
{
    [Header("Visuals")]
    [Tooltip("The 3D model prefab.")]
    public GameObject Prefab;

    [Header("Grid Settings")]
    public BuildingType Type;
    
    [Tooltip("Grid size of the building in cells (X: width, Y: height).")]
    public Vector2Int Size = new Vector2Int(1, 1);
    
    [Tooltip("If checked, the building can only appear on corners (optional).")]
    public bool CornerOnly = false;

    [Header("Transform Adjustments")]
    [Tooltip("Modify the position of the model relative to the grid cell center.")]
    public Vector3 PositionOffset = Vector3.zero;

    [Tooltip("Rotate the model (in degrees). Usually only modify Y.")]
    public Vector3 RotationOffset = Vector3.zero;

    [Tooltip("Scale of the model on axes. (1, 1, 1) is the original size.")]
    public Vector3 Scale = Vector3.one; 
}