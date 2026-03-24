using UnityEngine;
using TMPro;

public class ConstructionPopup : MonoBehaviour
{
    [Header("Elemente UI din Pop-up")]
    public TMP_InputField nameInputField; 
    public TextMeshProUGUI costText;      
    public TextMeshProUGUI titleText;     
    public GameObject windowObject;       

    private DepartmentTypeSO pendingType;
    private float currentCalculatedCost;
    private System.Action<string, DepartmentTypeSO, float> confirmAction;

    void Start() { CloseWindow(); }

    public void OpenPopup(DepartmentTypeSO type, float actualCost, System.Action<string, DepartmentTypeSO, float> onConfirm)
    {
        pendingType = type;
        currentCalculatedCost = actualCost;
        confirmAction = onConfirm;

        nameInputField.text = type.defaultName; 
        titleText.text = "SETUP " + type.defaultName.ToUpper();
        costText.text = "$" + actualCost.ToString("F0"); 

        windowObject.SetActive(true);
        windowObject.transform.SetAsLastSibling();
    }

    public void OnBuildButtonPressed()
    {
        if (confirmAction != null)
        {
            confirmAction.Invoke(nameInputField.text, pendingType, currentCalculatedCost);
        }
        CloseWindow();
    }

    public void OnCancelButtonPressed() { CloseWindow(); }
    void CloseWindow() { windowObject.SetActive(false); }
}