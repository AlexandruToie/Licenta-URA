using UnityEngine;

[System.Serializable]
public class MarketingCampaignData
{
    public string campaignName;
    [TextArea] public string description;
    public Sprite icon;
    
    [Header("Base Stats per Day")]
    public float baseCostPerDay;
    public float baseRpGainPerDay;
    public float basePopGainPerDay;
}