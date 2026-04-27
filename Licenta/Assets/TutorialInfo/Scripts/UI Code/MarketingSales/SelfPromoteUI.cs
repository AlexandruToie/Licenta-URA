using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

public class SelfPromoteUI : MonoBehaviour
{
    [Header("List Settings")]
    public Transform listContent; 
    public GameObject campaignPrefab; 
    
    [Header("Details Panel References")]
    public TextMeshProUGUI descriptionText;
    public TextMeshProUGUI costPerDayText;
    public TextMeshProUGUI totalCostText;
    
    [Header("Sliders")]
    public Slider daysSlider;
    public TextMeshProUGUI daysValueText;
    
    [Header("Action Buttons")]
    public Button launchButton;
    
    [Header("NEW: Status & Cooldown UI")]
    public TextMeshProUGUI currentPopStatusText; 
    public GameObject cooldownOverlay; 
    public TextMeshProUGUI cooldownText; 

    [Header("Bottom Bar")]
    public TextMeshProUGUI totalInvestmentText;

    [Header("Campaign Database")]
    public List<MarketingCampaignData> availableCampaigns;

    private MarketingCampaignData selectedCampaign;
    private int currentDaysChosen = 1;
    
    private bool isUnderCooldown = false;
    private int cooldownDaysLeft = 0;

    void Awake()
    {
        if (availableCampaigns == null || availableCampaigns.Count == 0)
        {
            availableCampaigns = new List<MarketingCampaignData>()
            {
                new MarketingCampaignData { campaignName = "Local Flyer Handout", description = "Hire students to distribute printed flyers. Quick, localized visibility.", baseCostPerDay = 10f, baseRpGainPerDay = 0f, basePopGainPerDay = 5f },
                new MarketingCampaignData { campaignName = "Paid Social Media Ads", description = "Boost agency awareness through targeted social campaigns.", baseCostPerDay = 30f, baseRpGainPerDay = 2f, basePopGainPerDay = 15f },
                new MarketingCampaignData { campaignName = "Downtown Billboard", description = "Display our agency logo on a large billboard near the business district.", baseCostPerDay = 75f, baseRpGainPerDay = 5f, basePopGainPerDay = 30f },
                new MarketingCampaignData { campaignName = "Local Radio Spot", description = "A 30-second commercial airing during morning drive time.", baseCostPerDay = 120f, baseRpGainPerDay = 10f, basePopGainPerDay = 50f },
                new MarketingCampaignData { campaignName = "TV Commercial (Prime)", description = "Produce and air a professional commercial during peak local TV viewership.", baseCostPerDay = 350f, baseRpGainPerDay = 30f, basePopGainPerDay = 150f }
            };
        }
    }

    void Start()
    {
        PopulateList();
        launchButton.interactable = false;
        if(cooldownOverlay != null) cooldownOverlay.SetActive(false);
        daysSlider.onValueChanged.AddListener(OnDaysSliderChanged);
        launchButton.onClick.AddListener(LaunchCampaign);
        OnDaysSliderChanged(daysSlider.value); 
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnUIUpdate += UpdatePopStatus;
            GameManager.Instance.OnDayPassedEvent += AdvanceCooldown;
        }
    }

    private void OnDestroy()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnUIUpdate -= UpdatePopStatus;
            GameManager.Instance.OnDayPassedEvent -= AdvanceCooldown;
        }
    }

    private void UpdatePopStatus()
    {
        if (currentPopStatusText != null && GameManager.Instance != null)
        {
            float currentPop = GameManager.Instance.popularity;
            string spawnRate = "Low";
            if (currentPop > 30) spawnRate = "Medium";
            if (currentPop > 70) spawnRate = "High!";
            currentPopStatusText.text = $"Current POP: {currentPop}/100\n<size=70%>Order Frequency: {spawnRate}</size>";
        }
    }

    private void AdvanceCooldown(int currentDay)
    {
        if (!isUnderCooldown) return;
        cooldownDaysLeft--;
        if (cooldownDaysLeft <= 0)
        {
            isUnderCooldown = false;
            cooldownOverlay.SetActive(false);
            launchButton.gameObject.SetActive(true);
        }
        else
        {
            if (cooldownText != null) cooldownText.text = $"ON COOLDOWN\n{cooldownDaysLeft} DAYS LEFT";
        }
    }

    private void PopulateList()
    {
        foreach (Transform child in listContent) Destroy(child.gameObject);
        foreach (var campaign in availableCampaigns)
        {
            GameObject newIdx = Instantiate(campaignPrefab, listContent);
            TextMeshProUGUI btnText = newIdx.GetComponentInChildren<TextMeshProUGUI>();
            if (btnText != null) btnText.text = campaign.campaignName;        
            newIdx.GetComponent<Button>().onClick.AddListener(() => SelectCampaign(campaign));
        }
    }

    public void SelectCampaign(MarketingCampaignData data)
    {
        selectedCampaign = data;
        descriptionText.text = $"<b>{data.campaignName}</b>\n{data.description}";
        costPerDayText.text = $"Daily Cost: ${data.baseCostPerDay:F2}";    
        UpdateTotalCalculation();
        bool needsUpgrade = (GameManager.Instance.popularity >= 100f && GameManager.Instance.currentMilestone < 3);
        if (needsUpgrade)
        {
            launchButton.interactable = false;
            launchButton.GetComponentInChildren<TextMeshProUGUI>().text = "UPGRADE REQUIRED";
        }
        else if (!isUnderCooldown) 
        {
            launchButton.interactable = true;
            launchButton.GetComponentInChildren<TextMeshProUGUI>().text = "LAUNCH CAMPAIGN";
        }
    }

    private void OnDaysSliderChanged(float value)
    {
        currentDaysChosen = (int)value;
        daysValueText.text = $"{currentDaysChosen} Days";
        UpdateTotalCalculation();
    }

    private void UpdateTotalCalculation()
    {
        if (selectedCampaign == null) return;       
        float totalCost = selectedCampaign.baseCostPerDay * currentDaysChosen;
        totalCostText.text = $"TOTAL COST: ${totalCost:F2}";        
        float totalRp = selectedCampaign.baseRpGainPerDay * currentDaysChosen;
        float totalPop = selectedCampaign.basePopGainPerDay * currentDaysChosen;  
        if (totalInvestmentText != null)
        {
            totalInvestmentText.text = $"Projected Return: +{totalRp} RP | +{totalPop} POP";
        }
    }

    private void LaunchCampaign()
    {
        if (selectedCampaign == null || isUnderCooldown) return;  
        if (GameManager.Instance.popularity >= 100f && GameManager.Instance.currentMilestone < 3)
        {
            Debug.LogWarning("You must upgrade your agency first!");
            return;
        } 
        float totalCost = selectedCampaign.baseCostPerDay * currentDaysChosen;
        if (GameManager.Instance != null && GameManager.Instance.money >= totalCost)
        {
            GameManager.Instance.AddMoney(-totalCost);          
            if (MarketingActiveManager.Instance != null)
            {
                MarketingActiveManager.Instance.StartNewCampaign(selectedCampaign, currentDaysChosen);
                isUnderCooldown = true;
                cooldownDaysLeft = currentDaysChosen;
                launchButton.gameObject.SetActive(false);
                cooldownOverlay.SetActive(true);
                if (cooldownText != null) cooldownText.text = $"ON COOLDOWN\n{cooldownDaysLeft} DAYS LEFT";               
                Debug.Log($"<color=green>Campaign launched!</color> Cooldown started for {cooldownDaysLeft} days."); 
            }
        }
        else
        {
            Debug.LogWarning("Not enough funds!");
        }
    }
}