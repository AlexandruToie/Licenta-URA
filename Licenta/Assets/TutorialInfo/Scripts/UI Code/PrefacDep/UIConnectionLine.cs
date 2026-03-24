using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems; 

public class UIConnectionLine : MonoBehaviour, IPointerClickHandler 
{
    public UIDepartmentNode bossNode; 
    public UIDepartmentNode subNode;  

    private NodeConnector startConnector;
    private NodeConnector endConnector;
    private float lineWidth;

    private RectTransform[] segments;
    private RectTransform rectTransform;

    void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
    }

    public void SetConnection(NodeConnector start, NodeConnector end, Transform container, float width)
    {
        startConnector = start;
        endConnector = end;
        lineWidth = width;
        bossNode = start.myNode;
        subNode = end.myNode;

        rectTransform.pivot = new Vector2(0.5f, 0.5f);
        rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
        rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
        rectTransform.anchoredPosition3D = Vector3.zero;
        rectTransform.localRotation = Quaternion.identity;

        if (TryGetComponent<Image>(out Image img)) img.enabled = false;

        segments = new RectTransform[3];
        for (int i = 0; i < 3; i++)
        {
            GameObject segObj = new GameObject($"Seg_{i}");
            segObj.transform.SetParent(transform, false);
            
            Image segImg = segObj.AddComponent<Image>();
            segImg.color = img != null ? img.color : Color.white;
            
            segImg.raycastTarget = true; 

            segments[i] = segObj.GetComponent<RectTransform>();
            segments[i].pivot = new Vector2(0.5f, 0.5f);
            segments[i].anchorMin = new Vector2(0.5f, 0.5f);
            segments[i].anchorMax = new Vector2(0.5f, 0.5f);
        }
        UIDepartmentNode.UpdateAllDepartmentsUI();
    }

    void Update()
    {
        if (startConnector == null || endConnector == null)
        {
            Destroy(gameObject); return;
        }

        Vector2 startPos = rectTransform.InverseTransformPoint(startConnector.transform.position);
        Vector2 endPos = rectTransform.InverseTransformPoint(endConnector.transform.position);

        DrawOrthogonal(startPos, endPos);
    }

    private void DrawOrthogonal(Vector2 start, Vector2 end)
    {
        float midY = (start.y + end.y) / 2f;
        PositionSegment(segments[0], start, new Vector2(start.x, midY));
        PositionSegment(segments[1], new Vector2(start.x, midY), new Vector2(end.x, midY));
        PositionSegment(segments[2], new Vector2(end.x, midY), end);
    }

    private void PositionSegment(RectTransform segment, Vector2 start, Vector2 end)
    {
        Vector2 direction = end - start;
        float distance = direction.magnitude;
        segment.sizeDelta = new Vector2(distance + lineWidth, lineWidth);
        segment.anchoredPosition3D = new Vector3(start.x + direction.x / 2f, start.y + direction.y / 2f, 0f);
        segment.localRotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg);
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData.button == PointerEventData.InputButton.Right)
        {
            if (ConfirmationManager.Instance != null)
            {
                ConfirmationManager.Instance.ShowConfirmation(
                    "Are you sure you want to delete this connection?", 
                    () => DeleteConnection()
                );
            }
            else
            {
                Debug.LogWarning("You don't have a ConfirmationManager in the scene! Deleting connection without confirmation.");
                DeleteConnection(); 
            }
        }
    }

    private void DeleteConnection()
    {
        if (bossNode != null && subNode != null)
        {
            bossNode.mySubordinates.Remove(subNode);
            subNode.myBoss = null;
        }
        UIDepartmentNode.UpdateAllDepartmentsUI();
        Destroy(gameObject);
    }
}