using UnityEngine;

public class DepartmentSelectionManager : MonoBehaviour
{
    public static DepartmentSelectionManager Instance;

    private UIDepartmentNode currentSelectedNode;

    void Awake()
    {
        Instance = this;
    }

    public void SelectNode(UIDepartmentNode node)
    {
        if (currentSelectedNode != null && currentSelectedNode != node)
        {
            currentSelectedNode.Deselect();
        }
        currentSelectedNode = node;
    }

    public void DeselectAll()
    {
        if (currentSelectedNode != null)
        {
            currentSelectedNode.Deselect();
            currentSelectedNode = null;
        }
    }
}