using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.IO;
using SFB;

public class EditorUIController : MonoBehaviour
{
    [Header("Engine Reference")]
    public PixelArtCanvas canvasEngine; 

    [Header("Tools Buttons")]
    public Button btnPencil;
    public Button btnEraser;
    public Button btnBucket;
    public Button btnEyedropper;

    [Header("Action Buttons")]
    public Button btnClear; 
    public Button btnLoad;  
    
   
    public Color activeToolColor = Color.green;
    public Color normalToolColor = Color.white;

    [Header("Controls")]
    public Slider zoomSlider;
    public Slider brushSizeSlider;
    public TMP_InputField fileNameInput;
    public Button saveButton;
    public Button closeButton;
    public GameObject editorWindow;

    [Header("Color Palette")]
    public Image colorPreview; //This will show the currently selected color

    void Start()
    {
        // Configuration
        if(zoomSlider) 
        {
            zoomSlider.minValue = 1f; zoomSlider.maxValue = 10f;
            zoomSlider.onValueChanged.AddListener((v) => canvasEngine.SetZoom(v));
        }

        if(brushSizeSlider)
        {
            brushSizeSlider.wholeNumbers = true;
            brushSizeSlider.minValue = 1f;   
            brushSizeSlider.maxValue = 64f;      
            
            //The initial value of the brush size
            brushSizeSlider.value = 5f;

            // Ascultam schimbarea
            brushSizeSlider.onValueChanged.AddListener((v) => 
            {
                if(canvasEngine) canvasEngine.SetBrushSize(v);
            });
        }

        if(saveButton) saveButton.onClick.AddListener(SaveImageUserChoice);
        if(closeButton) closeButton.onClick.AddListener(CloseEditor);
        if(btnClear) btnClear.onClick.AddListener(() => canvasEngine.ClearCanvas(true));
        if(btnLoad) btnLoad.onClick.AddListener(LoadImageFromPC);

        // Tool buttons configuration
        btnPencil.onClick.AddListener(() => SelectTool(PixelArtCanvas.ToolType.Pencil, btnPencil));
        btnEraser.onClick.AddListener(() => SelectTool(PixelArtCanvas.ToolType.Eraser, btnEraser));
        btnBucket.onClick.AddListener(() => SelectTool(PixelArtCanvas.ToolType.Bucket, btnBucket));
        btnEyedropper.onClick.AddListener(() => SelectTool(PixelArtCanvas.ToolType.Eyedropper, btnEyedropper));

        // We check if the canvasEngine has an event for color picking and subscribe to it, so we can update the color preview in the UI
        if(canvasEngine)
        {
            canvasEngine.OnColorPicked += UpdateColorPreview;
        }

        // Selecting the default tool at the start
        SelectTool(PixelArtCanvas.ToolType.Pencil, btnPencil);
    }

    void Update()
    {
        if ((Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl)) && Input.GetKeyDown(KeyCode.Z))
        {
            if (canvasEngine)
            {
                canvasEngine.Undo();
                Debug.Log("Undo action performed.");
            }
        }
    }

    void SelectTool(PixelArtCanvas.ToolType type, Button clickedBtn)
    {
        if(canvasEngine) canvasEngine.SetTool(type);

        // Reset all the buttons to their origilan color
        btnPencil.image.color = normalToolColor;
        btnEraser.image.color = normalToolColor;
        btnBucket.image.color = normalToolColor;
        btnEyedropper.image.color = normalToolColor;

        // We change color for the active tool button to give user feedback on what tool is currently selected
        clickedBtn.image.color = activeToolColor;
    }

    public void ChangeColor(Color newColor)
    {
        if(canvasEngine) canvasEngine.SetColor(newColor);
        UpdateColorPreview(newColor);
        
        if (canvasEngine.currentTool == PixelArtCanvas.ToolType.Eraser || 
            canvasEngine.currentTool == PixelArtCanvas.ToolType.Eyedropper)
        {
            SelectTool(PixelArtCanvas.ToolType.Pencil, btnPencil);
        }
    }

    void UpdateColorPreview(Color c)
    {
        if (colorPreview) colorPreview.color = c;
    }

    void SaveImageUserChoice() // This method will be called when the user clicks the Save button, it will save the image to desktop with the name provided in the input field
    {
        if (canvasEngine == null) return;
        string fileName = string.IsNullOrWhiteSpace(fileNameInput.text) ? "MyLogo" : fileNameInput.text;
        if (!fileName.EndsWith(".png")) fileName += ".png";

        Texture2D tex = canvasEngine.GetTexture();
        byte[] bytes = tex.EncodeToPNG();
        string path = Path.Combine(System.Environment.GetFolderPath(System.Environment.SpecialFolder.Desktop), fileName);
        File.WriteAllBytes(path, bytes);
        Application.OpenURL("file://" + System.Environment.GetFolderPath(System.Environment.SpecialFolder.Desktop));
        
        if (GameManager.Instance != null) GameManager.Instance.companyLogo = tex;
    }

    void LoadImageFromPC()
    {
        if (canvasEngine == null) return; 

        var extensions = new [] {
            new ExtensionFilter("Image Files", "png", "jpg", "jpeg" ),
        };

        string[] paths = StandaloneFileBrowser.OpenFilePanel("Open Image", "", extensions, false);

        if (paths.Length > 0 && !string.IsNullOrEmpty(paths[0]))
        {
            string filePath = paths[0];
            StartCoroutine(LoadImageRoutine(filePath));
        }
    }

    System.Collections.IEnumerator LoadImageRoutine(string url)
    {
        // Convertim path-ul in format URL pentru UnityWebRequest (mai sigur)
        string finalUrl = "file:///" + url;
        
        using (UnityEngine.Networking.UnityWebRequest uwr = UnityEngine.Networking.UnityWebRequestTexture.GetTexture(finalUrl))
        {
            yield return uwr.SendWebRequest();

            if (uwr.result == UnityEngine.Networking.UnityWebRequest.Result.Success)
            {
                // Luam textura descarcata
                Texture2D loadedTex = UnityEngine.Networking.DownloadHandlerTexture.GetContent(uwr);
                
                // O trimitem la Canvas
                if (loadedTex != null)
                {
                    canvasEngine.LoadImageToCanvas(loadedTex);
                    Debug.Log("Imagine incarcata cu succes: " + url);
                }
            }
            else
            {
                Debug.LogError("Eroare la incarcare: " + uwr.error);
            }
        }
    }

    void CloseEditor()
    {
        if (GameManager.Instance != null && canvasEngine != null)
            GameManager.Instance.companyLogo = canvasEngine.GetTexture();
        
        if (editorWindow) editorWindow.SetActive(false);
    }
}