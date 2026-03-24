using UnityEngine;
using UnityEngine.UI;

public class SalesWindowUIManager : MonoBehaviour
{
    [Header("Pages")]
    public GameObject pageOrders;
    public GameObject pageAcceptedOrders;
    public GameObject pageSelfPromote;

    [Header("Buttons")]
    public Button tabOrdersBtn;
    public Button tabAcceptedOrdersBtn;
    public Button tabSelfPromoteBtn;

    [Header("Design")]
    public Color activeTabColor = new Color(0.7f, 0.7f, 0.7f, 1f);
    public Color inactiveTabColor = Color.white;

    void Start()
    {
        if (tabOrdersBtn != null)
        {
            tabOrdersBtn.onClick.RemoveAllListeners();
            tabOrdersBtn.onClick.AddListener(ShowOrdersPage);
        }

        if (tabAcceptedOrdersBtn != null)
        {
            tabAcceptedOrdersBtn.onClick.RemoveAllListeners();
            tabAcceptedOrdersBtn.onClick.AddListener(ShowAcceptedOrdersPage);
        }
        if (tabSelfPromoteBtn != null)
        {
            tabSelfPromoteBtn.onClick.RemoveAllListeners();
            tabSelfPromoteBtn.onClick.AddListener(ShowSelfPromotePage);
        }
        ShowOrdersPage();
    }

    public void ShowOrdersPage()
    {
        pageOrders.SetActive(true);
        pageAcceptedOrders.SetActive(false);
        pageSelfPromote.SetActive(false);
        if (tabOrdersBtn != null) tabOrdersBtn.GetComponent<Image>().color = activeTabColor;
        if (tabAcceptedOrdersBtn != null) tabAcceptedOrdersBtn.GetComponent<Image>().color = inactiveTabColor;
        if (tabSelfPromoteBtn != null) tabSelfPromoteBtn.GetComponent<Image>().color = inactiveTabColor;
    }

    public void ShowAcceptedOrdersPage()
    {
        pageOrders.SetActive(false);
        pageAcceptedOrders.SetActive(true);
        pageSelfPromote.SetActive(false);
        if (tabOrdersBtn != null) tabOrdersBtn.GetComponent<Image>().color = inactiveTabColor;
        if (tabAcceptedOrdersBtn != null) tabAcceptedOrdersBtn.GetComponent<Image>().color = activeTabColor;
        if (tabSelfPromoteBtn != null) tabSelfPromoteBtn.GetComponent<Image>().color = inactiveTabColor;
        if (tabSelfPromoteBtn != null) tabSelfPromoteBtn.GetComponent<Image>().color = inactiveTabColor;
    }
    
    public void ShowSelfPromotePage()
    {
        pageOrders.SetActive(false);
        pageAcceptedOrders.SetActive(false);
        pageSelfPromote.SetActive(true);

        if (tabOrdersBtn != null) tabOrdersBtn.GetComponent<Image>().color = inactiveTabColor;
        if (tabAcceptedOrdersBtn != null) tabAcceptedOrdersBtn.GetComponent<Image>().color = inactiveTabColor;
        if (tabSelfPromoteBtn != null) tabSelfPromoteBtn.GetComponent<Image>().color = activeTabColor;
    }

}