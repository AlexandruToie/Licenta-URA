using UnityEngine;
using UnityEngine.UI;

public class ColorPaletteButton : MonoBehaviour
{
    //This is a simple script that will be attached to each color button in the palette, 
    // it will notify the EditorUIController when it's clicked, so we can change the current color in the canvas engine and update the color preview in the UI
   
    public EditorUIController controller; 

    void Start() // We add a listener to the button click event, so when the user clicks on this color in the palette, we notify the EditorUIController to change the current color
    {
        Button btn = GetComponent<Button>();
        Image img = GetComponent<Image>();
        
        btn.onClick.AddListener(() => 
        {
            if(controller) controller.ChangeColor(img.color);
        });
    }
}