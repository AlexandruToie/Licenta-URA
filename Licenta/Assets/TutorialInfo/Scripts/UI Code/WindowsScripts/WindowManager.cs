using UnityEngine;
using UnityEngine.UI;

public class WindowManager : MonoBehaviour
{
    [Header("Reference for the camera script")]
    public MonoBehaviour cameraScript; 

    [Header("References for Windows")]
    public GameObject windowManagement;
    public GameObject windowSales;
    public GameObject windowProduction;
    public GameObject windowUpgrades;
    public GameObject windowSettings;

    [Header("References for Dock Buttons")]
    public Button btnManagement;
    public Button btnSales;
    public Button btnProduction;
    public Button btnUpgrades;
    public Button btnSettings;

    [Header("References for Close Buttons")]
    public Button closeBtnManagement;
    public Button closeBtnSales;
    public Button closeBtnProduction;
    public Button closeBtnUpgrades;
    public Button closeBtnSettings;

    private CursorLockMode previousLockMode;
    private bool previousCursorVisible;

    void Start()
    {
        CloseAll(); 

        btnManagement.onClick.AddListener(() => ToggleWindow(windowManagement));
        btnSales.onClick.AddListener(() => ToggleWindow(windowSales));
        btnProduction.onClick.AddListener(() => ToggleWindow(windowProduction));
        btnUpgrades.onClick.AddListener(() => ToggleWindow(windowUpgrades));//Decomented for testing
        btnSettings.onClick.AddListener(() => ToggleWindow(windowSettings));//Decomented for testing

        if (closeBtnManagement != null) 
            closeBtnManagement.onClick.AddListener(CloseAll);
        if (closeBtnSales != null) 
            closeBtnSales.onClick.AddListener(CloseAll);
        if (closeBtnProduction != null) 
            closeBtnProduction.onClick.AddListener(CloseAll);
        if (closeBtnUpgrades != null) 
            closeBtnUpgrades.onClick.AddListener(CloseAll);
        if (closeBtnSettings != null) 
            closeBtnSettings.onClick.AddListener(CloseAll);

    }

    void Update()
    {
        CheckDepartmentRequirements();
    }

    private void CheckDepartmentRequirements()
    {
        bool hasProduction = false;
        bool hasCreationOrMarketing = false;
        bool hasCEO = false;

        if (UIDepartmentNode.allDepartments != null)
        {
            foreach (var node in UIDepartmentNode.allDepartments)
            {
                if (node.myType != null && !string.IsNullOrEmpty(node.myType.defaultName))
                {
                    string depName = node.myType.defaultName.ToLower();

                    if (depName.Contains("production"))
                    {
                        hasProduction = true;
                    }
                    if (depName.Contains("creation") || depName.Contains("sales") || depName.Contains("marketing") || depName.Contains("customer"))
                    {
                        hasCreationOrMarketing = true;
                    }
                    if(depName.Contains("ceo"))
                    {
                        hasCEO = true;
                    }
                }
            }
        }

        if (btnProduction != null) 
        {
            btnProduction.interactable = hasProduction;
            if (!hasProduction && windowProduction != null && windowProduction.activeSelf)
            {
                CloseAll();
            }
        }
        if (btnSales != null) 
        {
            btnSales.interactable = hasCreationOrMarketing;
            if (!hasCreationOrMarketing && windowSales != null && windowSales.activeSelf)
            {
                CloseAll();
            }
        }
        if (btnUpgrades !=null)
        {
            btnUpgrades.interactable = hasCEO;
            if (!hasCEO && windowUpgrades != null && windowUpgrades.activeSelf)
            {
                CloseAll();
            }
        }
    }

    public void ToggleWindow(GameObject targetWindow)
    {
        bool isTargetActive = targetWindow.activeSelf;
        CloseAllWindowsInternal();
        if (!isTargetActive)
        {
            targetWindow.SetActive(true);
            SetUIState(true);
        }
        else
        {
            SetUIState(false);
        }
    }

    public void CloseAll()
    {
        CloseAllWindowsInternal();
        SetUIState(false); 
    }

    private void CloseAllWindowsInternal()
    {
        if(windowManagement != null) windowManagement.SetActive(false);
        if(windowSales != null) windowSales.SetActive(false);
        if(windowProduction != null) windowProduction.SetActive(false);
        if(windowUpgrades != null) windowUpgrades.SetActive(false);
        if(windowSettings != null) windowSettings.SetActive(false);
    }

    void SetUIState(bool isUIOpen)
    {
        if (isUIOpen)
        {
            // We save the current cursor state before changing it, so we can restore it later
            previousLockMode = Cursor.lockState;
            previousCursorVisible = Cursor.visible;

            // We disable the camera script to prevent player movement while UI is open
            if (cameraScript != null) cameraScript.enabled = false;

            // We block the cursor and hide it, so the player can interact with the UI without affecting the camera
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
        else
        {
            //Reenable the camera script to allow player movement again
            if (cameraScript != null) cameraScript.enabled = true;

            Cursor.lockState = CursorLockMode.Locked; 
            Cursor.visible = false;
        }
    }
}