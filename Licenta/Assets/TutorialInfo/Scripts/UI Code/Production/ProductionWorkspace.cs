using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

public class ProductionWorkspace : MonoBehaviour
{
    [Header("Dropdown References")]
    public TMP_Dropdown materialDropdown;
    public TMP_Dropdown sizeDropdown;
    public TMP_Dropdown templateDropdown;
    public TMP_Dropdown qualityDropdown;

    [Header("Quantity & Info References")]
    public TMP_InputField quantityInput;
    public TextMeshProUGUI costText;
    public TextMeshProUGUI timeText;

    [Header("Production Flow Elements")]
    public Button produceButton;
    public Slider progressBar; 
    public Button goToDrawingButton; 

    [Header("Drawing Phase")]
    public RawImage drawingPreview;      
    public Button openEditorBtn;         
    public GameObject fullScreenEditor;  
    public PixelArtCanvas pixelCanvas;   

    private OrderData currentActiveOrder;
    private float currentCalculatedCost = 0f;
    private float currentCalculatedTimeHours = 0f;
    private bool isProducing = false;
    private float productionEndHour = 0f;
    private float productionStartHour = 0f;
    private bool wasEditorActive = false;

    [Header("Resource HUD (Upper Bar)")]
    public TextMeshProUGUI hudPaperText;
    public TextMeshProUGUI hudVinylText;
    public TextMeshProUGUI hudCanvasText;
    public TextMeshProUGUI hudInkText;
    public TextMeshProUGUI hudBandwidthText;

    [Header("Window Navigation")]
    public GameObject warehouseWindow;
    public Button openWarehouseButton;

    void Start()
    {
        PopulateDropdowns();
        materialDropdown.onValueChanged.AddListener(delegate { OnDropdownChanged(); });
        sizeDropdown.onValueChanged.AddListener(delegate { OnDropdownChanged(); });
        templateDropdown.onValueChanged.AddListener(delegate { OnDropdownChanged(); });
        qualityDropdown.onValueChanged.AddListener(delegate { OnDropdownChanged(); });
        quantityInput.onValueChanged.AddListener(delegate { OnDropdownChanged(); });

        produceButton.onClick.AddListener(StartProduction);

        if (openEditorBtn != null) 
            openEditorBtn.onClick.AddListener(OpenDrawingEditor);
            
        if (goToDrawingButton != null)
            goToDrawingButton.onClick.AddListener(OpenDrawingEditor);

        UpdateCalculations();

        if (openWarehouseButton != null)
        {
            openWarehouseButton.onClick.AddListener(() => {
                if (warehouseWindow != null) warehouseWindow.SetActive(true);
                gameObject.SetActive(false);
            });
        }
    }

    public void ResetWorkspace()
    {
        if (materialDropdown != null) materialDropdown.value = 0;
        if (sizeDropdown != null) sizeDropdown.value = 0;
        if (templateDropdown != null) templateDropdown.value = 0;
        if (qualityDropdown != null) qualityDropdown.value = 0;
        if (quantityInput != null) quantityInput.text = "";
        if (progressBar != null) 
        {
            progressBar.value = 0f;
            progressBar.gameObject.SetActive(false);
        }
        if (goToDrawingButton != null) goToDrawingButton.gameObject.SetActive(false);
        if (produceButton != null) produceButton.gameObject.SetActive(false);
        if (drawingPreview != null)
        {
            drawingPreview.texture = null;
            drawingPreview.color = new Color(1, 1, 1, 0); 
        }
        if (pixelCanvas != null)
        {
            pixelCanvas.ClearCanvas(false); 
        }
        currentActiveOrder = null;
        isProducing = false;
        ToggleInputs(true);
        UpdateCalculations();
    }

    private void OnEnable()
    {
        UpdateResourceHUD();
        ActiveQuestUI activeQuest = FindAnyObjectByType<ActiveQuestUI>();
        if (activeQuest != null)
        {
            if (currentActiveOrder != activeQuest.myOrder)
            {
                ResetWorkspace(); 
                currentActiveOrder = activeQuest.myOrder; 
            }
            if (currentActiveOrder != null && currentActiveOrder.productionDone)
            {
                if(produceButton != null) produceButton.gameObject.SetActive(false);
                if(goToDrawingButton != null) goToDrawingButton.gameObject.SetActive(true);
                if(progressBar != null) progressBar.gameObject.SetActive(false);
            }
            else if (isProducing)
            {
                if(produceButton != null) produceButton.gameObject.SetActive(false);
                if(goToDrawingButton != null) goToDrawingButton.gameObject.SetActive(false);
                if(progressBar != null) progressBar.gameObject.SetActive(true);
            }
            else
            {
                if(produceButton != null) produceButton.gameObject.SetActive(true);
                if(goToDrawingButton != null) goToDrawingButton.gameObject.SetActive(false);
                if(progressBar != null) progressBar.gameObject.SetActive(false);
                UpdateCalculations();
            }
        }
        else
        {
            ResetWorkspace();
        }
        if (isProducing && DayNightCycle.Instance != null)
        {
            if (DayNightCycle.Instance.TotalHours >= productionEndHour)
            {
                FinishProduction();
            }
        }
        VerifyRequirements(); 
    }
    private void Update()
    {
        if (isProducing && DayNightCycle.Instance != null)
        {
            float currentHour = DayNightCycle.Instance.TotalHours;
            if (currentHour >= productionEndHour)
            {
                FinishProduction();
            }
            else
            {
                float elapsed = currentHour - productionStartHour;
                float duration = productionEndHour - productionStartHour;
                if (progressBar != null) progressBar.value = elapsed / duration;
            }
        }

        if (fullScreenEditor != null)
        {
            bool isEditorActive = fullScreenEditor.activeSelf;
            
            if (isEditorActive && Input.GetKeyDown(KeyCode.Escape))
            {
                fullScreenEditor.SetActive(false);
                isEditorActive = false;
            }

            if (wasEditorActive && !isEditorActive)
            {
                CloseDrawingEditorAndScan();
            }

            wasEditorActive = isEditorActive;
        }
    }

    public void UpdateResourceHUD()
    {
        if (ResourceManager.Instance == null) return;
        string labelColor = "<color=#a0a0a0>"; 
        string endColor = "</color>";

        if (hudPaperText != null) 
            hudPaperText.text = $"{labelColor}Paper:{endColor} <b>{ResourceManager.Instance.paperRolls}</b>";
        if (hudVinylText != null) 
            hudVinylText.text = $"{labelColor}Vinyl:{endColor} <b>{ResourceManager.Instance.vinylRolls}</b>";           
        if (hudCanvasText != null) 
            hudCanvasText.text = $"{labelColor}Canvas:{endColor} <b>{ResourceManager.Instance.canvasRolls}</b>";         
        if (hudInkText != null) 
            hudInkText.text = $"{labelColor}Ink:{endColor} <b>{ResourceManager.Instance.inkLiters:F0}L</b>";    
        if (hudBandwidthText != null) 
            hudBandwidthText.text = $"{labelColor}Cloud:{endColor} <b>{ResourceManager.Instance.cloudBandwidthGB}GB</b>";
    }

    private void OpenDrawingEditor()
    {
        if (currentActiveOrder == null || !currentActiveOrder.productionDone)
        {
            Debug.LogWarning("Production is not done!");
            return;
        }

        if (fullScreenEditor != null)
        {
            fullScreenEditor.SetActive(true);
            Time.timeScale = 0f;
            Debug.Log("Game on pause!");
            if (pixelCanvas != null) ScanDominantColor(pixelCanvas.GetTexture());
        }
    }

    private void CloseDrawingEditorAndScan()
    {
        Time.timeScale = 1f;
        Debug.Log("Resume game!");

        if (pixelCanvas != null)
        {
            Texture2D finalArt = pixelCanvas.GetTexture();
            if (drawingPreview != null) 
            {
                drawingPreview.texture = finalArt;
                drawingPreview.color = Color.white;
            }
            ScanDominantColor(finalArt);
        }
    }

    private void ScanDominantColor(Texture2D tex)
    {
        if (currentActiveOrder == null || tex == null) return;

        Color[] pixels = tex.GetPixels();
        int redCount = 0, blueCount = 0, greenCount = 0, yellowCount = 0, bwCount = 0;
        int totalColoredPixels = 0;

        foreach (Color c in pixels)
        {
            if (c.a < 0.1f) continue; 
            if (c.r > 0.95f && c.g > 0.95f && c.b > 0.95f) continue; 
            totalColoredPixels++;

            if (c.r > 0.5f && c.g < 0.4f && c.b < 0.4f) redCount++;
            else if (c.b > 0.5f && c.r < 0.4f && c.g < 0.4f) blueCount++;
            else if (c.g > 0.5f && c.r < 0.4f && c.b < 0.4f) greenCount++;
            else if (c.r > 0.5f && c.g > 0.5f && c.b < 0.4f) yellowCount++; 
            else if (c.r < 0.3f && c.g < 0.3f && c.b < 0.3f) bwCount++; 
        }

        string detectedColor = "None";
        float threshold = 0.15f; 

        if (totalColoredPixels > 50)
        {
            currentActiveOrder.drawingDone = true;
            if ((float)redCount / totalColoredPixels >= threshold) detectedColor = "Red";
            else if ((float)blueCount / totalColoredPixels >= threshold) detectedColor = "Blue";
            else if ((float)greenCount / totalColoredPixels >= threshold) detectedColor = "Green";
            else if ((float)yellowCount / totalColoredPixels >= threshold) detectedColor = "Yellow";
            else if ((float)bwCount / totalColoredPixels >= threshold) detectedColor = "Black & White";
        }
        else
        {
            currentActiveOrder.drawingDone = false;
        }

        foreach (var chapter in currentActiveOrder.chapters)
        {
            foreach (var req in chapter.requirements)
            {
                if (req.type == RequirementType.Color) req.isCompleted = (req.targetValue == detectedColor || req.targetValue == "Any");
            }
        }
        
        ActiveQuestUI questUI = FindAnyObjectByType<ActiveQuestUI>();
        if (questUI != null)
        {
            questUI.RefreshVisuals();
            questUI.CheckIfDeliverable(); 
        }
    }

    private void StartProduction()
    {
        if (GameManager.Instance == null || DayNightCycle.Instance == null || ResourceManager.Instance == null) return;

        int quantity = 0;
        int.TryParse(quantityInput.text, out quantity);
        string selectedMat = materialDropdown.options[materialDropdown.value].text;
        string errorMsg = "";
        
        if (!ResourceManager.Instance.TryConsumeResources(selectedMat, quantity, out errorMsg))
        {
            if (ErrorManager.Instance != null)
            {
                ErrorManager.Instance.ShowErrorAtCursor(errorMsg);
            }
            else
            {
                Debug.LogError("ErrorManager was not found. Message: " + errorMsg);
            }
            return; 
        }
        UpdateResourceHUD();
        GameManager.Instance.AddMoney(-currentCalculatedCost);
        productionStartHour = DayNightCycle.Instance.TotalHours;
        productionEndHour = productionStartHour + currentCalculatedTimeHours;
        isProducing = true;
        ToggleInputs(false);
        if (produceButton != null) produceButton.gameObject.SetActive(false);
        if (progressBar != null) progressBar.gameObject.SetActive(true);
    }

    private void FinishProduction()
    {
        isProducing = false;

        if (currentActiveOrder != null) currentActiveOrder.productionDone = true;

        if (progressBar != null) 
        {
            progressBar.value = 1f;
            progressBar.gameObject.SetActive(false);
        }

        if (goToDrawingButton != null) goToDrawingButton.gameObject.SetActive(true);
        if (produceButton != null) produceButton.gameObject.SetActive(false);

        FindAnyObjectByType<ActiveQuestUI>()?.CheckIfDeliverable();
        Debug.Log("<color=green>Production finished.</color>");
    }

    private void OnDropdownChanged()
    {
        UpdateCalculations();
        VerifyRequirements(); 
    }
    
    private void VerifyRequirements()
    {
        if (currentActiveOrder == null || currentActiveOrder.productionDone) return;

        string selectedMat = materialDropdown.options[materialDropdown.value].text;
        string selectedSize = sizeDropdown.options[sizeDropdown.value].text;
        string selectedTemplate = templateDropdown.options[templateDropdown.value].text;
        string selectedQuality = qualityDropdown.options[qualityDropdown.value].text;
        string selectedQuantity = quantityInput.text;

        foreach (var chapter in currentActiveOrder.chapters)
        {
            foreach (var req in chapter.requirements)
            {
                if (req.type == RequirementType.Material) req.isCompleted = (req.targetValue == selectedMat || req.targetValue == "Any");
                if (req.type == RequirementType.Size) req.isCompleted = (req.targetValue == selectedSize || req.targetValue == "Any");
                if (req.type == RequirementType.Template) req.isCompleted = (req.targetValue == selectedTemplate || req.targetValue == "Any");
                if (req.type == RequirementType.Quality) req.isCompleted = (req.targetValue == selectedQuality || req.targetValue == "Any");
                if (req.type == RequirementType.Quantity) req.isCompleted = (req.targetValue == selectedQuantity);
            }
        }
        FindAnyObjectByType<ActiveQuestUI>()?.RefreshVisuals();
    }

    private void PopulateDropdowns()
    {
        PopulateDropdown(materialDropdown, new List<string> { "Glossy Paper", "Matte Cardstock", "Outdoor Banner", "Window Graphic", "Canvas", "Social Media Post", "Website Ad", "Digital Billboard", "Mobile App Ad" });
        PopulateDropdown(sizeDropdown, new List<string> { "A5 Flyer", "A3 Poster", "Citylight (1x2m)", "Billboard (4x3m)", "Roll-up", "1080x1080 (Square)", "1080x1920 (Story)", "1920x1080 (FHD)", "300x250 (Web)" });
        PopulateDropdown(templateDropdown, new List<string> { "Corporate", "Aggressive Retail", "Elegant", "Minimalist", "Playful" });
        PopulateDropdown(qualityDropdown, new List<string> { "Draft (72 DPI)", "Standard Print", "High-Res (300 DPI)", "Fast Load (Web)", "Standard Display", "4K UHD Ready" });
    }

    private void PopulateDropdown(TMP_Dropdown dropdown, List<string> options)
    {
        if (dropdown == null) return;
        dropdown.ClearOptions();
        List<string> finalOptions = new List<string> { "-- Select --" };
        finalOptions.AddRange(options);
        dropdown.AddOptions(finalOptions);
    }

    private void ToggleInputs(bool state)
    {
        materialDropdown.interactable = state;
        sizeDropdown.interactable = state;
        templateDropdown.interactable = state;
        qualityDropdown.interactable = state;
        quantityInput.interactable = state;
    }

    private void UpdateCalculations()
    {
        if (materialDropdown == null || sizeDropdown == null || qualityDropdown == null || quantityInput == null) return;
        
        int quantity = 0;
        int.TryParse(quantityInput.text, out quantity);
        if (quantity > 1000000) quantity = 1000000; 

        float basePricePerUnit = 0f;
        float baseTimeMinutesPerUnit = 0f;
        string mat = materialDropdown.options[materialDropdown.value].text;

        switch (mat)
        {
            case "Glossy Paper": basePricePerUnit = 0.01f; baseTimeMinutesPerUnit = 0.1f; break;
            case "Matte Cardstock": basePricePerUnit = 0.03f; baseTimeMinutesPerUnit = 0.15f; break;
            case "Outdoor Banner": basePricePerUnit = 0.50f; baseTimeMinutesPerUnit = 8f; break;
            case "Window Graphic": basePricePerUnit = 0.25f; baseTimeMinutesPerUnit = 4f; break;
            case "Canvas": basePricePerUnit = 0.40f; baseTimeMinutesPerUnit = 5f; break;
            case "Social Media Post": basePricePerUnit = 0.10f; baseTimeMinutesPerUnit = 10f; break; 
            case "Website Ad": basePricePerUnit = 0.15f; baseTimeMinutesPerUnit = 15f; break;
            case "Digital Billboard": basePricePerUnit = 1.00f; baseTimeMinutesPerUnit = 60f; break;
            case "Mobile App Ad": basePricePerUnit = 0.10f; baseTimeMinutesPerUnit = 20f; break;
            default: basePricePerUnit = 0f; baseTimeMinutesPerUnit = 0f; break;
        }

        float sizeMultiplier = 1f;
        string size = sizeDropdown.options[sizeDropdown.value].text;
        if (size.Contains("A3") || size.Contains("FHD") || size.Contains("Story")) sizeMultiplier = 1.2f;
        else if (size.Contains("Citylight") || size.Contains("Roll-up")) sizeMultiplier = 1.5f;
        else if (size.Contains("Billboard")) sizeMultiplier = 2.0f;
        
        float qualityMultiplier = 1f;
        string quality = qualityDropdown.options[qualityDropdown.value].text;
        if (quality.Contains("High-Res") || quality.Contains("4K")) qualityMultiplier = 1.5f;
        else if (quality.Contains("Draft") || quality.Contains("Fast")) qualityMultiplier = 0.5f;

        currentCalculatedCost = (basePricePerUnit * sizeMultiplier * qualityMultiplier) * quantity;

        float totalMinutes = (baseTimeMinutesPerUnit * sizeMultiplier * qualityMultiplier) * quantity;
        currentCalculatedTimeHours = totalMinutes / 60f;

        if(costText != null) costText.text = $"Cost: <color=#ff4444>-${currentCalculatedCost:F2}</color>";
        
        if(timeText != null)
        {
            if (currentCalculatedTimeHours < 1f)
                timeText.text = $"Time: <color=#ffffaa>{totalMinutes:F0} mins</color>";
            else
                timeText.text = $"Time: <color=#ffffaa>{currentCalculatedTimeHours:F1} hours</color>";
        }

        if(produceButton != null)
        {
            produceButton.interactable = (GameManager.Instance != null && GameManager.Instance.money >= currentCalculatedCost);
        }
    }
}