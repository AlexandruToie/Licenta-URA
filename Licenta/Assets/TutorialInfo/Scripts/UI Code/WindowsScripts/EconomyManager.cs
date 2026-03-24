using UnityEngine;

public class EconomyManager : MonoBehaviour
{
    public static EconomyManager Instance;

    [Header("Salary Settings")]
    public int unassignedSalary = 20; //Minimum salary for unassigned employees
    public int defaultSalary = 100;   // Default salary

    public int projectedMonthlyExpenses = 0;

    void Awake()
    {
        Instance = this;
    }

    void Update()
    {
        CalculateProjectedExpenses();
    }

    // This function will be called once every 30 days
    public void PaySalaries()
    {
        int totalSalariesToPay = 0;

        if (HRManager.Instance != null)
        {
            totalSalariesToPay += HRManager.Instance.unassignedEmployees.Count * unassignedSalary;
        }

        foreach (UIDepartmentNode dept in UIDepartmentNode.allDepartments)
        {
            if (dept.myType != null)
            {
                int salaryForThisRole = GetSalaryForDepartment(dept.myType.defaultName);
                totalSalariesToPay += dept.myEmployees.Count * salaryForThisRole;
            }
        }
        if (GameManager.Instance != null && totalSalariesToPay > 0)
        {
            GameManager.Instance.AddMoney(-totalSalariesToPay);
            Debug.Log($"[Payday] ${totalSalariesToPay} deducted for employee salaries!");

            if (ErrorManager.Instance != null)
            {
                ErrorManager.Instance.ShowErrorAtCursor($"PAYDAY: -${totalSalariesToPay} in salaries!");
            }
        }
    }

    public void CalculateProjectedExpenses()
    {
        int total = 0;

        if (HRManager.Instance != null)
        {
            total += HRManager.Instance.unassignedEmployees.Count * unassignedSalary;
        }

        foreach (UIDepartmentNode dept in UIDepartmentNode.allDepartments)
        {
            if (dept.myType != null)
            {
                int salaryForThisRole = GetSalaryForDepartment(dept.myType.defaultName);
                total += dept.myEmployees.Count * salaryForThisRole;
            }
        }

        // TODO: Add other recurring expenses here (e.g., server costs, software licenses)

        projectedMonthlyExpenses = total;
    }

    private int GetSalaryForDepartment(string deptName)
    {
        if (string.IsNullOrEmpty(deptName)) return defaultSalary;

        string nameLower = deptName.ToLower();

        if (nameLower.Contains("ceo")) return 0;
        if (nameLower.Contains("manager")) return 800;       // Any kind of manager
        if (nameLower.Contains("network")) return 600;       // Network Admin
        if (nameLower.Contains("hr")) return 500;            // Human Resources
        if (nameLower.Contains("marketing")) return 400;     // Marketing
        if (nameLower.Contains("production")) return 300;    // Production
        if (nameLower.Contains("creation")) return 350;      // Creation Dept

        return defaultSalary; 
    }
}