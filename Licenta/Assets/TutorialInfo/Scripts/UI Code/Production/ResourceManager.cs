using UnityEngine;
using System.Collections.Generic;

[System.Serializable]
public class ResourceDelivery
{
    public int orderID;
    public string resourceType; 
    public int amount;
    public int arrivalDay;
}

public class ResourceManager : MonoBehaviour
{
    public static ResourceManager Instance;

    [Header("Storage Limits (Max Capacity)")]
    public int maxPhysicalStorage = 750; 
    public int maxCloudStorage = 750;    

    [Header("Physical Storage (Units)")]
    public int paperRolls = 100;
    public int vinylRolls = 50;
    public int canvasRolls = 20;
    public float inkLiters = 150f;

    [Header("Digital Storage (GB)")]
    public int cloudBandwidthGB = 200;

    [Header("Economy Settings")]
    public float feePaper = 0.25f;
    public float feeVinyl = 0.50f;
    public float feeCanvas = 1.00f;
    public float feeInk = 1.00f;
    public float feeCloud = 0.00f; 
    private int lastBilledDay = 0;

    [Header("Pending Deliveries")]
    public List<ResourceDelivery> pendingDeliveries = new List<ResourceDelivery>();
    
    private int currentOrderCounter = 1; 

    private void Awake()
    {
        if (Instance == null) Instance = this;
    }

    private void Start()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnDayPassedEvent += HandleDayPassed;
        }
    }

    public int GetTotalPhysicalItems()
    {
        int total = paperRolls + vinylRolls + canvasRolls + (int)inkLiters;
        foreach (var delivery in pendingDeliveries)
        {
            if (delivery.resourceType != "Bandwidth") total += delivery.amount;
        }
        return total;
    }

    public int GetTotalCloudItems()
    {
        int total = cloudBandwidthGB;
        foreach (var delivery in pendingDeliveries)
        {
            if (delivery.resourceType == "Bandwidth") total += delivery.amount;
        }
        return total;
    }

    private void HandleDayPassed(int newDay)
    {
        for (int i = pendingDeliveries.Count - 1; i >= 0; i--)
        {
            if (newDay >= pendingDeliveries[i].arrivalDay)
            {
                ReceiveDelivery(pendingDeliveries[i]);
                pendingDeliveries.RemoveAt(i); 
            }
        }

        if (newDay > 0 && newDay % 30 == 0 && newDay != lastBilledDay)
        {
            lastBilledDay = newDay;
            PayMonthlyStorage();
        }
    }

    public void OrderResource(string type, int amount)
    {
        int currentDay = DayNightCycle.Instance != null ? DayNightCycle.Instance.currentDay : 1;
        int daysToDeliver = Random.Range(3, 6); 
        
        pendingDeliveries.Add(new ResourceDelivery {
            orderID = currentOrderCounter++, 
            resourceType = type,
            amount = amount,
            arrivalDay = currentDay + daysToDeliver
        });
    }

    public void PlaceBulkOrder(List<ResourceDelivery> basket)
    {
        int currentDay = DayNightCycle.Instance != null ? DayNightCycle.Instance.currentDay : 1;
        int daysToDeliver = Random.Range(3, 6); 
        int thisOrderID = currentOrderCounter++; 

        foreach (var item in basket)
        {
            item.arrivalDay = currentDay + daysToDeliver;
            item.orderID = thisOrderID;
            pendingDeliveries.Add(item);
        }
        
        Debug.Log($"[Logistics] Bulk order placed! Arriving on day {currentDay + daysToDeliver}.");
    }

    private void ReceiveDelivery(ResourceDelivery delivery)
    {
        switch (delivery.resourceType)
        {
            case "Paper": paperRolls += delivery.amount; break;
            case "Vinyl": vinylRolls += delivery.amount; break;
            case "Canvas": canvasRolls += delivery.amount; break;
            case "Ink": inkLiters += delivery.amount; break;
            case "Bandwidth": cloudBandwidthGB += delivery.amount; break;
        }
        
        ProductionWorkspace pw = FindAnyObjectByType<ProductionWorkspace>();
        if (pw != null) pw.UpdateResourceHUD();
    }

    private void PayMonthlyStorage()
    {
        float totalCost = (paperRolls * feePaper) + 
                          (vinylRolls * feeVinyl) + 
                          (canvasRolls * feeCanvas) + 
                          (inkLiters * feeInk) +
                          (cloudBandwidthGB * feeCloud);

        if (GameManager.Instance != null)
        {
            GameManager.Instance.AddMoney(-totalCost);
        }
    }

    public bool TryConsumeResources(string materialName, int quantity, out string errorMessage)
    {
        errorMessage = "";
        float inkNeeded = 0f;
        bool isDigital = materialName.Contains("Social") || materialName.Contains("Website") || 
                         materialName.Contains("Billboard") || materialName.Contains("App");

        if (!isDigital) inkNeeded = quantity * 0.1f;

        if (materialName.Contains("Paper") || materialName.Contains("Cardstock"))
        {
            if (paperRolls < quantity || inkLiters < inkNeeded) 
            {
                errorMessage = "NOT ENOUGH RESOURCES!\n\nYou are missing:\n";
                if (paperRolls < quantity) errorMessage += $"- {quantity - paperRolls} Paper Rolls\n";
                if (inkLiters < inkNeeded) errorMessage += $"- {inkNeeded - inkLiters:F1} Liters of Ink";
                return false;
            }
            paperRolls -= quantity;
            inkLiters -= inkNeeded;
        }
        else if (materialName.Contains("Banner") || materialName.Contains("Window"))
        {
            if (vinylRolls < quantity || inkLiters < inkNeeded) 
            {
                errorMessage = "NOT ENOUGH RESOURCES!\n\nYou are missing:\n";
                if (vinylRolls < quantity) errorMessage += $"- {quantity - vinylRolls} Vinyl Rolls\n";
                if (inkLiters < inkNeeded) errorMessage += $"- {inkNeeded - inkLiters:F1} Liters of Ink";
                return false;
            }
            vinylRolls -= quantity;
            inkLiters -= inkNeeded;
        }
        else if (materialName.Contains("Canvas"))
        {
            if (canvasRolls < quantity || inkLiters < inkNeeded) 
            {
                errorMessage = "NOT ENOUGH RESOURCES!\n\nYou are missing:\n";
                if (canvasRolls < quantity) errorMessage += $"- {quantity - canvasRolls} Canvas Rolls\n";
                if (inkLiters < inkNeeded) errorMessage += $"- {inkNeeded - inkLiters:F1} Liters of Ink";
                return false;
            }
            canvasRolls -= quantity;
            inkLiters -= inkNeeded;
        }
        else if (isDigital)
        {
            if (cloudBandwidthGB < quantity) 
            {
                errorMessage = $"NOT ENOUGH RESOURCES!\n\nYou are missing:\n- {quantity - cloudBandwidthGB} GB Cloud Bandwidth";
                return false;
            }
            cloudBandwidthGB -= quantity;
        }

        return true;
    }

    private void OnDestroy()
    {
        if (GameManager.Instance != null)
            GameManager.Instance.OnDayPassedEvent -= HandleDayPassed;
    }
}