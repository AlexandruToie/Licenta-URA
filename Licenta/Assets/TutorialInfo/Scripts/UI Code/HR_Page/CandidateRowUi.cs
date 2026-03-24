using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class CandidateRowUI : MonoBehaviour
{
    public TextMeshProUGUI nameText;
    public Slider managementBar, productionBar, marketingCustomerBar, marketingSalesBar;
    
    public Button hireButton;
    public TextMeshProUGUI hireButtonText; 
    public Button rejectButton;

    private Employee myCandidate;
    private int hirePrice; 

    public void Initialize(Employee candidate)
    {
        myCandidate = candidate;

        if (nameText != null) nameText.text = candidate.employeeName;

        SetupSlider(managementBar, candidate.managementSkill, candidate.managementPotential);
        SetupSlider(productionBar, candidate.productionSkill, candidate.productionPotential);
        SetupSlider(marketingCustomerBar, candidate.marketingCustomerSkill, candidate.marketingCustomerPotential);
        SetupSlider(marketingSalesBar, candidate.marketingSalesSkill, candidate.marketingSalesPotential);

        float avgSkill = (candidate.managementSkill + candidate.productionSkill + candidate.marketingCustomerSkill + candidate.marketingSalesSkill) / 4f;

        hirePrice = Mathf.RoundToInt(25f + (avgSkill * 10f));// the formula to calculate the hireing price

        if (hireButtonText != null)
        {
            hireButtonText.text = $"Hire (${hirePrice})";
        }

        hireButton.onClick.RemoveAllListeners();
        hireButton.onClick.AddListener(OnHireClicked);

        rejectButton.onClick.RemoveAllListeners();
        rejectButton.onClick.AddListener(OnRejectClicked);
    }

    private void SetupSlider(Slider slider, float currentValue, float maxValue)
    {
        if (slider == null) return;
        slider.maxValue = maxValue;
        slider.value = currentValue;
    }

    private void OnHireClicked()
    {
        if (GameManager.Instance.money < hirePrice)
        {
            if (ErrorManager.Instance != null)
            {
                ErrorManager.Instance.ShowErrorAtCursor("ERROR: Not enough money!");
            }
            return; 
        }

        GameManager.Instance.AddMoney(-hirePrice);

        HRManager.Instance.marketCandidates.Remove(myCandidate);
        
        HRManager.Instance.SendToUnassigned(myCandidate);
        
        if (HRPageUIManager.Instance != null)
        {
            HRPageUIManager.Instance.RefreshHRPage();
        }
    }

    private void OnRejectClicked()
    {
        HRManager.Instance.marketCandidates.Remove(myCandidate);
        
        if (HRPageUIManager.Instance != null)
        {
            HRPageUIManager.Instance.RefreshHRPage();
        }
    }
}