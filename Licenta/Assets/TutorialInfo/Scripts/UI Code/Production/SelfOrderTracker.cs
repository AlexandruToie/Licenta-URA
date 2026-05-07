using UnityEngine;
using TMPro;
using System.Collections;
using System.Linq;// The library used for grouping and ordering the deliveries by arrival day

public class SelfOrderTracker : MonoBehaviour
{
    [Header("UI References")]
    public Transform contentContainer; 
    public GameObject orderPrefab;     

    [Header("Animation Settings")]
    public RectTransform panelRect;    
    public float slideDuration = 0.3f; 
    public float slideOutDistance = 470f;
    
    private Vector2 visiblePos;
    private Vector2 hiddenPos;
    private Coroutine slideCoroutine;
    private bool isInitialized = false;

    private void Awake()
    {
        if (panelRect != null)
        {
            visiblePos = panelRect.anchoredPosition; 
            hiddenPos = new Vector2(visiblePos.x + slideOutDistance, visiblePos.y); 
            isInitialized = true;
        }
    }

    private void OnEnable()
    {
        RefreshList();
        
        if (GameManager.Instance != null)
            GameManager.Instance.OnDayPassedEvent += OnDayPassed;

        if (isInitialized && panelRect != null)
        {
            panelRect.anchoredPosition = hiddenPos;
            if (slideCoroutine != null) StopCoroutine(slideCoroutine);
            slideCoroutine = StartCoroutine(SlideTo(visiblePos, false));
        }
    }

    private void OnDisable()
    {
        if (GameManager.Instance != null)
            GameManager.Instance.OnDayPassedEvent -= OnDayPassed;
    }

    private void OnDayPassed(int newDay)
    {
        RefreshList();
    }

    public void CloseTracker()
    {
        if (gameObject.activeInHierarchy)
        {
            if (slideCoroutine != null) StopCoroutine(slideCoroutine);
            slideCoroutine = StartCoroutine(SlideTo(hiddenPos, true)); 
        }
    }

    private IEnumerator SlideTo(Vector2 targetPos, bool disableAfter)
    {
        float time = 0;
        Vector2 startPos = panelRect.anchoredPosition;

        while (time < slideDuration)
        {
            time += Time.deltaTime;
            float t = time / slideDuration;
            t = t * t * (3f - 2f * t); 
            panelRect.anchoredPosition = Vector2.Lerp(startPos, targetPos, t);
            yield return null;
        }

        panelRect.anchoredPosition = targetPos;

        if (disableAfter)
        {
            gameObject.SetActive(false);
        }
    }

    public void RefreshList()
    {
        if (ResourceManager.Instance == null || contentContainer == null || orderPrefab == null) return;

        foreach (Transform child in contentContainer)
        {
            Destroy(child.gameObject);
        }

        if (ResourceManager.Instance.pendingDeliveries.Count == 0)
        {
            GameObject emptyMsg = Instantiate(orderPrefab, contentContainer);
            emptyMsg.GetComponentInChildren<TextMeshProUGUI>().text = "<color=#aaaaaa>No pending deliveries...</color>";
            return;
        }

        var groupedOrders = ResourceManager.Instance.pendingDeliveries
            .GroupBy(d => d.orderID);

        int currentDay = DayNightCycle.Instance != null ? DayNightCycle.Instance.currentDay : 1;

        foreach (var group in groupedOrders)
        {
            int arrivalDay = group.First().arrivalDay; 
            int daysLeft = arrivalDay - currentDay;
            string timeText = daysLeft <= 0 ? "<color=green>Arriving Today!</color>" : $"Arriving in {daysLeft} days";

            string orderDetails = $"<b>Order #{group.Key} - {timeText}</b>\n<size=90%>";
            
            foreach (var item in group)
            {
                orderDetails += $"• {item.amount}x {item.resourceType}\n";
            }
            orderDetails += "</size>";

            GameObject orderCard = Instantiate(orderPrefab, contentContainer);
            orderCard.GetComponentInChildren<TextMeshProUGUI>().text = orderDetails;
        }
    }
}