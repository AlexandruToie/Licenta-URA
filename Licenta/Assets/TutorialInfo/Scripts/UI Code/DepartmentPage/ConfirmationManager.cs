using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System; 

public class ConfirmationManager : MonoBehaviour
{
    public static ConfirmationManager Instance;

    [Header("UI References")]
    public GameObject confirmationPanel; 
    public TextMeshProUGUI messageText;  
    public Button yesButton;
    public Button noButton;

    private Action onConfirmAction;
    private Action onCancelAction; 

    void Awake()
    {
        Instance = this;
        if (confirmationPanel != null) confirmationPanel.SetActive(false);
    }

    public void ShowConfirmation(string message, Action onConfirm, Action onCancel = null)
    {
        messageText.text = message;
        onConfirmAction = onConfirm;
        onCancelAction = onCancel;
        
        yesButton.onClick.RemoveAllListeners();
        yesButton.onClick.AddListener(Confirm);
        
        noButton.onClick.RemoveAllListeners();
        noButton.onClick.AddListener(Cancel);

        confirmationPanel.SetActive(true);
        confirmationPanel.transform.SetAsLastSibling();
    }

    private void Confirm()
    {
        onConfirmAction?.Invoke();
        confirmationPanel.SetActive(false);
    }

    private void Cancel()
    {
        onCancelAction?.Invoke(); 
        confirmationPanel.SetActive(false);
    }
}