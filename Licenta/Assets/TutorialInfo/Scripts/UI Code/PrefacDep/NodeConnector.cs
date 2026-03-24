using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class NodeConnector : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler, IPointerEnterHandler, IPointerExitHandler
{
    public UIDepartmentNode myNode;
    public bool isInput; 

    private void Awake()
    {
        if (myNode == null) myNode = GetComponentInParent<UIDepartmentNode>();
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (ConnectionManager.Instance != null)
            ConnectionManager.Instance.hoveredConnector = this;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (ConnectionManager.Instance != null && ConnectionManager.Instance.hoveredConnector == this)
            ConnectionManager.Instance.hoveredConnector = null;
    }


    public void OnBeginDrag(PointerEventData eventData)
    {
        if (isInput) return; 

        ConnectionManager.Instance.ShowAllNodePoints();
        ConnectionManager.Instance.StartUIConnection(this);
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (isInput) return;
        ConnectionManager.Instance.UpdateUIConnection(eventData.position);
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (isInput) return;

        ConnectionManager.Instance.HideAllNodePoints();
        
        if (myNode != null) myNode.SetPointsActive(true);

        if (ConnectionManager.Instance.hoveredConnector != null)
        {
            ConnectionManager.Instance.FinishUIConnection(ConnectionManager.Instance.hoveredConnector);
            return;
        }

        ConnectionManager.Instance.CancelConnection();
    }
}