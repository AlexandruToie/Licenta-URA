using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class DeliveryPopupUI : MonoBehaviour
{
    [Header("UI References")]
    public TextMeshProUGUI titleText;
    public TextMeshProUGUI statsText;
    public Button claimButton;

    public void SetupPopup(string client, float accuracy, float money, float rp, float pop)
    {
        if (titleText != null)
        {
            if (accuracy >= 0.8f) 
                titleText.text = "EXCELLENT WORK!";
            else if (accuracy >= 0.5f) 
                titleText.text = "ORDER DELIVERED";
            else 
                titleText.text = "CLIENT DISAPPOINTED...";
        }
        if (statsText != null)
        {
            statsText.text = $"Client: <b>{client}</b>\n" +
                             $"Accuracy: <b>{Mathf.RoundToInt(accuracy * 100)}%</b>\n\n" +
                             $"<color=#55ff55>+ ${money:F2}</color>\n" +
                             $"<color=#55aaff>+ {(int)rp} RP</color>\n" +
                             $"<color=#ffaa55>+ {(int)pop} POP</color>";
        }
        if (claimButton != null)
        {
            claimButton.onClick.RemoveAllListeners();
            claimButton.onClick.AddListener(() => Destroy(gameObject));
        }
    }
}