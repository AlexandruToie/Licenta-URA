using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class HUDController : MonoBehaviour
{
    public static HUDController Instance;

    [Header("--- MONEY ---")]
    public TextMeshProUGUI moneyText;

    [Header("--- TIME & DATE ---")]
    public TextMeshProUGUI clockText;
    public TextMeshProUGUI dayText;

    [Header("--- REPUTATION (Stars) ---")]
    public Image[] stars;
    public Color activeStarColor = new Color(1f, 0.8f, 0f); 
    public Color inactiveStarColor = new Color(0.3f, 0.3f, 0.3f, 0.5f); 

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    public void UpdateMoneyUI(double amount)
    {
        if (moneyText != null) moneyText.text = "$ " + amount.ToString("N0");
    }

    public void UpdateTimeUI(int hour, int minute, int day)
    {
        if (clockText != null) clockText.text = $"{hour:D2}:{minute:D2}";
        if (dayText != null) dayText.text = $"DAY {day:D2}";
    }

    public void UpdateReputationUI(float currentRep)
    {
        int starCount = Mathf.FloorToInt(currentRep / 20f);

        if (stars != null)
        {
            for (int i = 0; i < stars.Length; i++)
            {
                if (i < starCount) stars[i].color = activeStarColor;
                else stars[i].color = inactiveStarColor;
            }
        }
    }
}