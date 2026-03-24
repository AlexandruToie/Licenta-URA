using UnityEngine;
using UnityEngine.UI;
using TMPro;

[RequireComponent(typeof(Button))] 
public class BuildButtonWrapper : MonoBehaviour
{
    [Header("Settings")]
    public DepartmentTypeSO departmentType;
    public DepartmentsManager manager;      

    [Header("UI Feedback")]
    public TextMeshProUGUI buttonText; 

    private Button myButton;
    private string originalText;

    void Awake()
    {
        myButton = GetComponent<Button>();
        if (buttonText != null)
        {
            originalText = buttonText.text;
        }
    }

    void Update()
    {
        if (departmentType == null || GameManager.Instance == null) return;
        bool hasReputation = GameManager.Instance.reputation >= departmentType.requiredReputation;
        int currentBuilt = GetBuiltCount();
        bool hasReachedMax = currentBuilt >= departmentType.maxNumberOfDepartments;
        bool hasMoney = GameManager.Instance.money >= departmentType.buildCost;

        if (!hasReputation)
        {
            myButton.interactable = false; 
            if (buttonText != null) 
            {
                buttonText.text = $"Req. {departmentType.requiredReputation} Rep";
                buttonText.color = Color.black;
            }
        }
        else if (hasReachedMax)
        {
            myButton.interactable = false;
            if (buttonText != null) 
            {
                buttonText.text = "MAX BUILT";
                buttonText.color = Color.gray;
            }
        }
        else
        {
            myButton.interactable = true; 
            
            if (buttonText != null) 
            {
                buttonText.text = $"{originalText}";
                buttonText.color = hasMoney ? Color.black : new Color(1f, 0.3f, 0.3f); 
            }
        }
    }

    public void OnClick()
    {
        if (manager != null)
        {
            manager.RequestBuild(departmentType);
        }
    }
    private int GetBuiltCount()
    {
        int count = 0;
        foreach (UIDepartmentNode node in UIDepartmentNode.allDepartments)
        {
            if (node != null && node.myType == departmentType) 
            {
                count++;
            }
        }
        return count;
    }
}