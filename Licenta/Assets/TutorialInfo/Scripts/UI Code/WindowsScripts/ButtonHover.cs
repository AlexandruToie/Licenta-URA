using UnityEngine;
using UnityEngine.EventSystems;
using TMPro;

public class ButtonHover : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [Header("What button is this?")]
    public string buttonName;

    [Header("UI References")]
    public GameObject tooltipPanel;
    public TextMeshProUGUI tooltipText;

    [Header("Settings")]
    public Vector2 offset = new Vector2(15f, 15f);
    private static RectTransform tooltipRect;
    private bool isHovering = false;

    private void Start()
    {
        if (tooltipPanel != null && tooltipRect == null)
        {
            tooltipRect = tooltipPanel.GetComponent<RectTransform>();
        }
    }

    private void Update()
    {
        if (isHovering && tooltipPanel != null)
        {
            FollowMouse();
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        isHovering = true;
        
        if (tooltipPanel != null && tooltipText != null)
        {
            tooltipPanel.SetActive(true);
            tooltipText.text = buttonName;
        }
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        isHovering = false;
        if (tooltipPanel != null)
        {
            tooltipPanel.SetActive(false);
        }
    }

    void FollowMouse()
    {
        if (tooltipRect == null) return; //We make sure tooltipRect is assigned

        Vector2 mousePos = Input.mousePosition;

        float pivotX = (mousePos.x > Screen.width / 2) ? 1f : 0f; // X pivot based on mouse position
        
        tooltipRect.pivot = new Vector2(pivotX, 0f); // Y pivot is always 0 (bottom)

        float finalOffsetX = (pivotX == 0) ? 15f : -15f; // Adjust X offset based on pivot
        
        tooltipRect.position = new Vector3(mousePos.x + finalOffsetX, mousePos.y + offset.y, 0f); // Set position with offset
    }
}