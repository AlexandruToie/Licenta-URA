using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class DiagramNavigation : MonoBehaviour, IDragHandler, IBeginDragHandler, IPointerDownHandler
{
    public float zoomSpeed = 0.1f;
    public float minZoom = 0.5f;
    public float maxZoom = 2.0f;
    private ScrollRect scrollRect;
    private RectTransform contentRect;
    private RectTransform viewportRect;

    void Awake()
    {
        scrollRect = GetComponent<ScrollRect>();
        contentRect = scrollRect.content;
        viewportRect = scrollRect.viewport;
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (DepartmentSelectionManager.Instance != null)
        {
            DepartmentSelectionManager.Instance.DeselectAll();
        }
    }

    void Update()
    {
        float scrollInput = Input.GetAxis("Mouse ScrollWheel");

        if (scrollInput != 0.0f)
        {
            ApplyZoom(scrollInput);
        }
    }

    void ApplyZoom(float scroll)
    {

        if (DepartmentPanelManager.Instance != null && DepartmentPanelManager.Instance.managePanel.activeInHierarchy)
        {
            return;
        }

        Vector3 currentScale = contentRect.localScale;
        float newScaleVal = Mathf.Clamp(currentScale.x + (scroll * zoomSpeed), minZoom, maxZoom);       
        float viewportWidth = viewportRect.rect.width;
        float viewportHeight = viewportRect.rect.height;
        float contentBaseWidth = contentRect.rect.width;
        float contentBaseHeight = contentRect.rect.height;
        float minScaleX = viewportWidth / contentBaseWidth;
        float minScaleY = viewportHeight / contentBaseHeight;
        float absoluteMinScale = Mathf.Max(minScaleX, minScaleY);
        if (newScaleVal < absoluteMinScale)
        {
            newScaleVal = absoluteMinScale;
        }
        contentRect.localScale = new Vector3(newScaleVal, newScaleVal, 1f);
        ClampPosition();
    }

    void ClampPosition()
    {
        float contentWidth = contentRect.rect.width * contentRect.localScale.x; // Adjust for current zoom on x-axis
        float contentHeight = contentRect.rect.height * contentRect.localScale.y; // Adjust for current zoom on y-axis
        float viewportWidth = viewportRect.rect.width; // Viewport size remains constant on x-axis
        float viewportHeight = viewportRect.rect.height; // Viewport size remains constant on y-axis
        float xLimit = (contentWidth - viewportWidth) / 2f; // Calculate limits based on zoomed content size on x-axis
        float yLimit = (contentHeight - viewportHeight) / 2f; // Calculate limits based on zoomed content size on y-axis
        if (xLimit < 0) xLimit = 0; // Prevent negative limits when content is smaller than viewport on x-axis
        if (yLimit < 0) yLimit = 0; // Prevent negative limits when content is smaller than viewport on y-axis
        Vector2 pos = contentRect.anchoredPosition; // Get current position
        pos.x = Mathf.Clamp(pos.x, -xLimit, xLimit); // Clamp x position within limits
        pos.y = Mathf.Clamp(pos.y, -yLimit, yLimit); // Clamp y position within limits

        contentRect.anchoredPosition = pos; // Apply clamped position to content
    }

    public void OnBeginDrag(PointerEventData eventData) { }

    public void OnDrag(PointerEventData eventData)
    {
        if (eventData.button == PointerEventData.InputButton.Right)
        {
            contentRect.anchoredPosition += eventData.delta;
            ClampPosition();
        }
    }
}