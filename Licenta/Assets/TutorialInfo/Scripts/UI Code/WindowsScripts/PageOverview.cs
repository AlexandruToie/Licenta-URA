using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class OverviewPage : MonoBehaviour
{
    [Header("Identity UI Elements")]
    public TMP_InputField nameInput;
    public RawImage logoImage;
    
    [Header("Painter Reference")]
    public PixelArtCanvas logoCanvas;

    [Header("Stats UI Elements")]
    public TextMeshProUGUI statsMoneyText;
    public TextMeshProUGUI statsReputationText;
    public TextMeshProUGUI statsEmployeesText; 
    public TextMeshProUGUI statsExpensesText; 

    void Start()
    {
        if (GameManager.Instance != null)
        {
            if (nameInput != null) nameInput.text = GameManager.Instance.companyName;
            UpdateStats();
        }
        if(nameInput != null)
        {
            nameInput.onEndEdit.AddListener(SaveCompanyName);
        }
    }

    void OnEnable() // Subscribe to the UI update event when the page is enabled
    {
        UpdateStats();
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnUIUpdate += UpdateStats;
        }
    }

    void OnDisable() // Unsubscribe to prevent memory leaks
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnUIUpdate -= UpdateStats;
        }
    }

    void SaveCompanyName(string newName)
    {
        if (string.IsNullOrWhiteSpace(newName)) return;

        if (GameManager.Instance != null)
        {
            GameManager.Instance.SetCompanyName(newName);
            Debug.Log($"The company name has been changed to: {newName}");
        }
    }

    void UpdateStats()
    {
        if (GameManager.Instance == null) return;
        
        if (logoImage != null)
        {
            if (logoCanvas != null && logoCanvas.GetTexture() != null)
            {
                logoImage.texture = logoCanvas.GetTexture();
                logoImage.color = Color.white;
            }
            else if (GameManager.Instance.companyLogo != null)
            {
                logoImage.texture = GameManager.Instance.companyLogo;
                logoImage.color = Color.white;
            }
        }

        if(statsMoneyText != null) 
            statsMoneyText.text = $"$ {GameManager.Instance.money:N2}";

        if(statsReputationText != null) 
            statsReputationText.text = $"{GameManager.Instance.reputation:0}/100";

        if(statsExpensesText != null) 
        {
            float expenses = 0;
            if (EconomyManager.Instance != null)
            {
                EconomyManager.Instance.CalculateProjectedExpenses();
                expenses = EconomyManager.Instance.projectedMonthlyExpenses;
            }

            statsExpensesText.text = $"-${expenses:N2}";
            statsExpensesText.color = Color.red; 
        }

        if(statsEmployeesText != null) 
        {
            int employeeCount = 0;
            
            if (HRManager.Instance != null) 
                employeeCount += HRManager.Instance.unassignedEmployees.Count;

            if (UIDepartmentNode.allDepartments != null) 
            {
                foreach (var dept in UIDepartmentNode.allDepartments)
                {
                    if (dept != null && dept.myEmployees != null)
                    {
                        employeeCount += dept.myEmployees.Count;
                    }
                }
            }

            statsEmployeesText.text = $"{employeeCount}"; 
        }
    }
}