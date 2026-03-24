using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ConnectionManager : MonoBehaviour
{
    public static ConnectionManager Instance;

    [Header("Settings")]
    public RectTransform uiLinePrefab; 
    public Transform linesContainer;   
    public float lineWidth = 5f;       

    [HideInInspector]
    public NodeConnector hoveredConnector;

    private GameObject tempLineContainer; 
    private RectTransform[] tempSegments;
    private NodeConnector startingConnector;

    void Awake() { Instance = this; }

    public void ShowAllNodePoints()
    {
        UIDepartmentNode[] allNodes = FindObjectsByType<UIDepartmentNode>(FindObjectsSortMode.None);
        foreach (var node in allNodes) node.SetPointsActive(true);
    }

    public void HideAllNodePoints()
    {
        UIDepartmentNode[] allNodes = FindObjectsByType<UIDepartmentNode>(FindObjectsSortMode.None);
        foreach (var node in allNodes) node.SetPointsActive(false);
    }

    public void StartUIConnection(NodeConnector startPoint)
    {
        startingConnector = startPoint;

        tempLineContainer = new GameObject("TempLine");
        RectTransform tempRect = tempLineContainer.AddComponent<RectTransform>();
        tempRect.SetParent(linesContainer, false);
        tempRect.SetAsFirstSibling();

        tempRect.localScale = Vector3.one;
        tempRect.anchoredPosition3D = Vector3.zero;
        tempRect.localRotation = Quaternion.identity;

        tempSegments = new RectTransform[3];
        Color lineColor = uiLinePrefab.GetComponent<Image>() != null ? uiLinePrefab.GetComponent<Image>().color : Color.white;

        for (int i = 0; i < 3; i++)
        {
            GameObject seg = new GameObject($"TempSeg_{i}");
            seg.transform.SetParent(tempRect, false); 
            Image img = seg.AddComponent<Image>();
            img.color = lineColor;
            img.raycastTarget = false;

            tempSegments[i] = seg.GetComponent<RectTransform>();
            tempSegments[i].pivot = new Vector2(0.5f, 0.5f);
            tempSegments[i].anchorMin = new Vector2(0.5f, 0.5f);
            tempSegments[i].anchorMax = new Vector2(0.5f, 0.5f);
        }
    }

    public void UpdateUIConnection(Vector2 mouseScreenPos)
    {
        if (startingConnector == null || tempLineContainer == null) return;

        Canvas canvas = linesContainer.GetComponentInParent<Canvas>();
        Camera cam = canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;
        RectTransform tempRect = tempLineContainer.GetComponent<RectTransform>();

        Vector2 startLocalPos = tempRect.InverseTransformPoint(startingConnector.transform.position);
        Vector2 endLocalPos;

        if (hoveredConnector != null && hoveredConnector.isInput && hoveredConnector.myNode != startingConnector.myNode)
        {
            endLocalPos = tempRect.InverseTransformPoint(hoveredConnector.transform.position);
        }
        else
        {
            RectTransformUtility.ScreenPointToLocalPointInRectangle(tempRect, mouseScreenPos, cam, out endLocalPos);
        }

        float midY = (startLocalPos.y + endLocalPos.y) / 2f;
        PositionSegment(tempSegments[0], startLocalPos, new Vector2(startLocalPos.x, midY));
        PositionSegment(tempSegments[1], new Vector2(startLocalPos.x, midY), new Vector2(endLocalPos.x, midY));
        PositionSegment(tempSegments[2], new Vector2(endLocalPos.x, midY), endLocalPos);
    }

    public void FinishUIConnection(NodeConnector endPoint)
    {
        if (endPoint.myNode == startingConnector.myNode) { CancelConnection(); return; }
        if (!endPoint.isInput) { CancelConnection(); return; }
        
        if (endPoint.myNode.myType != null && endPoint.myNode.myType.isCEO) 
        { 
            ErrorManager.Instance.ShowErrorAtCursor("ERROR: This department cannot have a boss!");
            CancelConnection(); return; 
        }

        if (endPoint.myNode.myBoss != null) 
        { 
            ErrorManager.Instance.ShowErrorAtCursor("ATENTION: This department already has a boss!");
            CancelConnection(); return; 
        }

        startingConnector.myNode.mySubordinates.Add(endPoint.myNode);
        endPoint.myNode.myBoss = startingConnector.myNode;

        RectTransform permanentLine = Instantiate(uiLinePrefab, linesContainer);
        permanentLine.SetAsFirstSibling();
        
        permanentLine.localScale = Vector3.one;
        permanentLine.anchoredPosition3D = Vector3.zero;
        permanentLine.localRotation = Quaternion.identity;

        UIConnectionLine smartScript = permanentLine.GetComponent<UIConnectionLine>();
        smartScript.SetConnection(startingConnector, endPoint, linesContainer, lineWidth);

        CancelConnection();
    }

    public void CancelConnection()
    {
        if (tempLineContainer != null) Destroy(tempLineContainer);
        startingConnector = null;
    }

    private void PositionSegment(RectTransform segment, Vector2 start, Vector2 end)
    {
        Vector2 direction = end - start;
        float distance = direction.magnitude;
        segment.sizeDelta = new Vector2(distance + lineWidth, lineWidth);
        segment.anchoredPosition3D = new Vector3(start.x + direction.x / 2f, start.y + direction.y / 2f, 0f);
        segment.localRotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg);
    }
}