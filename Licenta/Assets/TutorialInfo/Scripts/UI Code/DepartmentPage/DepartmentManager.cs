using UnityEngine;
using System.Collections.Generic;

public class DepartmentsManager : MonoBehaviour
{
    [Header("Essential References")]
    public Transform nodesContainer; 
    public GameObject nodePrefab;    
    public ConstructionPopup popupScript; 

    private Dictionary<DepartmentTypeSO, int> builtDepartmentsCount = new Dictionary<DepartmentTypeSO, int>(); 
 
    public int globalOfficeSpace = 50; 
    
    private int currentTotalEmployees => GameManager.Instance != null ? GameManager.Instance.totalEmployees : 0;

    public void RequestBuild(DepartmentTypeSO type)
    {
        if (GameManager.Instance == null)
        {
            Debug.LogError("GameManager is not found!");
            return;
        }

        int count = builtDepartmentsCount.ContainsKey(type) ? builtDepartmentsCount[type] : 0;
        
        if (count >= type.maxNumberOfDepartments)
        {
            ErrorManager.Instance.ShowErrorAtCursor($"ERROR: Limit reached! Max {type.maxNumberOfDepartments} for {type.defaultName}!");
            return; 
        }

        if (GameManager.Instance.reputation < type.requiredReputation)
        {
            ErrorManager.Instance.ShowErrorAtCursor($"ERROR: You need {type.requiredReputation} reputation to build {type.defaultName}!");
            return;
        }

        int spaceLeft = globalOfficeSpace - currentTotalEmployees;
        if (spaceLeft < type.maxEmployees)
        {
            ErrorManager.Instance.ShowErrorAtCursor($"ERROR: Not enough space! Need {type.maxEmployees} spaces.");
            return;
        }

        float actualCost = type.buildCost; 

        if (count > 0 && type.costIncreasePerNewDepartment > 0)
        {
            float increaseAmount = type.buildCost * type.costIncreasePerNewDepartment * count; 
            actualCost = type.buildCost + increaseAmount;
        }

        if (GameManager.Instance.money < (double)actualCost)
        {
            ErrorManager.Instance.ShowErrorAtCursor($"ERROR: Not enough money! Cost: ${actualCost}");
            return;
        }

        popupScript.OpenPopup(type, actualCost, FinalizeBuild); 
    }

    void FinalizeBuild(string chosenName, DepartmentTypeSO type, float finalCost)
    {
        GameManager.Instance.AddMoney(-finalCost); 

        if (builtDepartmentsCount.ContainsKey(type))
            builtDepartmentsCount[type]++;
        else
            builtDepartmentsCount.Add(type, 1);

        GameObject newNodeObj = Instantiate(nodePrefab, nodesContainer);
        
        newNodeObj.GetComponent<RectTransform>().anchoredPosition = new Vector2(Random.Range(-50, 50), Random.Range(-50, 50)); 

        UIDepartmentNode nodeScript = newNodeObj.GetComponent<UIDepartmentNode>();
        
        if (nodeScript != null)
        {
            nodeScript.InitializeNode(chosenName, type);
        }
    }

    public void RemoveDepartment(DepartmentTypeSO type)
    {
        if (builtDepartmentsCount.ContainsKey(type))
        {
            builtDepartmentsCount[type]--;
            if (builtDepartmentsCount[type] < 0) 
                builtDepartmentsCount[type] = 0;
        }
    }
}