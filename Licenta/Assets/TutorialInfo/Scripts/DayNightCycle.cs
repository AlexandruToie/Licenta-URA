using UnityEngine;

public class DayNightCycle : MonoBehaviour
{
    public static DayNightCycle Instance;

    [Header("Time Settings")]
    [Tooltip("The duration of a day")]
    public float dayDuration = 60f; 

    [Range(0, 24)]
    public float timeOfDay = 8f; 

    public int currentDay = 1;
    public float TotalHours { get; private set; } 

    [Header("Sun References")]
    public Light sunLight;

    [Header("Lighting Atmosphere")]
    public Gradient sunColor;
    public Gradient ambientColor;
    public Gradient fogColor;
    public AnimationCurve sunIntensity;

    [Header("Sun Movement")]
    public float sunTilt = -30f;
    private float updateTimer = 0f;
    private float updateInterval = 0.05f;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    private void Update()
    {
        AdvanceTime();
        
        UpdateHUD();

        updateTimer += Time.deltaTime;
        if (updateTimer > updateInterval)
        {
            UpdateLighting();
            updateTimer = 0f;
        }
    }

    void AdvanceTime()
    {
        float hoursPassed = (Time.deltaTime / dayDuration) * 24f;
        timeOfDay += hoursPassed;
        TotalHours += hoursPassed;

        if (timeOfDay >= 24f)
        {
            timeOfDay -= 24f;
            currentDay++;  

            if (currentDay % 30 == 0)
            {
                if (EconomyManager.Instance != null)
                {
                    EconomyManager.Instance.PaySalaries();
                }
            }

            if (GameManager.Instance != null)
            {
                GameManager.Instance.AdvanceDay();
            }
        }
    }

    void UpdateHUD()
    {
        if (HUDController.Instance != null)
        {
            int hours = Mathf.FloorToInt(timeOfDay);
            int minutes = Mathf.FloorToInt((timeOfDay - hours) * 60f);
            HUDController.Instance.UpdateTimeUI(hours, minutes, currentDay);
        }
    }
    void UpdateLighting()
    {
        float alpha = timeOfDay / 24f; 
        float sunAngle = (alpha * 360f) - 90f; 

        sunLight.transform.rotation = Quaternion.Euler(sunAngle, sunTilt, 0);

        sunLight.color = sunColor.Evaluate(alpha);
        sunLight.intensity = sunIntensity.Evaluate(alpha);

        RenderSettings.ambientLight = ambientColor.Evaluate(alpha);
        RenderSettings.fogColor = fogColor.Evaluate(alpha);

        if (sunLight.intensity == 0 && sunLight.shadows != LightShadows.None)
            sunLight.shadows = LightShadows.None;
        else if (sunLight.intensity > 0 && sunLight.shadows == LightShadows.None)
            sunLight.shadows = LightShadows.Soft;
    }
    
    public void SkipToNextMorning()
    {
        float hoursToSkip = (24f - timeOfDay) + 8f;
        TotalHours += hoursToSkip; 

        timeOfDay = 8f; 
        currentDay++;
        
        if (currentDay % 30 == 0)
        {
            if (EconomyManager.Instance != null)
            {
                EconomyManager.Instance.PaySalaries();
            }
        }

        if (GameManager.Instance != null)
        {
            GameManager.Instance.AdvanceDay();
        }
    }
}