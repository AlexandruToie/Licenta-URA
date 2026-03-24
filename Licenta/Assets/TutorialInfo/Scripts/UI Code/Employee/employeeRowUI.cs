using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class EmployeeRowUI : MonoBehaviour
{
    [Header("Base info")]
    public TextMeshProUGUI nameText;
    
    [Header("Progress bars")]
    public Slider managementBar;
    public Slider productionBar;
    public Slider marketingCustomerBar;
    public Slider marketingSalesBar;

    [Header("Training System")]
    public GameObject mainTrainButtonObj;  
    public GameObject buttonContainer;   
    public GameObject skillChoiceContainer;    
    public Button btnTrainMng, btnTrainProd, btnTrainCust, btnTrainSales; 
    public GameObject timerContainer;        
    public TextMeshProUGUI timerText;        

    [Header("Fire System")]
    public Button fireButton;

    private Employee myEmployee;
    private UIDepartmentNode myDepartment;
    
    private SkillType selectedSkillToTrain;
    private int currentTrainingCost;

    public void Initialize(Employee emp, UIDepartmentNode department)
    {
        myEmployee = emp;
        myDepartment = department;

        if (nameText != null) nameText.text = emp.employeeName;

        SetupSlider(managementBar, emp.managementSkill, emp.managementPotential);
        SetupSlider(productionBar, emp.productionSkill, emp.productionPotential);
        SetupSlider(marketingCustomerBar, emp.marketingCustomerSkill, emp.marketingCustomerPotential);
        SetupSlider(marketingSalesBar, emp.marketingSalesSkill, emp.marketingSalesPotential);

        Button mainTrainBtn = mainTrainButtonObj.GetComponent<Button>();
        mainTrainBtn.onClick.RemoveAllListeners();
        mainTrainBtn.onClick.AddListener(OnMainTrainClicked);

        fireButton.onClick.RemoveAllListeners();
        fireButton.onClick.AddListener(OnFireClicked);

        btnTrainMng.onClick.RemoveAllListeners();
        btnTrainMng.onClick.AddListener(() => PromptTraining(SkillType.Management));

        btnTrainProd.onClick.RemoveAllListeners();
        btnTrainProd.onClick.AddListener(() => PromptTraining(SkillType.Production));

        btnTrainCust.onClick.RemoveAllListeners();
        btnTrainCust.onClick.AddListener(() => PromptTraining(SkillType.CustomerCare));

        btnTrainSales.onClick.RemoveAllListeners();
        btnTrainSales.onClick.AddListener(() => PromptTraining(SkillType.Sales));

        UpdateVisualState();
    }

    void Update()
    {
        if (myEmployee == null) return;

        if (myEmployee.isTraining)
        {
            UpdateVisualTimer();
        }
        else if (timerContainer.activeSelf)
        {
            SetupSlider(managementBar, myEmployee.managementSkill, myEmployee.managementPotential);
            SetupSlider(productionBar, myEmployee.productionSkill, myEmployee.productionPotential);
            SetupSlider(marketingCustomerBar, myEmployee.marketingCustomerSkill, myEmployee.marketingCustomerPotential);
            SetupSlider(marketingSalesBar, myEmployee.marketingSalesSkill, myEmployee.marketingSalesPotential);

            UpdateVisualState();
        }
    }

    private void SetupSlider(Slider slider, float currentValue, float maxValue)
    {
        if (slider == null) return;
        slider.maxValue = maxValue;
        slider.value = currentValue;
    }

    private void OnMainTrainClicked()
    {
        mainTrainButtonObj.SetActive(false);
        buttonContainer.SetActive(false);
        skillChoiceContainer.SetActive(true);
        timerContainer.SetActive(false);
    }

    private void PromptTraining(SkillType skill)
    {
        selectedSkillToTrain = skill;
        float currentSkillLevel = 0f;
        string skillName = "";

        switch (skill)
        {
            case SkillType.Management: currentSkillLevel = myEmployee.managementSkill; skillName = "Management"; break;
            case SkillType.Production: currentSkillLevel = myEmployee.productionSkill; skillName = "Production"; break;
            case SkillType.CustomerCare: currentSkillLevel = myEmployee.marketingCustomerSkill; skillName = "Cust. Care"; break;
            case SkillType.Sales: currentSkillLevel = myEmployee.marketingSalesSkill; skillName = "Sales"; break;
        }

        currentTrainingCost = Mathf.RoundToInt(10f + (currentSkillLevel * 10f));
        string message = $"Train {myEmployee.employeeName} in {skillName} for ${currentTrainingCost}?";

        if (ConfirmationManager.Instance != null)
        {
            ConfirmationManager.Instance.ShowConfirmation(
                message, 
                OnConfirmTrainingClicked, 
                null                     
            );
        }
    }

    private void OnConfirmTrainingClicked()
    {
        if (GameManager.Instance.money >= currentTrainingCost)
        {
            GameManager.Instance.AddMoney(-currentTrainingCost);
            StartSpecificTraining(selectedSkillToTrain);
        }
        else
        {
            if (ErrorManager.Instance != null)
            {
                ErrorManager.Instance.ShowErrorAtCursor("ERROR: Not enough money to train!");
            }
        }
    }


    private void StartSpecificTraining(SkillType skillToTrain)
    {
        myEmployee.isTraining = true;
        myEmployee.currentlyTrainingSkill = skillToTrain;
        myEmployee.trainingTimer = myEmployee.CalculateTrainingTime(skillToTrain); 

        UpdateVisualState();
    }

    private void UpdateVisualState()
    {
        if (myEmployee.isTraining)
        {
            mainTrainButtonObj.SetActive(false);
            buttonContainer.SetActive(false);
            skillChoiceContainer.SetActive(false);
            timerContainer.SetActive(true);
            UpdateVisualTimer();
        }
        else
        {
            mainTrainButtonObj.SetActive(true);
            buttonContainer.SetActive(true);
            skillChoiceContainer.SetActive(false);
            timerContainer.SetActive(false);
        }
    }

    private void UpdateVisualTimer()
    {
        if (timerText == null) return;
        int minutes = Mathf.FloorToInt(myEmployee.trainingTimer / 60F);
        int seconds = Mathf.FloorToInt(myEmployee.trainingTimer - minutes * 60);
        timerText.text = string.Format("{0:00}:{1:00}", minutes, seconds);

        if (myEmployee.trainingTimer <= 0)
        {
            SetupSlider(managementBar, myEmployee.managementSkill, myEmployee.managementPotential);
            SetupSlider(productionBar, myEmployee.productionSkill, myEmployee.productionPotential);
            SetupSlider(marketingCustomerBar, myEmployee.marketingCustomerSkill, myEmployee.marketingCustomerPotential);
            SetupSlider(marketingSalesBar, myEmployee.marketingSalesSkill, myEmployee.marketingSalesPotential);
            UpdateVisualState(); 
        }
    }

    private void OnFireClicked()
    {
        if (myDepartment != null && myEmployee != null)
        {
            myDepartment.DismissEmployee(myEmployee);
            if (DepartmentPanelManager.Instance != null)
            {
                DepartmentPanelManager.Instance.RefreshList();
            }
        }
    }
}