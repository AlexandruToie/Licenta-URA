using System;
using UnityEngine;

public class NoiseMap : MonoBehaviour
{
    // Width of the noise texture in pixels
    public int width = 256;
    // Height of the noise texture in pixels
    public int height = 256;

    // Scale factor for the Perlin noise - higher values create more zoomed-in patterns
    public float scale = 20f;
    // X-axis offset for the noise pattern
    public float offsetX = 100f;
    // Y-axis offset for the noise pattern
    public float offsetY = 100f;

    void Start()
    {
        // Initialize random offsets for X and Y to generate different patterns each time
        offsetX = UnityEngine.Random.Range(0f, 9999f);
        offsetY = UnityEngine.Random.Range(0f, 9999f);
    }

    void Update()
    {
        //Update tghe texture every frame
        Renderer renderer = GetComponent<Renderer>();
        renderer.material.mainTexture = GenerateTexture();
    }

    Texture2D GenerateTexture()
    {
        // Create a new texture with the specified width and height and apply it to the material
        Texture2D texture = new Texture2D(width, height);
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                Color color = CalculateColor(x, y);
                texture.SetPixel(x, y, color);
            }
        }
        texture.Apply();
        return texture;
    }
    
    // Calculate the color for a specific pixel based on Perlin noise
    Color CalculateColor(int x, int y)
    {
        float xCoord = (float)x / width * scale + offsetX;
        float yCoord = (float)y / height * scale + offsetY;

        float semple = Mathf.PerlinNoise(xCoord, yCoord);
        return new Color(semple, semple, semple);
    }
}
