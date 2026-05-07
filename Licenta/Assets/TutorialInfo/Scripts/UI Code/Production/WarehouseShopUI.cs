using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

public class WarehouseShopUI : MonoBehaviour
{
    [Header("--- BASKET SYSTEM ---")]
    public TextMeshProUGUI basketTotalText; 
    public Button placeOrderButton;        
    private List<ResourceDelivery> currentBasket = new List<ResourceDelivery>();
    private float basketTotalCost = 0f;

    [Header("--- TOP INFO UI ---")]
    public TextMeshProUGUI totalFeeText;       
    public TextMeshProUGUI physicalStorageText; 
    public TextMeshProUGUI cloudStorageText;    

    [Header("Window Navigation")]
    public GameObject productionWindow;
    public Button closeButton; 

    [Header("--- PAPER ROLLS ---")]
    public TextMeshProUGUI paperInfoText;      
    public float paperPrice = 2.0f;
    public TMP_InputField paperInput;
    public TextMeshProUGUI paperCostPreview;
    public Button paperBuyBtn;

    [Header("--- VINYL ROLLS ---")]
    public TextMeshProUGUI vinylInfoText;    
    public float vinylPrice = 5.0f;
    public TMP_InputField vinylInput;
    public TextMeshProUGUI vinylCostPreview;
    public Button vinylBuyBtn;

    [Header("--- CANVAS ROLLS ---")]
    public TextMeshProUGUI canvasInfoText;    
    public float canvasPrice = 8.0f;
    public TMP_InputField canvasInput;
    public TextMeshProUGUI canvasCostPreview;
    public Button canvasBuyBtn;

    [Header("--- INK (Liters) ---")]
    public TextMeshProUGUI inkInfoText;     
    public float inkPrice = 1.0f;
    public TMP_InputField inkInput;
    public TextMeshProUGUI inkCostPreview;
    public Button inkBuyBtn;

    [Header("--- CLOUD BANDWIDTH (GB) ---")]
    public TextMeshProUGUI cloudInfoText;   
    public float cloudPrice = 0.5f;
    public TMP_InputField cloudInput;
    public TextMeshProUGUI cloudCostPreview;
    public Button cloudBuyBtn;

    private void Start()
    {
        if (paperBuyBtn) paperBuyBtn.onClick.AddListener(() => AddToBasket("Paper", paperInput, paperPrice));
        if (vinylBuyBtn) vinylBuyBtn.onClick.AddListener(() => AddToBasket("Vinyl", vinylInput, vinylPrice));
        if (canvasBuyBtn) canvasBuyBtn.onClick.AddListener(() => AddToBasket("Canvas", canvasInput, canvasPrice));
        if (inkBuyBtn) inkBuyBtn.onClick.AddListener(() => AddToBasket("Ink", inkInput, inkPrice));
        if (cloudBuyBtn) cloudBuyBtn.onClick.AddListener(() => AddToBasket("Bandwidth", cloudInput, cloudPrice));

        if (paperInput) paperInput.onValueChanged.AddListener(val => UpdatePreview(val, paperPrice, paperCostPreview));
        if (vinylInput) vinylInput.onValueChanged.AddListener(val => UpdatePreview(val, vinylPrice, vinylCostPreview));
        if (canvasInput) canvasInput.onValueChanged.AddListener(val => UpdatePreview(val, canvasPrice, canvasCostPreview));
        if (inkInput) inkInput.onValueChanged.AddListener(val => UpdatePreview(val, inkPrice, inkCostPreview));
        if (cloudInput) cloudInput.onValueChanged.AddListener(val => UpdatePreview(val, cloudPrice, cloudCostPreview));

        if (placeOrderButton) placeOrderButton.onClick.AddListener(FinalizeOrder);

        if (closeButton != null)
        {
            closeButton.onClick.AddListener(() => {
                if (productionWindow != null) productionWindow.SetActive(true);
                gameObject.SetActive(false);
            });
        }
    }

    private void UpdatePreview(string inputValue, float pricePerUnit, TextMeshProUGUI previewText)
    {
        if (previewText == null) return;

        if (int.TryParse(inputValue, out int amount) && amount > 0)
        {
            float totalCost = amount * pricePerUnit;
            previewText.text = $"Cost: ${totalCost:N2}";
            previewText.color = Color.white;
        }
        else
        {
            previewText.text = "Cost: $0.00";
            previewText.color = new Color(0.7f, 0.7f, 0.7f);
        }
    }

    private void AddToBasket(string resourceType, TMP_InputField inputField, float pricePerUnit)
    {
        if (inputField == null || string.IsNullOrEmpty(inputField.text) || ResourceManager.Instance == null) return;

        if (int.TryParse(inputField.text, out int amount) && amount > 0)
        {
            int currentTotal = (resourceType == "Bandwidth") ? ResourceManager.Instance.GetTotalCloudItems() : ResourceManager.Instance.GetTotalPhysicalItems();
            int max = (resourceType == "Bandwidth") ? ResourceManager.Instance.maxCloudStorage : ResourceManager.Instance.maxPhysicalStorage;
            
            foreach(var item in currentBasket) {
                if((resourceType == "Bandwidth" && item.resourceType == "Bandwidth") || (resourceType != "Bandwidth" && item.resourceType != "Bandwidth"))
                    currentTotal += item.amount;
            }

            if (currentTotal + amount > max) {
                if (ErrorManager.Instance != null) ErrorManager.Instance.ShowErrorAtCursor("NOT ENOUGH SPACE!");
                return;
            }

            var existingItem = currentBasket.Find(x => x.resourceType == resourceType);
            if (existingItem != null)
            {
                existingItem.amount += amount; 
            }
            else
            {
                currentBasket.Add(new ResourceDelivery { resourceType = resourceType, amount = amount });
            }

            basketTotalCost += amount * pricePerUnit;
            
            if (ErrorManager.Instance != null) ErrorManager.Instance.ShowErrorAtCursor($"<color=#00FF00>ADDED TO BASKET</color>\n{amount}x {resourceType}");
            
            inputField.text = ""; 
            UpdateBasketUI();
        }
    }

    private void FinalizeOrder()
    {
        if (currentBasket.Count == 0) return;

        if (GameManager.Instance != null && GameManager.Instance.money >= basketTotalCost)
        {
            GameManager.Instance.AddMoney(-basketTotalCost);
            ResourceManager.Instance.PlaceBulkOrder(new List<ResourceDelivery>(currentBasket));
            
            currentBasket.Clear();
            basketTotalCost = 0;
            UpdateBasketUI();
            if (ErrorManager.Instance != null) ErrorManager.Instance.ShowErrorAtCursor("<color=green>ORDER SENT TO SUPPLIER!</color>");
        }
        else
        {
            if (ErrorManager.Instance != null) ErrorManager.Instance.ShowErrorAtCursor("NOT ENOUGH MONEY!");
        }
    }

    private void UpdateBasketUI()
    {
        if (basketTotalText != null)
            basketTotalText.text = $"Total Order: <color=#55FF55>${basketTotalCost:F2}</color> ({currentBasket.Count} items)";
        
        if (placeOrderButton != null)
            placeOrderButton.interactable = currentBasket.Count > 0;
            
        UpdateStorageUI();
    }

    public void UpdateStorageUI()
    {
        if (ResourceManager.Instance == null) return;
        int currentPhys = ResourceManager.Instance.GetTotalPhysicalItems(); 
        int maxPhys = ResourceManager.Instance.maxPhysicalStorage;
        int currentCloud = ResourceManager.Instance.GetTotalCloudItems();
        int maxCloud = ResourceManager.Instance.maxCloudStorage;
        float fPaper = ResourceManager.Instance.feePaper;
        float fVinyl = ResourceManager.Instance.feeVinyl;
        float fCanvas = ResourceManager.Instance.feeCanvas;
        float fInk = ResourceManager.Instance.feeInk;
        float fCloud = ResourceManager.Instance.feeCloud;
        string physColorTag = GetCapacityColor(currentPhys, maxPhys);
        string cloudColorTag = GetCapacityColor(currentCloud, maxCloud);

        if (physicalStorageText != null) 
            physicalStorageText.text = $"Max Physical: {physColorTag}{currentPhys} / {maxPhys}</color>";

        if (cloudStorageText != null) 
            cloudStorageText.text = $"Max Cloud: {cloudColorTag}{currentCloud} / {maxCloud}</color>";
        
        int paper = ResourceManager.Instance.paperRolls;
        int vinyl = ResourceManager.Instance.vinylRolls;
        int canvas = ResourceManager.Instance.canvasRolls;
        float ink = ResourceManager.Instance.inkLiters;
        int cloud = ResourceManager.Instance.cloudBandwidthGB;

        float totalFee = (paper * fPaper) + (vinyl * fVinyl) + (canvas * fCanvas) + (ink * fInk) + (cloud * fCloud);

        if (totalFeeText != null)
            totalFeeText.text = $"Total Storage Fee: ${totalFee:F2} / month";
            
        if (paperInfoText != null)
            paperInfoText.text = $"<b>Paper Rolls</b> <size=80%><color=#aaaaaa>(${paperPrice:F2}/u)</color></size>\n<size=90%><color=#cccccc>In stock: {paper} | Tax: ${paper * fPaper:F2}/mo</color></size>";
        if (vinylInfoText != null)
            vinylInfoText.text = $"<b>Vinyl Rolls</b> <size=80%><color=#aaaaaa>(${vinylPrice:F2}/u)</color></size>\n<size=90%><color=#cccccc>In stock: {vinyl} | Tax: ${vinyl * fVinyl:F2}/mo</color></size>";
        if (canvasInfoText != null)
            canvasInfoText.text = $"<b>Canvas Rolls</b> <size=80%><color=#aaaaaa>(${canvasPrice:F2}/u)</color></size>\n<size=90%><color=#cccccc>In stock: {canvas} | Tax: ${canvas * fCanvas:F2}/mo</color></size>";
        if (inkInfoText != null)
            inkInfoText.text = $"<b>Ink (Liters)</b> <size=80%><color=#aaaaaa>(${inkPrice:F2}/L)</color></size>\n<size=90%><color=#cccccc>In stock: {ink:F0}L | Tax: ${ink * fInk:F2}/mo</color></size>";
        if (cloudInfoText != null)
            cloudInfoText.text = $"<b>Cloud Server</b> <size=80%><color=#aaaaaa>(${cloudPrice:F2}/GB)</color></size>\n<size=90%><color=#cccccc>In stock: {cloud}GB | Tax: $0.00/mo</color></size>";
    }

    private string GetCapacityColor(float current, float max)
    {
        if (max <= 0) return "<color=#FFFFFF>"; 
        
        float ratio = current / max;
        
        if (ratio >= 0.8f) return "<color=#FF5555>"; 
        if (ratio <= 0.2f) return "<color=#55FF55>"; 
        
        return "<color=#FFFFFF>"; 
    }

    private void OnEnable()
    {
        if (paperInput != null) paperInput.text = "";
        if (vinylInput != null) vinylInput.text = "";
        if (canvasInput != null) canvasInput.text = "";
        if (inkInput != null) inkInput.text = "";
        if (cloudInput != null) cloudInput.text = "";
        
        UpdatePreview("", paperPrice, paperCostPreview);
        UpdatePreview("", vinylPrice, vinylCostPreview);
        UpdatePreview("", canvasPrice, canvasCostPreview);
        UpdatePreview("", inkPrice, inkCostPreview);
        UpdatePreview("", cloudPrice, cloudCostPreview);

        currentBasket.Clear();
        basketTotalCost = 0;
        UpdateBasketUI();
    }
}