using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

public class HRStaffRowUI : MonoBehaviour
{
    public TextMeshProUGUI nameText;
    public Slider managementBar, productionBar, marketingCustomerBar, marketingSalesBar;
    
    [Header("Assign System")]
    public TMP_Dropdown departmentDropdown; 
    public Button assignButton;
    public TextMeshProUGUI assignButtonText; 
    
    public Button permanentFireButton;
    public TextMeshProUGUI fireButtonText;

    private Employee myEmployee;
    private List<UIDepartmentNode> availableDepartments = new List<UIDepartmentNode>();
    private bool isSelectingDepartment = false; 

    public void Initialize(Employee emp)
    {
        myEmployee = emp;

        if (nameText != null) nameText.text = emp.employeeName;

        SetupSlider(managementBar, emp.managementSkill, emp.managementPotential);
        SetupSlider(productionBar, emp.productionSkill, emp.productionPotential);
        SetupSlider(marketingCustomerBar, emp.marketingCustomerSkill, emp.marketingCustomerPotential);
        SetupSlider(marketingSalesBar, emp.marketingSalesSkill, emp.marketingSalesPotential);

        ResetUIState();

        assignButton.onClick.RemoveAllListeners();
        assignButton.onClick.AddListener(OnAssignClicked);

        permanentFireButton.onClick.RemoveAllListeners();
        permanentFireButton.onClick.AddListener(OnPermanentFireClicked);

        PopulateDropdown();
    }

    private void ResetUIState()
    {
        isSelectingDepartment = false;
        if (departmentDropdown != null) departmentDropdown.gameObject.SetActive(false);
        if (assignButtonText != null) assignButtonText.text = "Assign";
        if (fireButtonText != null) fireButtonText.text = "Fire";
    }

    private void PopulateDropdown()
    {
        if (departmentDropdown == null) return;

        departmentDropdown.ClearOptions();
        availableDepartments.Clear();

        List<string> dropOptions = new List<string>();

        foreach (UIDepartmentNode dept in UIDepartmentNode.allDepartments)
        {
            if (dept.myEmployees.Count < dept.myType.maxEmployees)
            {
                availableDepartments.Add(dept);
                string optionText = $"{dept.myType.defaultName} ({dept.myEmployees.Count}/{dept.myType.maxEmployees})";
                dropOptions.Add(optionText);
            }
        }

        if (availableDepartments.Count == 0)
        {
            dropOptions.Add("No deps built");
            assignButton.interactable = false;
        }
        else assignButton.interactable = true;

        departmentDropdown.AddOptions(dropOptions);
    }

    private void SetupSlider(Slider slider, float currentValue, float maxValue)
    {
        if (slider == null) return;
        slider.maxValue = maxValue;
        slider.value = currentValue;
    }

    private void OnAssignClicked()
    {
        if (availableDepartments.Count == 0) return;

        if (!isSelectingDepartment)
        {
            departmentDropdown.gameObject.SetActive(true); 
            if (assignButtonText != null) assignButtonText.text = "Confirm"; 
            if (fireButtonText != null) fireButtonText.text = "Cancel";
            isSelectingDepartment = true;
            return; 
        }

        int selectedIndex = departmentDropdown.value;
        UIDepartmentNode selectedDept = availableDepartments[selectedIndex];

        selectedDept.myEmployees.Add(myEmployee);
        selectedDept.UpdateNodeUI(); 
        HRManager.Instance.unassignedEmployees.Remove(myEmployee);

        if (HRPageUIManager.Instance != null)
        {
            HRPageUIManager.Instance.RefreshHRPage();
        }

        Destroy(gameObject);
    }

    private void OnPermanentFireClicked()
    {
        if (isSelectingDepartment)
        {
            ResetUIState();
            return;
        }
        HRManager.Instance.unassignedEmployees.Remove(myEmployee);
        Destroy(gameObject);
    }
}