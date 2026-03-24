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

    private OrderData myOrder;
    private int currentChapterIndex = 0;

    public void SetupQuest(OrderData order)
    {
        myOrder = order;
        titleText.text = $"{myOrder.clientName} Order";
        productText.text = $"Product: {myOrder.productType}";
        
        deliverButton.interactable = false; 
        currentChapterIndex = 0;

        prevButton.onClick.RemoveAllListeners();
        nextButton.onClick.RemoveAllListeners();
        deliverButton.onClick.RemoveAllListeners();

        prevButton.onClick.AddListener(PagePrevious);
        nextButton.onClick.AddListener(PageNext);
        deliverButton.onClick.AddListener(DeliverOrder);

        LoadChapter(currentChapterIndex);
    }

    private void LoadChapter(int index)
    {
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
        pageText.text = $"{index + 1} / {myOrder.chapters.Count}";
        prevButton.interactable = (index > 0);
        nextButton.interactable = (index < myOrder.chapters.Count - 1);

        CheckIfDeliverable();
        //Force UI to update to reflect the changes
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
        bool allDone = true;
        foreach (var chapter in myOrder.chapters)
        {
            foreach (var req in chapter.requirements)
            {
                if (!req.isCompleted) allDone = false;
            }
        }
        deliverButton.interactable = allDone;
    }

    private void DeliverOrder()
    {
        Debug.Log("Quest delivered.");
        
        if (GameManager.Instance != null)
        {
            GameManager.Instance.AddMoney(myOrder.moneyReward);
            GameManager.Instance.AddRP(myOrder.rpReward);
            GameManager.Instance.AddPOP(myOrder.popReward);
        }

        Destroy(gameObject);
    }
}