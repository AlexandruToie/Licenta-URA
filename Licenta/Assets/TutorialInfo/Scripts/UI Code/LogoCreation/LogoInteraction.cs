using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class LogoInteraction : MonoBehaviour, IPointerClickHandler
{
    [Header("UI References")]
    public GameObject contextMenu;      
    public GameObject editorWindow;     
    public RawImage logoDisplay;     

    [Header("Buttons")]
    public Button btnModify;
    public Button btnCloseEditor;

    [Header("Painter Reference")]
    public PixelArtCanvas painterScript; 

    //The cashe memory for the ReactMeniuu
    private RectTransform menuRect;

    private void OnEnable()
    {
        RefreshLogo();
    }

    public void RefreshLogo()
    {
        if (logoDisplay != null)
        {
            if (painterScript != null && painterScript.GetTexture() != null)
            {
                logoDisplay.texture = painterScript.GetTexture();
                logoDisplay.color = Color.white;
            }
            else if (GameManager.Instance != null && GameManager.Instance.companyLogo != null)
            {
                logoDisplay.texture = GameManager.Instance.companyLogo;
                logoDisplay.color = Color.white;
            }
        }
    }

    void Start()
    {
        if (contextMenu)
        {
            contextMenu.SetActive(false);
            menuRect = contextMenu.GetComponent<RectTransform>();
        }
        if (editorWindow) editorWindow.SetActive(false);

        if (btnModify) btnModify.onClick.AddListener(OpenEditor);
        if (btnCloseEditor) btnCloseEditor.onClick.AddListener(CloseEditor);
    }

    void Update()
    {
        if (contextMenu != null && contextMenu.activeSelf)
        {
            // Checking if the mouse it is still on top of the logo
            bool isOverLogo = RectTransformUtility.RectangleContainsScreenPoint(logoDisplay.rectTransform, Input.mousePosition);

            // Checking if the mouse is still on top of the menu
            bool isOverMenu = false;
            if (menuRect != null)
            {
                isOverMenu = RectTransformUtility.RectangleContainsScreenPoint(menuRect, Input.mousePosition);
            }
            if (!isOverLogo && !isOverMenu)
            {
                contextMenu.SetActive(false);
            }
        }
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData.button == PointerEventData.InputButton.Right)
        {
            if (contextMenu != null)
            {
                contextMenu.transform.position = eventData.position;
                contextMenu.SetActive(true);
            }
        }
        else
        {
            if (contextMenu != null) contextMenu.SetActive(false);
        }
    }

    void OpenEditor()
    {
        if (contextMenu) contextMenu.SetActive(false);
        if (editorWindow) editorWindow.SetActive(true);
    }

    void CloseEditor()
    {
        if (painterScript != null)
        {
            Texture2D createdTexture = painterScript.GetTexture();
            
            if (createdTexture != null)
            {
                if (logoDisplay != null)
                {
                    logoDisplay.texture = createdTexture;
                    logoDisplay.color = Color.white;
                    logoDisplay.SetAllDirty(); 
                }
                if (GameManager.Instance != null) 
                {
                    GameManager.Instance.companyLogo = createdTexture;
                }
            }
        }
        else
        {
            Debug.LogError("LogoInteraction: The reference to the painter script is missing. Please assign it in the inspector.");
        }
        if (editorWindow) editorWindow.SetActive(false);
    }
}