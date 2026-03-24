using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ErrorManager : MonoBehaviour
{
    public static ErrorManager Instance;
    
    [Header("Error container")]
    public Transform popupContainer; 

    private GameObject currentError; 

    void Awake() 
    { 
        Instance = this; 
    }

    public void ShowErrorAtCursor(string message)
    {
        if (currentError != null)
        {
            Destroy(currentError);
        }

        currentError = new GameObject("ErrorBlocker");
        currentError.transform.SetParent(popupContainer, false);
        currentError.transform.SetAsLastSibling(); 

        Image blockerImage = currentError.AddComponent<Image>();
        blockerImage.color = new Color(0f, 0f, 0f, 0f); 
        blockerImage.raycastTarget = true; 

        RectTransform blockerRect = currentError.GetComponent<RectTransform>();
        blockerRect.anchorMin = Vector2.zero;
        blockerRect.anchorMax = Vector2.one;  
        blockerRect.sizeDelta = Vector2.zero;
        blockerRect.anchoredPosition3D = Vector3.zero;

        GameObject boxObj = new GameObject("ErrorBox");
        boxObj.transform.SetParent(currentError.transform, false);

        Image boxImage = boxObj.AddComponent<Image>();
        boxImage.color = new Color(0.15f, 0.15f, 0.15f, 0.95f); 
        boxImage.raycastTarget = false;

        ContentSizeFitter fitter = boxObj.AddComponent<ContentSizeFitter>();
        fitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        HorizontalLayoutGroup layout = boxObj.AddComponent<HorizontalLayoutGroup>();
        layout.padding = new RectOffset(25, 25, 15, 15); 
        layout.childAlignment = TextAnchor.MiddleCenter;

        RectTransform boxRect = boxObj.GetComponent<RectTransform>();
        boxRect.anchorMin = new Vector2(0.5f, 0.5f);
        boxRect.anchorMax = new Vector2(0.5f, 0.5f);
        boxRect.pivot = new Vector2(0.5f, 0.5f);
        boxRect.anchoredPosition3D = Vector3.zero;

        GameObject textObj = new GameObject("Text");
        textObj.transform.SetParent(boxObj.transform, false);

        TextMeshProUGUI txt = textObj.AddComponent<TextMeshProUGUI>();
        txt.text = message;
        txt.color = new Color(1f, 0.3f, 0.3f); 
        txt.fontSize = 23; 
        txt.fontStyle = FontStyles.Bold;
        txt.alignment = TextAlignmentOptions.Center;
        txt.raycastTarget = false;

        Destroy(currentError, 2.5f);
    }
}