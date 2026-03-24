using UnityEngine;
using System.Collections.Generic;

[System.Serializable]
public class ClientOrder
{
    public string clientName;
    public string productType;
    public int requiredTimeDays;
    public float productionCost;
    public float profit;
    public float reputationGain;
    public bool isAccepted;
}

public class MarketingManager : MonoBehaviour
{
    public static MarketingManager Instance;

    [Header("Global Reputation")]
    [Tooltip("RPC - It affects the quality of the orders you recive")]
    [Range(0, 100)] public float globalReputation = 10f;

    [Tooltip("POP - It affects the profit and the quality of the orders")]
    [Range(0, 100)] public float popularity = 0f;

    [Header("Orders System")]
    public List<ClientOrder> pendingOrders = new List<ClientOrder>();
    public ClientOrder activeOrder;

    void Awake()
    {
        if (Instance == null) Instance = this;
    }

    //Hard codded used only temporarry, it will be upgraded later
    public void GenerateRandomOrders()
    {
        pendingOrders.Clear();

        for (int i = 0; i < 3; i++)
        {
            ClientOrder newOrder = new ClientOrder
            {
                clientName = "Client " + Random.Range(100, 999),
                productType = "Banner Ad",
                requiredTimeDays = Random.Range(3, 10),
                productionCost = Random.Range(50, 150),
                profit = Random.Range(200, 500) + (popularity * 2),
                reputationGain = Random.Range(1f, 3f),
                isAccepted = false
            };
            
            pendingOrders.Add(newOrder);
        }
        
        Debug.Log("It was generated 3 orders");
    }

    public void BuyPromotion(float cost, float popGain)
    {
        if (GameManager.Instance.money >= cost)
        {
            GameManager.Instance.AddMoney(-cost);
            popularity += popGain;
            if (popularity > 100) popularity = 100;
            
            Debug.Log($"Promotion started! POP has increased to {popularity}");
        }
        else
        {
            if (ErrorManager.Instance != null)
                ErrorManager.Instance.ShowErrorAtCursor("ERROR: Not enough money for promotion!");
        }
    }
}