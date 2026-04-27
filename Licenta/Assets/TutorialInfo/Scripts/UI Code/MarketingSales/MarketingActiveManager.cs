using UnityEngine;
using System.Collections.Generic;

[System.Serializable]
public class ActiveCampaignInfo
{
    public MarketingCampaignData data;
    public int daysRemaining;
}

public class MarketingActiveManager : MonoBehaviour
{
    public static MarketingActiveManager Instance { get; private set; }
    public List<ActiveCampaignInfo> activeCampaigns = new List<ActiveCampaignInfo>();

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void Start()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnDayPassedEvent += ProcessDailyMarketing;
        }
    }

    private void OnDestroy()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnDayPassedEvent -= ProcessDailyMarketing;
        }
    }

    public void StartNewCampaign(MarketingCampaignData data, int duration)
    {
        ActiveCampaignInfo newCampaign = new ActiveCampaignInfo
        {
            data = data,
            daysRemaining = duration
        };
        activeCampaigns.Add(newCampaign);
    }
    private void ProcessDailyMarketing(int currentDay)
    {
        if (activeCampaigns.Count == 0) return;
        List<ActiveCampaignInfo> finishedCampaigns = new List<ActiveCampaignInfo>();

        foreach (var campaign in activeCampaigns)
        {
            GameManager.Instance.AddRP(campaign.data.baseRpGainPerDay);
            GameManager.Instance.AddPOP(campaign.data.basePopGainPerDay);

            campaign.daysRemaining--;

            if (campaign.daysRemaining <= 0)
            {
                finishedCampaigns.Add(campaign);
            }
        }
        foreach (var finished in finishedCampaigns)
        {
            activeCampaigns.Remove(finished);
            Debug.Log($"The marketing campaign '{finished.data.campaignName}' has ended!");
        }
    }
}