using UnityEngine;
using System.Collections;

public class SidebarAnimator : MonoBehaviour
{
    [Header("References")]
    public RectTransform sidebarRect;
    public RectTransform arrowIcon;   
    public CanvasGroup canvasGroup; 

    [Header("Animation Settings")]
    public float openPosX = 0f;       
    public float closedPosX = -250f;  
    public float animationDuration = 0.3f; 

    private bool isOpen = true; 
    private Coroutine animationCoroutine;

    public void ToggleSidebar()
    {
        isOpen = !isOpen; 

        if (animationCoroutine != null)
        {
            StopCoroutine(animationCoroutine); // Stop any ongoing animation to prevent conflicts
        }

        float targetX = isOpen ? openPosX : closedPosX;
        float targetAlpha = isOpen ? 1f : 0f; 
        
        if (canvasGroup != null) canvasGroup.blocksRaycasts = isOpen; // Enable/disable interaction based on open state

        animationCoroutine = StartCoroutine(AnimatePanel(targetX, targetAlpha)); // Start the animation coroutine

        if (arrowIcon != null)
        {
            arrowIcon.localScale = new Vector3(isOpen ? 1 : -1, 1, 1); // Flip the arrow icon based on the open state
        }
    }

    private IEnumerator AnimatePanel(float targetX, float targetAlpha) // Coroutine to animate the sidebar's position and opacity
    {
        float elapsedTime = 0; // Store the starting position and alpha for the animation
        
        Vector2 startPos = sidebarRect.anchoredPosition; // Store the starting position for the animation
        float startAlpha = canvasGroup != null ? canvasGroup.alpha : 1f; // Store the starting position and alpha for the animation

        while (elapsedTime < animationDuration)
        {
            float t = elapsedTime / animationDuration; // Normalize time to a 0-1 range
            t = Mathf.SmoothStep(0f, 1f, t); // Smooth the animation curve for a more natural feel

            sidebarRect.anchoredPosition = Vector2.Lerp(startPos, new Vector2(targetX, startPos.y), t); // Lerp the position and alpha based on the normalized time
            if (canvasGroup != null) canvasGroup.alpha = Mathf.Lerp(startAlpha, targetAlpha, t);
            
            elapsedTime += Time.deltaTime;
            yield return null; 
        }

        sidebarRect.anchoredPosition = new Vector2(targetX, startPos.y); // Ensure the final position and alpha are set at the end of the animation
        if (canvasGroup != null) canvasGroup.alpha = targetAlpha;
    }
}