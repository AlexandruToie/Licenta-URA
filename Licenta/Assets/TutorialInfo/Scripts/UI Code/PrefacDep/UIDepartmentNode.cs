using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using System;
using Random = UnityEngine.Random;

public class UIDepartmentNode : MonoBehaviour, IDragHandler, IPointerDownHandler
{
    public static List<UIDepartmentNode> allDepartments = new List<UIDepartmentNode>();
    private RectTransform rectTransform;
    private Canvas canvas;

    [Header("Connectors")]
    public GameObject connectorsContainer;

    [Header("Visual references")]
    public TextMeshProUGUI departmentTitleText;
    public TextMeshProUGUI statsText; 
    public GameObject manageButtonObj;
    public Image backgroundImage;   

    [Header("Hierarchical relationships")]
    public UIDepartmentNode myBoss;
    public List<UIDepartmentNode> mySubordinates = new List<UIDepartmentNode>(); 

    [Header("Staff Management")]
    public List<Employee> myEmployees = new List<Employee>();

    [Header("Efficiency")]
    public float currentEfficiency;
    private float boostEfficency = 0.15f; //this can be upgraded in the upgrade page!!!

    public DepartmentTypeSO myType;
    private DateTime lastActiveTime;

    void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        canvas = GetComponentInParent<Canvas>();
        SetPointsActive(false); 
        allDepartments.Add(this);
    }

    void OnDisable()
    {
        lastActiveTime = DateTime.Now;
    }

    void OnEnable()
    {
        if (lastActiveTime != default(DateTime))
        {
            float secondsPassed = (float)(DateTime.Now - lastActiveTime).TotalSeconds;
            
            foreach (var emp in myEmployees)
            {
                if (emp.isTraining)
                {
                    emp.trainingTimer -= secondsPassed;

                    if (emp.trainingTimer <= 0)
                    {
                        FinishTraining(emp);
                    }
                }
            }
        }
    }

    void Update()
    {
        foreach (var emp in myEmployees)
        {
            if (emp.isTraining)
            {
                emp.trainingTimer -= Time.deltaTime;

                if (emp.trainingTimer <= 0)
                {
                    FinishTraining(emp);
                }
            }
        }
    }

    public static void UpdateAllDepartmentsUI()
    {
        foreach (var node in allDepartments)
        {
            if (node.myBoss == null)
            {
                node.UpdateHierarchyUI();
            }
        }
    }

    public void UpdateHierarchyUI()
    {
        UpdateNodeUI(); 
        foreach (var sub in mySubordinates)
        {
            sub.UpdateHierarchyUI();
        }
    }

    private float GetRelevantSkill(Employee emp)
    {
        if (myType == null || string.IsNullOrEmpty(myType.defaultName)) return 0f;

        string depName = myType.defaultName.ToLower();

        if (myType.isCEO || depName.Contains("management") || depName.Contains("manager"))
        {
            return emp.managementSkill;
        }
        else if (depName.Contains("production") || depName.Contains("creation"))
        {
            return emp.productionSkill;
        }
        else if (depName.Contains("human resources") || depName.Contains("hr") || 
                 depName.Contains("customer support") || depName.Contains("sales"))
        {
            return (emp.marketingCustomerSkill + emp.marketingSalesSkill) / 2f;
        }
        else if (depName.Contains("administrator") || depName.Contains("network"))
        {
            return (emp.marketingSalesSkill + emp.managementSkill + emp.productionSkill) / 3f;
        }
        
        return 0f;
    }

    public void UpdateNodeUI()
    {
        if (myType == null || statsText == null) return;

        int currentEmpCount = myEmployees.Count;
        int maxEmpCount = myType.maxEmployees; 
        float baseEfficiency = 0f;
        if (myType.isCEO)
        {
            baseEfficiency = 100f; 
        }
        else if (currentEmpCount > 0)
        {
            float sumSkills = 0f;
            foreach(var emp in myEmployees)
            {
                sumSkills += GetRelevantSkill(emp);
            }
            baseEfficiency = sumSkills / currentEmpCount; 
        }

        currentEfficiency = baseEfficiency;
        if (myBoss != null && !myType.isCEO && !myBoss.myType.isCEO)
        {
            string myDepName = myType.defaultName.ToLower();
            string bossDepName = myBoss.myType.defaultName.ToLower();
            bool isCorrectManager = 
                (myDepName.Contains("production") && bossDepName.Contains("production")) ||
                (myDepName.Contains("creation") && bossDepName.Contains("creation")) ||
                ((myDepName.Contains("customer") || myDepName.Contains("sales")) && bossDepName.Contains("marketing")); 
            float bossEfficiencyShare = myBoss.currentEfficiency * boostEfficency;

            if (isCorrectManager)
            {
                currentEfficiency += bossEfficiencyShare;
            }
            else
            {
                currentEfficiency -= bossEfficiencyShare;
            }
        }
        currentEfficiency = Mathf.Max(0f, currentEfficiency);

        statsText.text = $"LVL: 1\nEfficiency: {Mathf.RoundToInt(currentEfficiency)}%\nEmployees: {currentEmpCount}/{maxEmpCount}";
    }

    private void FinishTraining(Employee emp)
    {
        emp.isTraining = false;
        
        float trainingGain = Random.Range(0.5f, 5f); 
        
        switch (emp.currentlyTrainingSkill)
        {
            case SkillType.Management:
                emp.managementSkill = Mathf.Min(emp.managementSkill + trainingGain, emp.managementPotential);
                break;
            case SkillType.Production:
                emp.productionSkill = Mathf.Min(emp.productionSkill + trainingGain, emp.productionPotential);
                break;
            case SkillType.CustomerCare:
                emp.marketingCustomerSkill = Mathf.Min(emp.marketingCustomerSkill + trainingGain, emp.marketingCustomerPotential);
                break;
            case SkillType.Sales:
                emp.marketingSalesSkill = Mathf.Min(emp.marketingSalesSkill + trainingGain, emp.marketingSalesPotential);
                break;
        }

        emp.currentlyTrainingSkill = SkillType.None;
        Debug.Log($"{emp.employeeName} finished training! Got +{trainingGain:F1}");
        
        UIDepartmentNode.UpdateAllDepartmentsUI();
    }

    public void DismissEmployee(Employee emp)
    {
        if (myEmployees.Contains(emp))
        {
            myEmployees.Remove(emp);
            
            if (HRManager.Instance != null)
            {
                HRManager.Instance.SendToUnassigned(emp);
            }
            
            Debug.Log($"Dismissed employee: {emp.employeeName}");
            UIDepartmentNode.UpdateAllDepartmentsUI();
        }
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        rectTransform.SetAsLastSibling();
        if (DepartmentSelectionManager.Instance != null)
        {
            DepartmentSelectionManager.Instance.SelectNode(this);
        }
        SetPointsActive(true);

        ConnectionManager.Instance.HideAllNodePoints();
        if(connectorsContainer != null) connectorsContainer.SetActive(true);
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (canvas == null) return;
        
        Vector2 adjustedDelta = eventData.delta / canvas.scaleFactor; 

        if (transform.parent != null)
        {
            adjustedDelta /= transform.parent.localScale.x; 
        }
        rectTransform.anchoredPosition += adjustedDelta; 
    }

    public void SetPointsActive(bool isActive)
    {
        if (connectorsContainer != null) connectorsContainer.SetActive(isActive);
    }
    
    public void Deselect()
    {
        SetPointsActive(false); 
    }

    public void InitializeNode(string customName, DepartmentTypeSO type)
    {
        myType = type;
        if (departmentTitleText != null)
        {
            departmentTitleText.text = customName;
        }

        if (backgroundImage != null)
        {
            backgroundImage.color = type.themeColor;
        }

        if (myType.isCEO)
        {
            if (manageButtonObj != null) manageButtonObj.SetActive(false);
            Employee player = new Employee("Player (CEO)");
            
            player.managementSkill = 100f;
            player.productionSkill = 100f;
            player.marketingCustomerSkill = 100f;
            player.marketingSalesSkill = 100f;
            
            myEmployees.Add(player);
        }

        UIDepartmentNode.UpdateAllDepartmentsUI();
    }

    // public void HireRandomEmployee() //NEEDS IMPROVEMENT FOR THE NAMES!
    // {
    //     string[] names = { "Alex", "Maria", "Dan", "Elena", "Victor", "Ioana" };
    //     string randomName = names[Random.Range(0, names.Length)];
        
    //     Employee newEmp = new Employee(randomName);
    //     myEmployees.Add(newEmp);
        
    //     UpdateNodeUI();
    // }

    public void OpenManagePanel()
    {
        if (DepartmentPanelManager.Instance != null)
        {
            DepartmentPanelManager.Instance.OpenPanel(this);
        }
    }

    public void DeleteDepartment()
    {
        foreach (var emp in myEmployees)
        {
            if (HRManager.Instance != null)
            {
                HRManager.Instance.SendToUnassigned(emp);
            }
        }
        myEmployees.Clear();

        if (myBoss != null)
        {
            myBoss.mySubordinates.Remove(this);
        }

        foreach (var sub in mySubordinates)
        {
            sub.myBoss = null;
        }

        if (GameManager.Instance != null && myType != null)
        {
            float refundAmount = myType.buildCost * 0.5f;
            GameManager.Instance.AddMoney(refundAmount);
            Debug.Log($"Refunded ${refundAmount} for demolishing {myType.defaultName}");
        }

        DepartmentsManager depsManager = FindFirstObjectByType<DepartmentsManager>();
        if (depsManager != null && myType != null)
        {
            depsManager.RemoveDepartment(myType);
        }

        allDepartments.Remove(this);
        Destroy(gameObject);
    }
}