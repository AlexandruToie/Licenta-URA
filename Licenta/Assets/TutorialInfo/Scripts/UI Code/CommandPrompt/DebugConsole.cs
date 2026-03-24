using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

public class DebugConsole : MonoBehaviour
{
    public static DebugConsole Instance; 

    public GameObject consolePanel;
    public TMP_InputField commandInput;
    public TextMeshProUGUI logText;
    public ScrollRect scrollRect;
    
    public TextMeshProUGUI suggestionText; 

    [Header("Control Settings")]
    public MonoBehaviour cameraScript;

    private bool isConsoleOpen = false;
    private CursorLockMode previousLockMode;
    private bool previousCursorVisible;

    private List<string> commandHistory = new List<string>();
    private int historyIndex = 0;
    
    private List<string> validCommands = new List<string>()
    {
        "/help",
        "/addmoney",
        "/nextday",
        "/setrep",
        "/speed",
        "/clear",
        "/quit"
    };

    private string currentSuggestion = "";

    private string colorCommand = "#FFD700";
    private string colorSystem = "#cccccc";
    private string colorError = "#FF4444";
    private string colorSuggestion = "#AAAAAA";

    private void Awake() // Singleton Pattern
    {
        if (Instance == null) { Instance = this; DontDestroyOnLoad(gameObject); }
        else { Destroy(gameObject); return; }
        
        consolePanel.SetActive(false);
    }

    private void Start() // Initialize
    {
        if (logText != null) logText.text = "--- Unity Console Initialized ---\n Type /help for a list of commands.\n";
        Application.logMessageReceived += HandleUnityLog;
        commandInput.onValueChanged.AddListener(OnInputChanged);
        if(suggestionText != null) suggestionText.text = "";
    }

    private void OnDestroy() // Cleanup
    {
        Application.logMessageReceived -= HandleUnityLog;
    }

    private void Update() // Main Update Loop
    {
        if (Input.GetKeyDown(KeyCode.BackQuote)) ToggleConsole();

        if (isConsoleOpen)
        {
            if (commandInput.isFocused && commandHistory.Count > 0) // Navigate History
            {
                if (Input.GetKeyDown(KeyCode.UpArrow)) NavigateHistory(-1);
                else if (Input.GetKeyDown(KeyCode.DownArrow)) NavigateHistory(1);
            }

            if (Input.GetKeyDown(KeyCode.Tab)) // Autocomplete Suggestion
            {
                if (!string.IsNullOrEmpty(currentSuggestion))
                {
                    commandInput.text = currentSuggestion; 
                    commandInput.caretPosition = commandInput.text.Length;
                    commandInput.ActivateInputField(); 
                }
            }

            if (Input.GetKeyDown(KeyCode.Return)) // Submit Command
            {
                if (!string.IsNullOrWhiteSpace(commandInput.text)) SubmitCommand(commandInput.text);
                commandInput.ActivateInputField();
                commandInput.Select();
            }
        }
    }

    void OnInputChanged(string input) // Handle Suggestions
    {
        if (suggestionText == null) return;
        
        if (string.IsNullOrWhiteSpace(input))
        {
            suggestionText.text = "";
            currentSuggestion = "";
            return;
        }

        string match = validCommands.FirstOrDefault(c => c.StartsWith(input.ToLower())); // Find first matching command

        if (!string.IsNullOrEmpty(match))
        {
            currentSuggestion = match;
            suggestionText.text = $"Suggestion: <color={colorSuggestion}>{match}</color>";
        }
        else
        {
            currentSuggestion = "";
            suggestionText.text = "";
        }
    }

    void ToggleConsole() // Open/Close Console
    {
        isConsoleOpen = !isConsoleOpen;
        consolePanel.SetActive(isConsoleOpen);

        if (isConsoleOpen)
        {
            if (cameraScript != null) cameraScript.enabled = false; // Disable camera control
            
            previousLockMode = Cursor.lockState;
            previousCursorVisible = Cursor.visible;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;

            commandInput.ActivateInputField();
            ScrollToBottom();
        }
        else
        {
            if (cameraScript != null) cameraScript.enabled = true; // Re-enable camera control
            Cursor.lockState = previousLockMode;
            Cursor.visible = previousCursorVisible;
            commandInput.DeactivateInputField();
            commandInput.text = "";
            if(suggestionText != null) suggestionText.text = "";
        }
    }

    void SubmitCommand(string input) // Process Command
    {
        commandHistory.Add(input);
        historyIndex = commandHistory.Count;
        PrintLog($"> {input}", colorCommand);
        ProcessCommand(input);
        commandInput.text = "";
        StartCoroutine(KeepFocus());
    }

    void NavigateHistory(int direction) // Up/Down through command history
    {
        historyIndex += direction;
        if (historyIndex < 0) historyIndex = 0;
        if (historyIndex > commandHistory.Count) historyIndex = commandHistory.Count;

        if (historyIndex < commandHistory.Count)
        {
            commandInput.text = commandHistory[historyIndex];
            commandInput.caretPosition = commandInput.text.Length;
        }
        else commandInput.text = "";
    }

    void ProcessCommand(string input)
    {
        string[] parts = input.Split(' ');
        string command = parts[0].ToLower();

        switch (command)
        {
            case "/addmoney":
                if (parts.Length > 1 && float.TryParse(parts[1], out float amount))
                {
                    if (GameManager.Instance != null)
                    {
                        GameManager.Instance.AddMoney(amount);
                        PrintLog($"System: Added ${amount}. New Total: ${GameManager.Instance.money}", colorSystem);
                    }
                    else
                    {
                        PrintLog("Error: GameManager not found!", colorError);
                    }
                }
                else PrintLog("Usage: /addmoney [amount]", colorError);
                break;

            case "/nextday":
                if (DayNightCycle.Instance != null)
                {
                    DayNightCycle.Instance.SkipToNextMorning();
                    PrintLog("System: Advanced to next morning.", colorSystem);
                }
                else
                {
                    PrintLog("Error: DayNightCycle not found!", colorError);
                }
                break;

            case "/setrep":
                 if (parts.Length > 1 && float.TryParse(parts[1], out float rep))
                 {
                    if (rep < 0 || rep > 100)
                    {
                        PrintLog("Error: The reputation must be between 0 and 100!", colorError);
                        break; 
                    }
                    if (GameManager.Instance != null)
                    {
                        GameManager.Instance.SetReputation(rep);
                        PrintLog($"System: Reputation set to {rep}", colorSystem);
                    }
                 }
                else PrintLog("Usage: /setrep [0-100]", colorError);
                break;
             
             case "/speed":
                if (parts.Length > 1 && float.TryParse(parts[1], out float speed))
                {
                    Time.timeScale = speed; 
                    PrintLog($"System: Game Speed set to {speed}x", colorSystem);
                }
                break;
            
            case "/clear":
                logText.text = "";
                break;
                
            case "/quit":
                Application.Quit();
                #if UNITY_EDITOR
                UnityEditor.EditorApplication.isPlaying = false;
                #endif
                break;

            case "/help":
                string allCmds = string.Join("\n", validCommands);
                PrintLog($"Commands: {allCmds}", colorSystem);
                break;

            default:
                PrintLog($"Unknown command: {command}", colorError);
                break;
        }
    }

    public static void Log(string message, string color = "#FFFFFF") // Static method to log messages
    {
        if (Instance != null) Instance.PrintLog(message, color);
    }

    void PrintLog(string message, string hexColor) // Print message to console
    {
        if (logText != null)
        {
            logText.text += $"<color={hexColor}>{message}</color>\n";
            ScrollToBottom();
        }
    }

    void HandleUnityLog(string logString, string stackTrace, LogType type){} // Capture Unity Logs

    void ScrollToBottom() { StartCoroutine(ScrollToBottomCoroutine()); } // Ensure scrolling happens after UI update

    IEnumerator ScrollToBottomCoroutine() // Scroll to bottom of log
    {
        yield return new WaitForEndOfFrame();
        if (scrollRect != null) scrollRect.verticalNormalizedPosition = 0f;
    }

    IEnumerator KeepFocus() // Keep input field focused
    {
        yield return null;
        commandInput.ActivateInputField();
        commandInput.Select();
    }
}