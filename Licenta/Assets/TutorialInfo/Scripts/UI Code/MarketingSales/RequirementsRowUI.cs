using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class RequirementRowUI : MonoBehaviour
{
    [Header("References")]
    public Image statusIconImage;
    public TextMeshProUGUI descriptionText;
    
    [Header("Sprites Settings")]
    [Tooltip("The incomplete sprite")]
    public Sprite incompleteSprite;
    
    [Tooltip("The complete sprite")]
    public Sprite completeSprite;

    public void Setup(QuestRequirement req)
    {
        if (descriptionText != null)
        {
            descriptionText.text = req.description;
        }
        else
        {
            Debug.LogError($"[{gameObject.name}] EROARE:Description Text is empty!");
        }

        if (statusIconImage != null)
        {
            statusIconImage.color = Color.white;
        }

        UpdateStatus(req.isCompleted);
    }

    public void UpdateStatus(bool isCompleted)
    {
        if (statusIconImage == null) return;
        
        if (incompleteSprite == null || completeSprite == null)
        {
            Debug.LogWarning($"You dont have both sprites assigned for {gameObject.name}. Please assign them in the Inspector.");
            return;
        }
        
        if (isCompleted)
        {
            statusIconImage.sprite = completeSprite;
        }
        else
        {
            statusIconImage.sprite = incompleteSprite;
        }
    }
}