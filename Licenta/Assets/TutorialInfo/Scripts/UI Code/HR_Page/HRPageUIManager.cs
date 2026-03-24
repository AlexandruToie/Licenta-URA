using UnityEngine;

public class HRPageUIManager : MonoBehaviour
{
    public static HRPageUIManager Instance;//It enables the script to be accessed from other scripts
    [Header("Staff")]
    public Transform staffContentArea;   
    public GameObject hrStaffRowPrefab;  

    [Header("Recruitment")]
    public Transform marketContentArea;  
    public GameObject candidateRowPrefab; 

    void Awake()
    {
        Instance = this;
    } 

    void OnEnable()
    {
        RefreshHRPage();
    }

    public void RefreshHRPage()
    {
        if (HRManager.Instance == null) return;

        foreach (Transform child in staffContentArea) Destroy(child.gameObject);
        foreach (Transform child in marketContentArea) Destroy(child.gameObject);

        foreach (Employee emp in HRManager.Instance.unassignedEmployees)
        {
            GameObject newRow = Instantiate(hrStaffRowPrefab, staffContentArea);
            HRStaffRowUI script = newRow.GetComponent<HRStaffRowUI>();
            
            if (script != null)
            {
                script.Initialize(emp);
            }
        }

        foreach (Employee cand in HRManager.Instance.marketCandidates)
        {
            GameObject newRow = Instantiate(candidateRowPrefab, marketContentArea);
            CandidateRowUI script = newRow.GetComponent<CandidateRowUI>();
            
            if (script != null)
            {
                script.Initialize(cand);
            }
        }
    }

    public void OnBuyNewCandidatesClicked()
    {
        if (GameManager.Instance.money >= 100)
        {
            GameManager.Instance.AddMoney(-100); 
            HRManager.Instance.GenerateCandidates(4); 
            RefreshHRPage();
        }
        else
        {
            if (ErrorManager.Instance != null)
                ErrorManager.Instance.ShowErrorAtCursor("ERROR: Not enough money!");
        }
    }
}