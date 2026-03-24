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
    //Common Requirements
    private string[] commonStyles = { "Corporate", "Aggressive Retail", "Elegant", "Minimalist", "Playful" };
    private string[] commonColors = { "Red", "Blue", "Green", "Yellow", "Black & White" };
    //Physical Print Requirements
    private string[] printMaterials = { "Glossy Paper", "Matte Cardstock", "Outdoor Banner", "Window Graphic", "Canvas" };
    private string[] printSizes = { "A5 Flyer", "A3 Poster", "Citylight (1x2m)", "Billboard (4x3m)", "Roll-up" };
    private string[] printQualities = { "Draft (72 DPI)", "Standard Print", "High-Res (300 DPI)" };
    //Digital Ad Requirements
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
        int ordersToGenerate = (newDay <= 7) ? 1 : Random.Range(1, 4);
        GenerateOrders(ordersToGenerate);
    }

    private void ClearPendingOrders()
    {
        foreach (Transform child in pendingContainer)
        {
            OrderUIElement orderUI = child.GetComponent<OrderUIElement>();
            if (orderUI != null && orderUI.CurrentState == OrderState.PendingOffer)
            {
                Destroy(child.gameObject);
            }
        }
    }

    private void GenerateOrders(int amount)
    {
        for (int i = 0; i < amount; i++)
        {
            string randomClient = possibleClients[Random.Range(0, possibleClients.Length)];
            if (GameManager.Instance.GetRPC(randomClient) <= 0) continue; 

            OrderData newOrderData = new OrderData
            {
                clientName = randomClient,
                productType = "Custom Project",
                moneyReward = Random.Range(1000f, 5000f),
                rpReward = Random.Range(5, 15),
                popReward = Random.Range(2, 8),
                timeLimitDays = Random.Range(2f, 5f),
                chapters = GenerateRandomChapters() 
            };

            GameObject newOrderObj = Instantiate(orderPrefab, pendingContainer);
            newOrderObj.GetComponent<OrderUIElement>().SetupOrder(newOrderData);
        }
    }

    private List<QuestChapter> GenerateRandomChapters()
    {
        List<QuestChapter> generatedChapters = new List<QuestChapter>();
        bool isPrintCampaign = Random.value > 0.5f;
        List<RequirementType> allTypes = new List<RequirementType> 
        { 
            RequirementType.Material, 
            RequirementType.Size, 
            RequirementType.Template, 
            RequirementType.Quality,
            RequirementType.Color 
        };

        QuestChapter currentChapter = new QuestChapter();

        foreach (RequirementType type in allTypes)
        {
            QuestRequirement req = CreateRequirementForType(type, isPrintCampaign);
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

    private QuestRequirement CreateRequirementForType(RequirementType type, bool isPrint)
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