using UnityEngine;
using TMPro;

public class DepartmentPanelManager : MonoBehaviour
{
    public static DepartmentPanelManager Instance;

    [Header("UI References")]
    public GameObject managePanel;         
    public TextMeshProUGUI departmentTitleText; 
    public TextMeshProUGUI employeeCountText;   
    public Transform scrollContent;         
    public GameObject employeeRowPrefab;    

    private UIDepartmentNode currentNode;    

    void Awake()
    {
        Instance = this;
        if (managePanel != null) managePanel.SetActive(false);
    }

    public void OpenPanel(UIDepartmentNode node)
    {
        currentNode = node;
        managePanel.SetActive(true);

        if (departmentTitleText != null) 
        {
            departmentTitleText.text = node.myType.defaultName;
        }
        
        RefreshList(); 
    }

    public void ClosePanel()
    {
        managePanel.SetActive(false);
        currentNode = null;
    }

    public void RefreshList()
    {
        if (currentNode == null) return;

        if (employeeCountText != null)
        {
            employeeCountText.text = $"Employees: {currentNode.myEmployees.Count} / {currentNode.myType.maxEmployees}";
        }

        foreach (Transform child in scrollContent)
        {
            Destroy(child.gameObject);
        }

        foreach (Employee emp in currentNode.myEmployees)
        {
            GameObject newRow = Instantiate(employeeRowPrefab, scrollContent);
            
            EmployeeRowUI rowScript = newRow.GetComponent<EmployeeRowUI>();
            if (rowScript != null)
            {
                rowScript.Initialize(emp, currentNode);
            }
        }
    }

    public void OnDeleteDepartmentClicked()
    {
        if (currentNode == null) return;

        ConfirmationManager.Instance.ShowConfirmation(
            "Are you sure you want to demolish this department?", 
            () => 
            {
                currentNode.DeleteDepartment(); 
                ClosePanel();                   
            }
        );
    }
}