using UnityEngine;
using System;
using System.IO;
using System.Collections.Generic;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("Identity Settings")]
    public string companyName = "My Startup";
    public Texture2D companyLogo; 
    public Color companyColor = Color.white;

    [Header("Live Data Collection")]
    public double money = 1000.0;   
    [Range(0, 100)] public float reputation = 10f; 
    [Range(0, 100)] public float popularity = 50f; 
    public double totalSpendingPerMonth = 0; 
    public int totalEmployees = 1; 

    private Dictionary<string, float> clientRPC = new Dictionary<string, float>();

    [Header("Time and System")]
    public int currentDay = 1;
    private float timer1Sec = 0f;

    public event Action OnUIUpdate;
    public event Action<int> OnDayPassedEvent;

    private void Awake()
    {
        if (Instance != null && Instance != this) 
        { 
            Destroy(gameObject); 
            return; 
        }
        Instance = this;
        DontDestroyOnLoad(gameObject); 
    }

    private void Start()
    {
        if (companyLogo == null)
        {
            companyLogo = new Texture2D(64, 64);
            Color[] colors = new Color[64 * 64];
            for (int i = 0; i < colors.Length; i++) colors[i] = Color.white;
            companyLogo.SetPixels(colors);
            companyLogo.Apply();
        }

        UpdateAllUI();
    }

    private void Update()
    {
        timer1Sec += Time.deltaTime;
        if (timer1Sec >= 1f)
        {
            PassiveCalculations();
            timer1Sec = 0f;
        }
    }

    void PassiveCalculations()
    {
        OnUIUpdate?.Invoke();
    }

    public void UpdateAllUI()
    {
        if (HUDController.Instance != null)
        {
            HUDController.Instance.UpdateMoneyUI(money);
            HUDController.Instance.UpdateReputationUI(reputation);
            HUDController.Instance.UpdateTimeUI(8, 0, currentDay);
        }
        
        OnUIUpdate?.Invoke();
    }

    public void SetCompanyName(string newName)
    {
        if (string.IsNullOrWhiteSpace(newName)) return;
        companyName = newName;
        UpdateAllUI();
    }

    public void AddMoney(double amount)
    {
        money += amount;
        HUDController.Instance.UpdateMoneyUI(money);
    }
    
    public void AddMoney(float amount)
    {
        AddMoney((double)amount);
    }

    public void SetReputation(float value)
    {
        reputation = Mathf.Clamp(value, 0f, 100f);
        HUDController.Instance.UpdateReputationUI(reputation);
    }

    public void AddPOP(float amount)
    {
        popularity = Mathf.Clamp(popularity + amount, 0f, 100f); 
        Debug.Log($"[Marketing Secret] The popularity is now: {popularity}/100");
    }

    public void AddRPC(string clientName, float amount)
    {
        if (!clientRPC.ContainsKey(clientName))
        {
            clientRPC[clientName] = 50f;
        }
        
        clientRPC[clientName] = Mathf.Clamp(clientRPC[clientName] + amount, 0f, 100f);
        Debug.Log($"RPC for the client {clientName} had been updated to: {clientRPC[clientName]}");

        if (clientRPC[clientName] <= 0)
        {
            Debug.LogWarning($"The client {clientName} had put you on the black list!");
        }
    }

    public void AddRP(float amount)
    {
        reputation = Mathf.Clamp(reputation + amount, 0f, 100f);
        
        if (HUDController.Instance != null)
        {
            HUDController.Instance.UpdateReputationUI(reputation);
        }
        Debug.Log($"[GameManager] You gained reputation! Total: {reputation}");
    }

    public float GetRPC(string clientName)
    {
        if (!clientRPC.ContainsKey(clientName)) return 50f;
        return clientRPC[clientName];
    }

    public void AdvanceDay()
    {
        currentDay++;
        Debug.Log($"[GameManager] A new day begun: {currentDay}");
        UpdateAllUI();
        OnDayPassedEvent?.Invoke(currentDay); 
    }

    public void SaveLogoToPC()
    {
        if (companyLogo != null)
        {
            Texture2D tempTex = new Texture2D(companyLogo.width, companyLogo.height, TextureFormat.RGBA32, false);
            Graphics.CopyTexture(companyLogo, tempTex);
            
            byte[] bytes = tempTex.EncodeToPNG(); 

            string desktopPath = System.Environment.GetFolderPath(System.Environment.SpecialFolder.Desktop);
            string folderPath = Path.Combine(desktopPath, "TycoonLogos");

            if (!Directory.Exists(folderPath))
            {
                Directory.CreateDirectory(folderPath);
            }

            string fileName = $"Logo_{companyName}_{System.DateTime.Now:yyyy-MM-dd_HH-mm-ss}.png";
            string fullPath = Path.Combine(folderPath, fileName);

            File.WriteAllBytes(fullPath, bytes);
            Debug.Log($"Logo saved at: {fullPath}");

            Application.OpenURL("file://" + folderPath);
        }
        else
        {
            Debug.LogError("No logo to save!");
        }
    }
}