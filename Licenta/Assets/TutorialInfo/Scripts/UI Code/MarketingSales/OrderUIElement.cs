using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using System.Text;

public enum OrderState { PendingOffer, Accepted }
public enum RequirementType { Color, Material, Template, Quality, Code, Size, Quantity }

[System.Serializable]
public class QuestRequirement
{
    public RequirementType type;
    public string targetValue; 
    public string description; 
    public bool isCompleted;   
}

[System.Serializable]
public class QuestChapter
{
    public List<QuestRequirement> requirements = new List<QuestRequirement>();
}

[System.Serializable]
public class OrderData
{
    public string clientName;
    public string productType;
    public float moneyReward;
    public int rpReward;
    public int popReward;
    public float timeLimitDays; 
    public int targetQuantity;
    public List<QuestChapter> chapters = new List<QuestChapter>(); 
    public bool productionDone = false;
    public bool drawingDone = false;
}

public class OrderUIElement : MonoBehaviour
{
    [Header("Text References")]
    public TextMeshProUGUI clientNameText;
    public TextMeshProUGUI productTypeText;
    public TextMeshProUGUI specificationsText;
    public TextMeshProUGUI moneyGainText;
    public TextMeshProUGUI rpGainText;
    public TextMeshProUGUI popGainText;
    
    [Header("Timer References")]
    public TextMeshProUGUI timerText;
    public GameObject timerGameObject; 

    [Header("Button References")]
    public Button positiveButton; 
    public Button negativeButton; 
    public TextMeshProUGUI positiveBtnText;
    public TextMeshProUGUI negativeBtnText;

    [Header("Automation Options")]
    public GameObject automateContainer; 
    public Toggle automateToggle;        

    private OrderData currentOrder;
    public OrderState CurrentState { get; private set; } = OrderState.PendingOffer;

    private float expirationTotalHour; 
    private bool isTimerRunning = false;

    public void SetupOrder(OrderData orderInfo) 
    {
        currentOrder = orderInfo;

        clientNameText.text = $"Client: {currentOrder.clientName}";
        productTypeText.text = $"Product: {currentOrder.productType}";
        UpdatePreVisualSpecs();
        moneyGainText.text = $"+${currentOrder.moneyReward:F2}";
        rpGainText.text = $"+{currentOrder.rpReward} RP";
        popGainText.text = $"+{currentOrder.popReward} POP";

        SetState(OrderState.PendingOffer);
    }
    
    private void UpdatePreVisualSpecs()
    {
        if (specificationsText == null) return;

        if (currentOrder.chapters == null || currentOrder.chapters.Count == 0)
        {
            specificationsText.text = "Error: Order data is empty.";
            return;
        }
        string fSize = "Any", fTemplate = "Any", fColor = "Any", fMaterial = "Any", fQuality = "Any";
        
        foreach (var chapter in currentOrder.chapters)
        {
            foreach (var req in chapter.requirements)
            {
                if (req.type == RequirementType.Size) fSize = req.targetValue;
                if (req.type == RequirementType.Template) fTemplate = req.targetValue;
                if (req.type == RequirementType.Color) fColor = req.targetValue;
                if (req.type == RequirementType.Material) fMaterial = req.targetValue;
                if (req.type == RequirementType.Quality) fQuality = req.targetValue;
            }
        }
        StringBuilder brief = new StringBuilder("<b>Campaign Brief:</b> ");  
        brief.Append($"We need <b>{currentOrder.targetQuantity} units</b> of a <b>{fSize}</b> layout, using a <b>{fTemplate}</b> approach. ");
        brief.Append($"Produce it on <b>{fMaterial}</b> at <b>{fQuality}</b>. ");
        brief.Append($"Color profile required: <b>{fColor}</b>.");
        specificationsText.text = brief.ToString();
    }

    private void SetState(OrderState newState)
    {
        CurrentState = newState;
        
        positiveButton.onClick.RemoveAllListeners();
        negativeButton.onClick.RemoveAllListeners();

        switch (CurrentState)
        {
            case OrderState.PendingOffer:
                timerGameObject.SetActive(false); 
                isTimerRunning = false;
                if (automateContainer != null) automateContainer.SetActive(false);
                
                positiveBtnText.text = "Accept";
                negativeBtnText.text = "Reject";
                
                positiveButton.onClick.AddListener(OnAcceptOffer);
                negativeButton.onClick.AddListener(OnRejectOffer);
                break;

            case OrderState.Accepted:
                timerGameObject.SetActive(true); 
                if (DayNightCycle.Instance != null)
                {
                    expirationTotalHour = DayNightCycle.Instance.TotalHours + (currentOrder.timeLimitDays * 24f);
                }
                
                isTimerRunning = true;

                if (automateContainer != null) 
                {
                    bool hasUpgrade = false;
                    automateContainer.SetActive(hasUpgrade);
                    if (automateToggle != null)
                    {
                        automateToggle.isOn = false; 
                        automateToggle.interactable = hasUpgrade; 
                    }
                }
                
                positiveBtnText.text = "Start";
                negativeBtnText.text = "Cancel";

                positiveButton.onClick.AddListener(OnStartProduction);
                positiveButton.onClick.AddListener(OnCancelAcceptedOrder);
                break;
        }
    }

    void Update()
    {
        if (isTimerRunning && DayNightCycle.Instance != null)
        {
            float hoursLeft = expirationTotalHour - DayNightCycle.Instance.TotalHours;
            
            if (hoursLeft <= 0)
            {
                OnOrderExpired();
            }
            else
            {
                float daysLeft = hoursLeft / 24f;
                timerText.text = $"{daysLeft:F1} Days";
                timerText.color = daysLeft <= 1f ? Color.red : Color.black;
            }
        }
    }

    private void OnAcceptOffer()
    {
        SetState(OrderState.Accepted);
        if (OrderManager.Instance != null) OrderManager.Instance.MoveOrderToAccepted(gameObject);
    }

    private void OnRejectOffer()
    {
        if (GameManager.Instance != null) GameManager.Instance.AddRPC(currentOrder.clientName, -5f);
        Destroy(gameObject);
    }

    private void OnStartProduction()
    {
        isTimerRunning = false;
        bool isAutomated = (automateToggle != null && automateToggle.interactable && automateToggle.isOn);

        if (isAutomated)
        {
            Debug.Log($"Order sent to AUTOMATION!");
        }
        else
        {
            if (OrderManager.Instance != null) OrderManager.Instance.StartActiveQuest(currentOrder);
        }
        Destroy(gameObject); 
    }

    private void OnCancelAcceptedOrder()
    {
        isTimerRunning = false;
        ApplyPenalties();
        Destroy(gameObject);
    }

    private void OnOrderExpired()
    {
        isTimerRunning = false;
        ApplyPenalties();
        Destroy(gameObject);
    }

    private void ApplyPenalties()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.AddPOP(-currentOrder.popReward * 2f); 
            GameManager.Instance.AddRPC(currentOrder.clientName, -20f); 
        }
    }
}