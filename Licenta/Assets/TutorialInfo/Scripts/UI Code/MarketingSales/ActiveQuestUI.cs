using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

public class ActiveQuestUI : MonoBehaviour
{
    [Header("Main UI")]
    public TextMeshProUGUI titleText;
    public TextMeshProUGUI productText;
    
    [Header("Requirement Rows")]
    public RequirementRowUI[] requirementSlots; 

    [Header("Pagination UI")]
    public Button prevButton;
    public Button nextButton;
    public TextMeshProUGUI pageText;

    [Header("Action UI")]
    public Button deliverButton;

    public OrderData myOrder;
    private int currentChapterIndex = 0;

    [Header("Pop-up Reference")]
    public GameObject deliveryPopupPrefab;

    public void SetupQuest(OrderData order)
    {
        if (order == null) { Debug.LogError("EROARE: The order is NULL!"); return; }
        myOrder = order;
        
        requirementSlots = GetComponentsInChildren<RequirementRowUI>(true);
        
        if (requirementSlots.Length == 0)
        {
            Debug.LogError("GRAVE ERROR: No RequirementRowUI components found in children! Please add them in the Inspector.");
        }

        if (titleText != null) titleText.text = $"{myOrder.clientName} Order";
        if (productText != null) productText.text = $"Product: {myOrder.productType}";
        
        if (deliverButton != null)
        {
            deliverButton.interactable = false; 
            deliverButton.onClick.RemoveAllListeners();
            deliverButton.onClick.AddListener(DeliverOrder);
        }

        currentChapterIndex = 0;

        if (prevButton != null)
        {
            prevButton.onClick.RemoveAllListeners();
            prevButton.onClick.AddListener(PagePrevious);
        }
        if (nextButton != null)
        {
            nextButton.onClick.RemoveAllListeners();
            nextButton.onClick.AddListener(PageNext);
        }

        LoadChapter(currentChapterIndex);
    }

    public void RefreshVisuals()
    {
        LoadChapter(currentChapterIndex);
    }

    private void LoadChapter(int index)
    {
        if (myOrder.chapters == null || myOrder.chapters.Count == 0) return;
        QuestChapter chapter = myOrder.chapters[index];
        
        for (int i = 0; i < requirementSlots.Length; i++)
        {
            if (i < chapter.requirements.Count)
            {
                requirementSlots[i].gameObject.SetActive(true);
                requirementSlots[i].Setup(chapter.requirements[i]);
            }
            else
            {
                requirementSlots[i].gameObject.SetActive(false);
            }
        }
        
        if (pageText != null) pageText.text = $"{index + 1} / {myOrder.chapters.Count}";
        if (prevButton != null) prevButton.interactable = (index > 0);
        if (nextButton != null) nextButton.interactable = (index < myOrder.chapters.Count - 1);

        CheckIfDeliverable();
        
        Canvas.ForceUpdateCanvases();
        LayoutRebuilder.ForceRebuildLayoutImmediate(GetComponent<RectTransform>());
        if (transform.parent != null)
        {
            LayoutRebuilder.ForceRebuildLayoutImmediate(transform.parent.GetComponent<RectTransform>());
        }
    }

    private void PageNext()
    {
        if (currentChapterIndex < myOrder.chapters.Count - 1)
        {
            currentChapterIndex++;
            LoadChapter(currentChapterIndex);
        }
    }

    private void PagePrevious()
    {
        if (currentChapterIndex > 0)
        {
            currentChapterIndex--;
            LoadChapter(currentChapterIndex);
        }
    }

    public void CheckIfDeliverable()
    {
        if (myOrder == null) return; 
        bool canDeliver = myOrder.productionDone && myOrder.drawingDone;
        if (deliverButton != null) 
        {
            deliverButton.interactable = canDeliver;
            Image btnImage = deliverButton.GetComponent<Image>();
            if (btnImage != null)
            {
                if (canDeliver)
                    btnImage.color = new Color(0.2f, 0.8f, 0.2f);
                else
                    btnImage.color = Color.gray; 
            }
        }
    }

    private void DeliverOrder()
    {
        int totalRequirements = 0;
        int metRequirements = 0;
        foreach (var chapter in myOrder.chapters)
        {
            foreach (var req in chapter.requirements)
            {
                totalRequirements++;
                if (req.isCompleted) metRequirements++;
            }
        }
        float completionRate = totalRequirements > 0 ? (float)metRequirements / totalRequirements : 1f;
        float finalMoney = myOrder.moneyReward * completionRate;
        float finalRp = myOrder.rpReward * completionRate;
        float finalPop = myOrder.popReward * completionRate;

        if (GameManager.Instance != null)
        {
            if (completionRate < 0.5f)
            {
                Debug.LogWarning("You delivered a wrong order, the client is mad!");
                GameManager.Instance.AddRPC(myOrder.clientName, -15f); 
            }
            GameManager.Instance.AddMoney(finalMoney);
            GameManager.Instance.AddRP((int)finalRp);
            GameManager.Instance.AddPOP((int)finalPop);
        }

        if (deliveryPopupPrefab != null)
        {
            Canvas mainCanvas = FindAnyObjectByType<Canvas>();
            if (mainCanvas != null)
            {
                GameObject popup = Instantiate(deliveryPopupPrefab, mainCanvas.transform);
                popup.GetComponent<DeliveryPopupUI>().SetupPopup(myOrder.clientName, completionRate, finalMoney, finalRp, finalPop);
            }
        }
        ProductionWorkspace pw = FindAnyObjectByType<ProductionWorkspace>();
        if (pw != null) pw.ResetWorkspace();
        if (transform.parent != null)
        {
            if (transform.parent.childCount <= 1) 
            {
                transform.parent.gameObject.SetActive(false);
            }
        }

        Debug.Log($"Project delivered to {myOrder.clientName}. Accuracy: {completionRate*100}%. Money earned: ${finalMoney}");
        Destroy(gameObject);
    }
}