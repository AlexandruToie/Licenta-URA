using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class MilestoneController : MonoBehaviour
{
    [Header("Slider UI")]
    public Slider popularitySlider; 
    public TextMeshProUGUI popularityText; 
    public Image sliderFillImage; 

    [Header("Colors per Milestone")]
    public Color milestone1Color = new Color(1f, 0.8f, 0f); 
    public Color milestone2Color = new Color(0f, 0.8f, 1f); 
    public Color milestone3Color = new Color(0.8f, 0f, 1f); 

    [Header("Upgrade Popup UI")]
    public GameObject upgradePanel; 
    public Button upgradeButton;
    public Button closeButton;

    [Header("Upgrade Settings")]
    public double upgradeCost = 5000.0; 
    private bool hasSeenPopupToday = false;

    void Start()
    {
        if (upgradePanel != null) upgradePanel.SetActive(false);
        if (upgradeButton != null) upgradeButton.onClick.AddListener(OnUpgradeClicked);
        if (closeButton != null) closeButton.onClick.AddListener(OnCloseClicked);
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnUIUpdate += UpdateMilestoneUI;
            GameManager.Instance.OnDayPassedEvent += ResetPopupDaily; 
            UpdateMilestoneUI(); 
        }
    }
    private void OnDestroy()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnUIUpdate -= UpdateMilestoneUI;
            GameManager.Instance.OnDayPassedEvent -= ResetPopupDaily; 
        }
    }
    private void OnEnable()
    {
        CheckForPopup();
    }
    private void ResetPopupDaily(int newDay)
    {
        hasSeenPopupToday = false;
        if (gameObject.activeInHierarchy)
        {
            CheckForPopup();
        }
    }
    private void UpdateMilestoneUI()
    {
        if (GameManager.Instance == null) return;
        float currentPop = GameManager.Instance.popularity;
        int milestone = GameManager.Instance.currentMilestone;
        float totalSliderValue = ((milestone - 1) * 100f) + currentPop;
        if (popularitySlider != null) popularitySlider.value = totalSliderValue;
        if (sliderFillImage != null)
        {
            if (milestone == 1) sliderFillImage.color = milestone1Color;
            else if (milestone == 2) sliderFillImage.color = milestone2Color;
            else sliderFillImage.color = milestone3Color;
        }
        if (popularityText != null)
        {
            popularityText.text = $"Current Popularity: {currentPop}/100\n<size=70%>Milestone Level: {milestone}</size>";
        }
        CheckForPopup();
    }
    private void CheckForPopup()
    {
        if (GameManager.Instance == null) return;
        if (!gameObject.activeInHierarchy) return;
        float currentPop = GameManager.Instance.popularity;
        int milestone = GameManager.Instance.currentMilestone;
        if (currentPop >= 100f && milestone < 3 && !hasSeenPopupToday && !upgradePanel.activeSelf)
        {
            upgradePanel.SetActive(true);
            hasSeenPopupToday = true;
        }
    }
    private void OnUpgradeClicked()
    {
        if (GameManager.Instance.money >= upgradeCost)
        {
            GameManager.Instance.AddMoney(-upgradeCost);
            GameManager.Instance.currentMilestone++;
            GameManager.Instance.popularity = 0f; 
            
            upgradePanel.SetActive(false);
            hasSeenPopupToday = false;
            
            upgradeCost *= 2.5; 
            GameManager.Instance.UpdateAllUI();
        }
        else
        {
            if (ErrorManager.Instance != null)
            {
                ErrorManager.Instance.ShowErrorAtCursor("Not enough funds for Upgrade!");
            }
            else
            {
                Debug.LogWarning("Not enough money to upgrade!");
            }
        }
    }
    private void OnCloseClicked()
    {
        upgradePanel.SetActive(false);
    }
}