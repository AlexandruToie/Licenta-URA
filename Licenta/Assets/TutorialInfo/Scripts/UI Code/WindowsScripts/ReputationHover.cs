using UnityEngine;
using UnityEngine.EventSystems;
using TMPro;

public class SmartTooltip : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [Header("UI References")]
    public GameObject tooltipPanel;
    public TextMeshProUGUI tooltipText;

    [Header("Settings")]
    public Vector2 offset = new Vector2(15f, 15f);

    private bool isHovering = false;
    private RectTransform tooltipRect;

    private void Start()
    {

        if (tooltipPanel != null)
        {
            tooltipPanel.SetActive(false);
            tooltipRect = tooltipPanel.GetComponent<RectTransform>();
        }
    }

    private void Update()
    {
        if (isHovering && tooltipPanel != null)
        {
            UpdateLiveValue();
            FollowMouse();
        }
    }

    void UpdateLiveValue()
    {
        if (GameManager.Instance != null && tooltipText != null)
        {
            float currentRep = GameManager.Instance.reputation;
            tooltipText.text = $"Reputation: <color=#FFD700>{currentRep:0}</color> / 100";
        }
    }

    void FollowMouse() // Make tooltip follow mouse with smart pivot
    {
        if (tooltipRect == null) return;
        Vector2 mousePos = Input.mousePosition;
        float pivotX = (mousePos.x > Screen.width / 2) ? 1f : 0f;
        float pivotY = (mousePos.y > Screen.height / 2) ? 1f : 0f;
        tooltipRect.pivot = new Vector2(pivotX, pivotY);
        float finalOffsetX = (pivotX == 0) ? offset.x : -offset.x;
        float finalOffsetY = (pivotY == 0) ? offset.y : -offset.y;
        tooltipRect.position = new Vector3(mousePos.x + finalOffsetX, mousePos.y + finalOffsetY, 0f);
    }

    public void OnPointerEnter(PointerEventData eventData) // Show tooltip
    {
        isHovering = true;
        if (tooltipPanel != null) tooltipPanel.SetActive(true);
    }

    public void OnPointerExit(PointerEventData eventData) // Hide tooltip
    {
        isHovering = false;
        if (tooltipPanel != null) tooltipPanel.SetActive(false);
    }
}