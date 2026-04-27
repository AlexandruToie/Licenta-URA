using UnityEngine;
using System.Collections.Generic;

public class OrderManager : MonoBehaviour
{
    public static OrderManager Instance;

    [Header("Order Settings")]
    public GameObject orderPrefab;     
    
    [Header("Quest UI")]
    public GameObject questTrackerPrefab;

    [Header("UI Containers")]
    public Transform pendingContainer;   
    public Transform acceptedContainer;  
    public Transform questsContainer;    

    private string[] possibleClients = { "TechCorp", "DuraBuild", "SoftWorks", "MediaGlow", "Nexus" };
    
    private string[] commonStyles = { "Corporate", "Aggressive Retail", "Elegant", "Minimalist", "Playful" };
    private string[] commonColors = { "Red", "Blue", "Green", "Yellow", "Black & White" };
    
    private string[] printMaterials = { "Glossy Paper", "Matte Cardstock", "Outdoor Banner", "Window Graphic", "Canvas" };
    private string[] printSizes = { "A5 Flyer", "A3 Poster", "Citylight (1x2m)", "Billboard (4x3m)", "Roll-up" };
    private string[] printQualities = { "Draft (72 DPI)", "Standard Print", "High-Res (300 DPI)" };
    
    private string[] digitalMaterials = { "Social Media Post", "Website Ad", "Digital Billboard", "Mobile App Ad" };
    private string[] digitalSizes = { "1080x1080 (Square)", "1080x1920 (Story)", "1920x1080 (FHD)", "300x250 (Web)" };
    private string[] digitalQualities = { "Fast Load (Web)", "Standard Display", "4K UHD Ready" };

    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnDayPassedEvent += HandleNewDay;
        }
        GenerateOrders(1);
    }

    private void HandleNewDay(int newDay)
    {
        ClearPendingOrders();  
        int ordersToGenerate = 1;
        if (newDay > 3)
        {
            float currentPop = GameManager.Instance.popularity;
            if (currentPop >= 100f) ordersToGenerate = 3;
            else if (currentPop >= 50f) ordersToGenerate = Random.Range(2, 4);
            else ordersToGenerate = Random.Range(1, 4); 
        }
        GenerateOrders(ordersToGenerate);
    }

    private void ClearPendingOrders()
    {
        foreach (Transform child in pendingContainer)
        {
            OrderUIElement orderUI = child.GetComponent<OrderUIElement>();
            if (orderUI != null && orderUI.CurrentState == OrderState.PendingOffer) Destroy(child.gameObject);
        }
    }

    private void GenerateOrders(int amount)
    {
        float currentRp = GameManager.Instance.reputation;
        float moneyMultiplier = 1f + (currentRp / 100f * 0.25f);

        int currentDay = 1;
        if (DayNightCycle.Instance != null)
        {
            currentDay = DayNightCycle.Instance.currentDay;
        }
        
        for (int i = 0; i < amount; i++)
        {
            string randomClient = possibleClients[Random.Range(0, possibleClients.Length)];
            if (GameManager.Instance.GetRPC(randomClient) <= 0) continue; 
            
            int generatedQuantity = 10;
            if (currentDay <= 5) generatedQuantity = Random.Range(5, 15);
            else if (currentDay <= 15) generatedQuantity = Random.Range(15, 50);    
            else if (currentDay <= 30) generatedQuantity = Random.Range(50, 200);   
            else generatedQuantity = Random.Range(200, 1500);

            float calculatedTimeLimit = Random.Range(2f, 4f) + (generatedQuantity / 500f);
            
            float basePrice = 50f + (generatedQuantity * 2.5f); 
            float finalMoneyReward = basePrice * moneyMultiplier;
            
            OrderData newOrderData = new OrderData
            {
                clientName = randomClient,
                productType = "Custom Project",
                moneyReward = finalMoneyReward,
                rpReward = Random.Range(5, 12),
                popReward = Random.Range(2, 6),
                timeLimitDays = calculatedTimeLimit,
                targetQuantity = generatedQuantity,
                chapters = GenerateRandomChapters(generatedQuantity)
            };
            
            GameObject newOrderObj = Instantiate(orderPrefab, pendingContainer);
            newOrderObj.GetComponent<OrderUIElement>().SetupOrder(newOrderData);
        }
    }

    private List<QuestChapter> GenerateRandomChapters(int orderQuantity)
    {
        List<QuestChapter> generatedChapters = new List<QuestChapter>();
        bool isPrintCampaign = Random.value > 0.5f;
        
        List<RequirementType> allTypes = new List<RequirementType> 
        { 
            RequirementType.Material, 
            RequirementType.Size, 
            RequirementType.Template, 
            RequirementType.Quality,
            RequirementType.Color,
            RequirementType.Quantity 
        };

        QuestChapter currentChapter = new QuestChapter();

        foreach (RequirementType type in allTypes)
        {
            QuestRequirement req = CreateRequirementForType(type, isPrintCampaign, orderQuantity);
            currentChapter.requirements.Add(req);
            
            if (currentChapter.requirements.Count == 3)
            {
                generatedChapters.Add(currentChapter);
                currentChapter = new QuestChapter(); 
            }
        }
        if (currentChapter.requirements.Count > 0)
        {
            generatedChapters.Add(currentChapter);
        }

        return generatedChapters;
    }

    private QuestRequirement CreateRequirementForType(RequirementType type, bool isPrint, int orderQuantity)
    {
        QuestRequirement req = new QuestRequirement { type = type, isCompleted = false };
        bool isAny = Random.value < 0.05f;

        switch (type)
        {
            case RequirementType.Material:
                string[] matArray = isPrint ? printMaterials : digitalMaterials;
                req.targetValue = isAny ? "Any" : matArray[Random.Range(0, matArray.Length)];
                req.description = $"Media: {req.targetValue}";
                break;
                
            case RequirementType.Size:
                string[] sizeArray = isPrint ? printSizes : digitalSizes;
                req.targetValue = isAny ? "Any" : sizeArray[Random.Range(0, sizeArray.Length)];
                req.description = $"Size: {req.targetValue}";
                break;
                
            case RequirementType.Quality:
                string[] qualArray = isPrint ? printQualities : digitalQualities;
                req.targetValue = isAny ? "Any" : qualArray[Random.Range(0, qualArray.Length)];
                req.description = $"Resolution: {req.targetValue}";
                break;

            case RequirementType.Template:
                req.targetValue = isAny ? "Any" : commonStyles[Random.Range(0, commonStyles.Length)];
                req.description = $"Style: {req.targetValue}";
                break;
                
            case RequirementType.Color:
                req.targetValue = isAny ? "Any" : commonColors[Random.Range(0, commonColors.Length)];
                req.description = $"Dominant Color: {req.targetValue}";
                break;
                
            case RequirementType.Quantity:
                req.targetValue = orderQuantity.ToString();
                req.description = $"Required Units: {req.targetValue}";
                break;
        }

        return req;
    }
    
    public void MoveOrderToAccepted(GameObject orderObj)
    {
        orderObj.transform.SetParent(acceptedContainer, false);
    }
    
    public void StartActiveQuest(OrderData orderData)
    {
        if (questsContainer != null && !questsContainer.gameObject.activeInHierarchy)
        {
            questsContainer.gameObject.SetActive(true);
        }
        GameObject newQuestObj = Instantiate(questTrackerPrefab, questsContainer);
        newQuestObj.GetComponent<ActiveQuestUI>().SetupQuest(orderData);
    }
    
    void OnDestroy()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnDayPassedEvent -= HandleNewDay;
        }
    }
}