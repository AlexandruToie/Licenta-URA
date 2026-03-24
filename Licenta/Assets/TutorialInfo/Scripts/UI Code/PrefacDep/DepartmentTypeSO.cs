using UnityEngine;

[CreateAssetMenu(fileName = "NewDeptType", menuName = "Tycoon/Department Type")]
public class DepartmentTypeSO : ScriptableObject
{
    public string defaultName = "Marketing"; //The name of the department
    public float buildCost = 1000f;          // How much money it costs to create
    public int requiredReputation = 0;       // The reputation needed to unlock this department
    public int maxEmployees = 10;            // The maximum number of employees that can work in this department
    public int maximPossibleEmployees = 20;  // The absolute maximum number of employees (for balancing purposes)
    public int maxNumberOfDepartments = 1;      // The maximum number of departments that can be created of this type
    public float costIncreasePerNewDepartment = 1.5f; // How much the cost increases for each new department of this type
    public bool getOneForFreeAtTheStart = false;
    public Color themeColor = Color.white;   // The color of the node
    public bool isCEO = false;
}