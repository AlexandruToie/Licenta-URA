using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections.Generic;

public class PixelArtCanvas : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
{
    public enum ToolType { Pencil, Eraser, Bucket, Eyedropper }

    [Header("Canvas Settings")]
    public int resolution = 512; 
    public FilterMode textureMode = FilterMode.Point;
    
    [Header("Current State")]
    public ToolType currentTool = ToolType.Pencil;
    public Color brushColor = Color.black;
    public float brushSize = 5f; 

    [Header("Zoom Settings")]
    public RectTransform contentParent;
    public float minZoom = 1f;
    public float maxZoom = 10f;

    private List<Color[]> undoHistory = new List<Color[]>(); // A list that will store the history of pixel states for undo functionality
    private int maxUndoSteps = 10; 

    private bool isStateSavedForCurrentStroke = false;
    private float lastUndoTime = 0f;


    private Texture2D texture;
    private RawImage rawImage;
    private RectTransform rectTransform;
    private Vector2Int? lastPixelPos = null; 

    public System.Action<Color> OnColorPicked;

    void Start() 
    {
        rawImage = GetComponent<RawImage>();
        rectTransform = GetComponent<RectTransform>();
        
        texture = new Texture2D(resolution, resolution);
        texture.filterMode = textureMode;
        texture.wrapMode = TextureWrapMode.Clamp; 
        
        ClearCanvas(false);
        rawImage.texture = texture;
    }

    void Update()
    {
        if ((Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl)) && Input.GetKeyDown(KeyCode.Z))
        {
            if (Time.time - lastUndoTime > 0.1f)
            {
                Undo();
                lastUndoTime = Time.time;
            }
        }
    }

    public void OnPointerDown(PointerEventData eventData) 
    { 
        if (Input.GetMouseButton(0))
        {
            //To determinate the moment to save the state for undo, we check if it's the first pixel of the stroke (lastPixelPos is null) and if the current tool is not the eyedropper
            //  (since it doesn't modify the canvas). This way, we ensure that we only save the state once per stroke, preventing multiple entries in the undo history
            //  for a single continuous drawing action.
            if (!isStateSavedForCurrentStroke && currentTool != ToolType.Eyedropper)
            {
                SaveStateForUndo();
                isStateSavedForCurrentStroke = true;
            }
            UseTool(eventData, true); 
        }
    }

    public void OnPointerUp(PointerEventData eventData) 
    { 
        lastPixelPos = null; 
        isStateSavedForCurrentStroke = false;
    }

    public void SaveStateForUndo() // A simple function that saves the current pixel
    {
        undoHistory.Add(texture.GetPixels());
        if (undoHistory.Count > maxUndoSteps)
        {
            undoHistory.RemoveAt(0);
        }
    }

    public void Undo() // A simple function that restores the last saved pixel state from the undo history
    {
        if (undoHistory.Count > 0) 
        {
            int lastIndex = undoHistory.Count - 1;
            
            texture.SetPixels(undoHistory[lastIndex]);
            texture.Apply();
            undoHistory.RemoveAt(lastIndex);
        }
    }

    public void SetZoom(float zoomValue) 
    {
        if (contentParent != null)
        {
            float z = Mathf.Clamp(zoomValue, minZoom, maxZoom);
            contentParent.localScale = new Vector3(z, z, 1f);
            if (z <= minZoom + 0.1f) contentParent.anchoredPosition = Vector2.zero; 
            else ConstrainPosition(); 
        }
    }

    public void OnDrag(PointerEventData eventData) // How we draw
    {
        if (Input.GetMouseButton(1) || Input.GetMouseButton(2))
        {
            if (contentParent != null) { contentParent.anchoredPosition += eventData.delta; ConstrainPosition(); }
            return; 
        }
        if (Input.GetMouseButton(0)) UseTool(eventData, false);
    }

    void ConstrainPosition()  // A function that constrains the position of the content parent to prevent it from being dragged too far away when zoomed in
    {
        if (contentParent == null) return;
        float currentScale = contentParent.localScale.x;
        float limitX = (rectTransform.rect.width * currentScale) / 2f;
        float limitY = (rectTransform.rect.height * currentScale) / 2f;
        Vector2 pos = contentParent.anchoredPosition;
        pos.x = Mathf.Clamp(pos.x, -limitX, limitX);
        pos.y = Mathf.Clamp(pos.y, -limitY, limitY);
        contentParent.anchoredPosition = pos;
    }

    public void LoadImageToCanvas(Texture2D loadedTex)
    {
        SaveStateForUndo();
        Color[] newPixels = new Color[resolution * resolution];
        for(int y=0; y<resolution; y++)
            for(int x=0; x<resolution; x++)
                newPixels[y*resolution + x] = loadedTex.GetPixelBilinear(x / (float)resolution, y / (float)resolution);
        
        texture.SetPixels(newPixels);
        texture.Apply();
    }

    void UseTool(PointerEventData eventData, bool isClick)
    {
        Vector2 localCursor;
        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(rectTransform, eventData.position, eventData.pressEventCamera, out localCursor)) return;

        Rect r = rectTransform.rect;
        int x = Mathf.FloorToInt(((localCursor.x - r.x) / r.width) * resolution);
        int y = Mathf.FloorToInt(((localCursor.y - r.y) / r.height) * resolution);

        if (x < 0 || x >= resolution || y < 0 || y >= resolution) return;

        switch (currentTool)
        {
            case ToolType.Pencil: DrawLineLogic(x, y, brushColor); break;
            case ToolType.Eraser: DrawLineLogic(x, y, Color.white); break;
            case ToolType.Bucket: if (isClick) FloodFill(x, y, brushColor); break;
            case ToolType.Eyedropper: if (isClick) PickColor(x, y); break;
        }
        lastPixelPos = new Vector2Int(x, y);
        texture.Apply();
    }
    
    void DrawLineLogic(int x, int y, Color c) 
    { 
        if(lastPixelPos!=null) DrawLine(lastPixelPos.Value.x, lastPixelPos.Value.y, x, y, c); 
        else DrawBrush(x, y, c);
    }
    
    void DrawBrush(int cx, int cy, Color col)
    {
        float radius = Mathf.Max(0.5f, brushSize / 2f);
        int range = Mathf.CeilToInt(radius);
        for (int i = -range; i <= range; i++) 
            for (int j = -range; j <= range; j++) 
            {
                int px = cx + i; int py = cy + j;
                if (px >= 0 && px < resolution && py >= 0 && py < resolution) 
                    if (Vector2.Distance(new Vector2(cx, cy), new Vector2(px, py)) <= radius) texture.SetPixel(px, py, col);
            }
    }
    
    void DrawLine(int x0, int y0, int x1, int y1, Color col)
    {
        float dist = Vector2.Distance(new Vector2(x0, y0), new Vector2(x1, y1));
        float step = 1f / (dist + 1f); 
        for (float t = 0; t <= 1; t += step) 
            DrawBrush(Mathf.RoundToInt(Mathf.Lerp(x0, x1, t)), Mathf.RoundToInt(Mathf.Lerp(y0, y1, t)), col);
    }
    
    void FloodFill(int x, int y, Color c) 
    { 
        Color startColor = texture.GetPixel(x, y);
        if (IsSameColor(startColor, c)) return; 
        Queue<Vector2Int> pixels = new Queue<Vector2Int>(); pixels.Enqueue(new Vector2Int(x, y));
        bool[,] visited = new bool[resolution, resolution];
        while (pixels.Count > 0) 
        {
            Vector2Int p = pixels.Dequeue();
            if (p.x < 0 || p.x >= resolution || p.y < 0 || p.y >= resolution || visited[p.x, p.y]) continue;
            if (IsSameColor(texture.GetPixel(p.x, p.y), startColor)) 
            {
                texture.SetPixel(p.x, p.y, c); visited[p.x, p.y] = true;
                pixels.Enqueue(new Vector2Int(p.x + 1, p.y)); 
                pixels.Enqueue(new Vector2Int(p.x - 1, p.y));
                pixels.Enqueue(new Vector2Int(p.x, p.y + 1)); 
                pixels.Enqueue(new Vector2Int(p.x, p.y - 1));
            }
        }
    }
    
    bool IsSameColor(Color a, Color b) 
    { 
        return Mathf.Abs(a.r-b.r)<0.01f && Mathf.Abs(a.g-b.g)<0.01f && Mathf.Abs(a.b-b.b)<0.01f; 
    }
    void PickColor(int x, int y) 
    { 
        brushColor = texture.GetPixel(x,y); OnColorPicked?.Invoke(brushColor); currentTool = ToolType.Pencil;
    }
    
    public void ClearCanvas(bool saveUndo = true) 
    {
        if(saveUndo) SaveStateForUndo(); 
        Color[] c = new Color[resolution * resolution]; 
        for(int i=0; i<c.Length; i++) c[i] = Color.white; 
        texture.SetPixels(c); 
        texture.Apply(); 
    }

    public Texture2D GetTexture() => texture;
    public void SetTool(ToolType t) => currentTool = t;
    public void SetColor(Color c) => brushColor = c;
    public void SetBrushSize(float s) => brushSize = s;
}