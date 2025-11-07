using System.Collections.Generic;
using UnityEngine;

public class WindingPathTerrainGenerator : MonoBehaviour
{

    public int width = 256;
    public int height = 256;
    public int depth = 20;
    public float scale = 20f;
    public float offsetX = 100f;
    public float offsetY = 100f;


    [Header("Natural Terrain Noise (Octaves)")]
    
    public int octaves = 4; // Number of noise layers to combine

    [Range(0f, 1f)] // Persistence controls amplitude decrease per octave
    
    public float persistence = 0.45f; // Amplitude multiplier per octave
    public float lacunarity = 2f; // Frequency multiplier per octave

    
    [Header("Terrain Variation Mask")]
    public float maskScale = 5f; // Scale of the low-frequency mask for terrain variation
    public float minNoiseIntensity = 0.1f; // Minimum intensity for the terrain variation mask

    [Header("Flat Zone & Path Size")]
    public float centerSize = 0.2f;
    
    public float centerBlendFactor = 1.5f; // Multiplier for how wide the blend zone is around the center. 1.0 = same as center size, 2.0 = double.
    
    public float pathWidth = 0.015f; // percentage of total size
    
    public float pathBlendFactor = 4.5f; // Multiplier for how wide the blend zone is around the path. 1.0 = same as path width, 2.0 = double.
    
    [Header("Path Shape (Transfăgărășan)")]
    public int numPaths = 4;
    public int pathResolution = 150; // number of points sampled per path
    
    public float wobbleAmplitude = 5f; // amplitude for perlin wobble
    
    public float wobbleFrequency = 10f; // frequency for perlin wobble

    [Header("Terrain Texturing")]
    [Tooltip("Angle (0-1) where terrain becomes 'rock'. 0.4-0.6 is a good range.")]
    [Range(0f, 1f)]
    public float rockSlopeThreshold = 0.5f; // Normalized slope (0-1)
    [Tooltip("How much to blend the slope texture (rock).")]
    public float slopeBlendAmount = 0.1f;
    
    [Header("Tree Generation")]
  
    // public int treePrototypeIndex = 0; // This is no longer used
    [Tooltip("Base chance (0-1) to plant a tree at any given spot. (Set higher for testing)")]
    [Range(0f, 1f)]
  
    public float treeDensity = 0.4f; 
    [Tooltip("The minimum slope (0-1) trees can grow on.")]
    [Range(0f, 1f)]
    public float minTreeSlope = 0f;
    [Tooltip("The maximum slope (0-1) trees can grow on. (0.6 = ~54 degrees)")]
    [Range(0f, 1f)]
    public float maxTreeSlope = 0.6f; 
    [Tooltip("The minimum (normalized 0-1) height trees can grow at.")]
    [Range(0f, 1f)]
    public float minTreeHeight = 0.05f; 
    [Tooltip("The maximum (normalized 0-1) height trees can grow at.")]
    [Range(0f, 1f)]
    public float maxTreeHeight = 0.8f; // Maximum height for tree growth


    [Header("City Generation")]
    [Tooltip("Drag your CityGenerator GameObject here.")]
    public CityGenerator cityGenerator; 

    public float[,] pathMask;
    public List<Vector2[]> paths;
    public float centerRadius;
    private float outerCenterRadius;
    public int centerX;
    public int centerY;
    private float pathWidthPixels;
    private float maskOffsetX; 
    private float maskOffsetY;

    void Start() 
    {
        // ... (no changes in Start function) ...
        offsetX = Random.Range(0f, 9999f); // Randomize offsets for terrain variation
        offsetY = Random.Range(0f, 9999f);
        numPaths = Random.Range(2, 7);  // Randomize number of paths between 2 and 6

        maskOffsetX = Random.Range(1000f, 2000f); // Different range to avoid correlation with main noise
        maskOffsetY = Random.Range(1000f, 2000f);

        centerX = width / 2; 
        centerY = height / 2;
        centerRadius = width * centerSize; // Calculate center radius in pixels
        outerCenterRadius = centerRadius * centerBlendFactor; // Calculate outer radius for blending
        
        pathWidthPixels = width * pathWidth; // Convert path width to pixels

        // 1. Generate path logic
        GeneratePaths();

        // 2. Generate the terrain once, using the paths
        Terrain terrain = GetComponent<Terrain>();
        terrain.terrainData = GenerateTerrain(terrain); // --- MODIFIED --- Pass the whole Terrain object
    }

    TerrainData GenerateTerrain(Terrain terrain) // --- MODIFIED --- Accept the Terrain object
    {
        TerrainData terrainData = terrain.terrainData; // --- NEW --- Get the terrainData from the terrain
        terrainData.heightmapResolution = width + 1; // Heightmap resolution must be size + 1
        terrainData.size = new Vector3(width, depth, height); // Set the size of the terrain
        
        int resX = width + 1; // +1 because heightmap resolution is size + 1
        int resY = height + 1;

        float minHeight; // For preliminary pass
        float totalHeight;
        
        // We pass `true` for isPreliminaryPass. The flatAreaHeight (0f) is ignored.
        GenerateHeights(resX, resY, true, 0f, out minHeight, out totalHeight); 
        
        float avgHeight = totalHeight / (resX * resY);

        float flatAreaHeight = Random.Range(minHeight, avgHeight);

        // We pass `false` for isPreliminaryPass and our new flatAreaHeight. The `out` params are not needed here.
        float ignoreMin;
        float ignoreTotal;
        
        // Store the final heights
        float[,] finalHeights = GenerateHeights(resX, resY, false, flatAreaHeight, out ignoreMin, out ignoreTotal);
        
        // Set the generated heights to the terrain
        terrainData.SetHeights(0, 0, finalHeights); 

        // Generate and apply the textures (splatmaps)
        GenerateSplatmaps(terrainData, finalHeights);

        // Call the tree generation function
    GenerateTrees(terrainData, finalHeights);

    if (cityGenerator != null)
    {
        Debug.Log("Terrain generation complete. Initializing City Generator...");
        cityGenerator.Initialize(terrain, this);
    }
    else
    {
        Debug.LogWarning("No CityGenerator assigned to WindingPathTerrainGenerator. Skipping city generation.");
    }

    return terrainData;
}


    
    float[,] GenerateHeights(int resX, int resY, bool isPreliminaryPass, float flatAreaHeight, out float minHeight, out float totalHeight) // Generates the heights for the terrain
    {
        float[,] heights = new float[resY, resX]; 
        
        minHeight = 1f;
        totalHeight = 0f;
        float currentHeight = 0f;

        for (int y = 0; y < resY; y++)
        {
            for (int x = 0; x < resX; x++)
            {
                // Calculate the natural terrain height for this point first
                float naturalTerrainHeight = CalculateHeight(x, y, resX, resY);

                if (isPreliminaryPass)
                {
                    // Pass 1: Just calculate natural height and stats
                    currentHeight = naturalTerrainHeight;
                    heights[y, x] = currentHeight; // Use [y, x]

                    totalHeight += currentHeight;
                    if (currentHeight < minHeight)
                    {
                        minHeight = currentHeight;
                    }
                }
                else
                {
                    // Pass 2: Apply the flat areas using the pre-calculated flatAreaHeight and blend roads   
                    // 1. Calculate center blend
                    float centerBlend = 0f;
                    float distanceFromCenter = Vector2.Distance(new Vector2(x, y), new Vector2(centerX, centerY));
                    
                    if (distanceFromCenter <= centerRadius)
                    {
                        centerBlend = 1.0f;
                    }
                    else if (distanceFromCenter < outerCenterRadius)
                    {
                        centerBlend = 1f - Mathf.InverseLerp(centerRadius, outerCenterRadius, distanceFromCenter); // 0..1 blend
                    }
                    
                    // 2. Get path blend
                    // GetPathBlendFactor still uses [x, y] coordinates, which is correct
                    float pathBlend = GetPathBlendFactor(x, y); 
                    
                    // 3. Get the strongest blend (flattest area wins)
                    float totalBlend = Mathf.Max(centerBlend, pathBlend);
                    
                    // 4. Apply blend if needed
                    if (totalBlend > 0f)
                    {
                        // Use SmoothStep for a nicer, non-linear blend
                        float blend = Mathf.SmoothStep(0.0f, 1.0f, totalBlend); 
                        
                        // Lerp between the flat road height and the natural terrain height
                        heights[y, x] = Mathf.Lerp(naturalTerrainHeight, flatAreaHeight, blend); // Use [y, x]
                    }
                    else
                    {
                        heights[y, x] = naturalTerrainHeight; // Use [y, x]
                    }
                }
            }
        }

        return heights;
    }

    
    void GenerateSplatmaps(TerrainData terrainData, float[,] heights)
    {
        // ... (This function is unchanged) ...
        // --- IMPORTANT ---
        // This function assumes you have set up 3 Terrain Layers on your Terrain object
        // in the Unity Editor, in this *exact* order:
        //
        // Layer 0: Road / Dirt (for paths and center)
        // Layer 1: Grass (for low-lying flat areas)
        // Layer 2: Rock (for steep slopes)
        //
        // If you have a different number of layers, this will fail!
        // -----------------

        if (terrainData.alphamapLayers < 3)
        {
            Debug.LogError("Terrain needs at least 3 Terrain Layers (Road, Grass, Rock) to be painted.");
            return;
        }

        int alphaResX = terrainData.alphamapWidth;
        int alphaResY = terrainData.alphamapHeight;
        
        int heightResX = terrainData.heightmapResolution;
        int heightResY = terrainData.heightmapResolution;

        // Create the 3D array: [y, x, layerIndex]
        float[,,] alphamaps = new float[alphaResY, alphaResX, terrainData.alphamapLayers];

        for (int y = 0; y < alphaResY; y++)
        {
            for (int x = 0; x < alphaResX; x++)
            {
                // --- Map alphamap (x,y) to heightmap (hx, hy) ---
                // We find the normalized position (0-1) and scale it to the heightmap resolution
                float normX = (float)x / (alphaResX - 1);
                float normY = (float)y / (alphaResY - 1);
                
                int hx = (int)(normX * (heightResX - 1));
                int hy = (int)(normY * (heightResY - 1));

                // --- Get data for this point ---
                // Get height (normalized 0-1)
                float height = heights[hy, hx]; // We use [hy, hx] because heights is [y, x]

                // Get slope (normalized 0-1)
                // GetSteepness uses normalized (0-1) coordinates
                float slope = terrainData.GetSteepness(normX, normY) / 90f; 

                // --- Blending Logic ---
                // These will be our 3 layer weights.
                float[] weights = new float[terrainData.alphamapLayers];
                
                // 1. Is it a path or flat center?
                float pathBlend = GetPathBlendFactor(hx, hy);
                float centerBlend = 0f;
                float distanceFromCenter = Vector2.Distance(new Vector2(hx, hy), new Vector2(centerX, centerY));
                if (distanceFromCenter <= centerRadius)
                {
                    centerBlend = 1.0f;
                }
                else if (distanceFromCenter < outerCenterRadius)
                {
                    centerBlend = 1f - Mathf.InverseLerp(centerRadius, outerCenterRadius, distanceFromCenter);
                }
                
                // Get the strongest "flat" blend
                float flatBlend = Mathf.Max(pathBlend, centerBlend);
                flatBlend = Mathf.SmoothStep(0.0f, 1.0f, flatBlend);

                // --- Layer Weight Calculation ---
                // Layer 0: Road
                weights[0] = flatBlend; 

                // Layer 1: Grass
                weights[1] = 1f - flatBlend; // Start with grass where there is no road

                // Layer 2: Rock (based on slope)
                // Use InverseLerp to create a blend zone around the threshold
                float rockWeight = Mathf.InverseLerp(rockSlopeThreshold - slopeBlendAmount, rockSlopeThreshold + slopeBlendAmount, slope);
                rockWeight = Mathf.SmoothStep(0.0f, 1.0f, rockWeight) * (1f - flatBlend); // Don't apply rock to flat paths

                // Blend between grass and rock
                weights[1] = Mathf.Lerp(weights[1], 0f, rockWeight);
                weights[2] = Mathf.Lerp(weights[2], 1f, rockWeight);

                // --- Final Step: Normalize and Assign ---
                float totalWeight = 0f;
                for (int i = 0; i < weights.Length; i++)
                {
                    totalWeight += weights[i];
                }

                if (totalWeight > 0.0001f)
                {
                    for (int i = 0; i < weights.Length; i++)
                    {
                        alphamaps[y, x, i] = weights[i] / totalWeight;
                    }
                }
                else
                {
                     alphamaps[y, x, 1] = 1f; // Default to grass if weights are zero
                }
            }
        }

        // Finally, apply the alphamaps to the terrain
        terrainData.SetAlphamaps(0, 0, alphamaps);
    }

    
    void GenerateTrees(TerrainData terrainData, float[,] heights)
    {
        // ... (This function is unchanged) ...
        // --- MODIFIED --- Check if any tree prototypes exist
        if (terrainData.treePrototypes.Length == 0)
        {
            Debug.LogWarning("No Tree Prototypes found. Please add tree prefabs to the terrain in the editor to plant trees.");
            return;
        }

        List<TreeInstance> treeList = new List<TreeInstance>();
        int resX = terrainData.heightmapResolution;
        int resY = terrainData.heightmapResolution;
        
        // --- MODIFIED --- Get the number of available tree prototypes
        int numTreePrototypes = terrainData.treePrototypes.Length;

        // We loop over the heightmap coordinates
        for (int y = 0; y < resY; y++)
        {
            for (int x = 0; x < resX; x++)
            {
                // Use a random check to see if we should plant a tree here
                // This controls the overall density
                if (Random.value > treeDensity)
                {
                    continue;
                }

                // --- CHECK RULES ---
                float normX = (float)x / (resX - 1); 
                float normY = (float)y / (resY - 1);
                
                float height = heights[y, x];
                float slope = terrainData.GetSteepness(normX, normY) / 90f;

                // 1. Check Height Rule
                if (height < minTreeHeight || height > maxTreeHeight)
                {
                    continue;
                }

                // 2. Check Slope Rule
                if (slope < minTreeSlope || slope > maxTreeSlope)
                {
                    continue;
                }

                // 3. Check Path/Center Rule (don't plant on roads)
                float pathBlend = GetPathBlendFactor(x, y);
                float distanceFromCenter = Vector2.Distance(new Vector2(x, y), new Vector2(centerX, centerY));
                float centerBlend = 0f;
                if (distanceFromCenter <= centerRadius) centerBlend = 1f;

                if (pathBlend > 0.1f || centerBlend > 0.1f)
                {
                    continue; // This is a path or the center, skip planting
                }

                // --- ALL RULES PASSED: PLANT A TREE ---
                TreeInstance tree = new TreeInstance();
                
                // Position is (normalized X, heightmap Y, normalized Z)
                tree.position = new Vector3(normX, height, normY);
                
                // --- MODIFIED --- Pick a random tree prototype index
                tree.prototypeIndex = Random.Range(0, numTreePrototypes);
                
                // Add some random variation to size
                tree.widthScale = Random.Range(0.7f, 1.3f);
                tree.heightScale = Random.Range(0.7f, 1.3f);
                
                // Default colors
                tree.color = Color.white;
                tree.lightmapColor = Color.white;

                treeList.Add(tree);
            }
        }

        // Apply the new list of trees to the terrain
        // SetTreeInstances is much faster than AddTreeInstance
        terrainData.SetTreeInstances(treeList.ToArray(), true);

        // Optional: Refresh the terrain collider after placing trees
        float[,,] emptyAlphamap = terrainData.GetAlphamaps(0, 0, 1, 1);
        terrainData.SetAlphamaps(0, 0, emptyAlphamap);
    }


    float GetPathBlendFactor(int x, int y)
    {
        // ... (This function is unchanged) ...
        if (pathMask == null) return 0f;
        // Use correct bounds check
        if (x < 0 || x >= width || y < 0 || y >= height) return 0f;
        return pathMask[x, y];
    }
    public float GetPublicPathBlend(int x, int y)
    {
        return GetPathBlendFactor(x, y);
    }
    
    bool IsNearPath(int x, int y) // Helper to check if a point is near any path
    {
        // ... (This function is unchanged) ...
        return GetPathBlendFactor(x,y) > 0f;
    }


    float CalculateHeight(int x, int y, int resX, int resY)
    {
        // ... (This function is unchanged) ...
        float amplitude = 1f;
        float frequency = 1f;
        float noiseValue = 0f;
        float maxPossibleHeight = 0f; // To normalize the result between 0 and 1

        for (int i = 0; i < octaves; i++)
        {
            // Using (resX - 1) and (resY - 1) so the scale is consistent
            float xCoord = (float)x / (resX - 1) * scale * frequency + offsetX;
            float yCoord = (float)y / (resY - 1) * scale * frequency + offsetY;

            float perlinValue = Mathf.PerlinNoise(xCoord, yCoord);
            noiseValue += perlinValue * amplitude;

            maxPossibleHeight += amplitude; // Add the maximum possible amplitude

            amplitude *= persistence; // Amplitude decreases for the next octave
            frequency *= lacunarity; // Frequency increases (finer details)
        }

        // Normalize the final value to guarantee it's between 0 and 1
        float fractalNoise = noiseValue / maxPossibleHeight;
        
        // 1. Get the mask value (low frequency, large areas)
        float maskXCoord = (float)x / (resX - 1) * maskScale + maskOffsetX;
        float maskYCoord = (float)y / (resY -1) * maskScale + maskOffsetY;
        float maskValue = Mathf.PerlinNoise(maskXCoord, maskYCoord);

        // 2. Smooth the mask to create sharper transitions between flat/hilly
        maskValue = Mathf.SmoothStep(0.0f, 1.0f, maskValue);

        // 3. Map the 0..1 mask value to our desired intensity (e.g., 0.1 to 1.0)
        float intensity = Mathf.Lerp(minNoiseIntensity, 1.0f, maskValue);

        // 4. Apply the intensity to the fractal noise
        return fractalNoise * intensity;
    }

    void GeneratePaths()
    {
        // ... (This function is unchanged) ...
        pathMask = new float[width, height]; // Initialize the path mask
        paths = new List<Vector2[]>(); // List to hold all generated paths

        for (int i = 0; i < numPaths; i++) // Generate each path
        {
            float angle = Random.Range(0f, Mathf.PI * 2f);
            float radius = Mathf.Sqrt(Random.Range(0f, 1f)) * centerRadius; 
            Vector2 start = new Vector2(centerX, centerY) + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius;

            Vector2 target = RandomPointOnBorder();

            Vector2 direction = (target - start);
            Vector2 dirNorm = direction.normalized;
            Vector2 perp = new Vector2(-dirNorm.y, dirNorm.x); 

            Vector2[] pathPoints = new Vector2[pathResolution];
            float seed = Random.Range(0f, 1000f);

            for (int p = 0; p < pathResolution; p++)
            {
                float t = (float)p / (pathResolution - 1);
                Vector2 basePos = Vector2.Lerp(start, target, t);

                float noise = Mathf.PerlinNoise(seed + t * wobbleFrequency, seed + t * 0.37f);
                float wobble = (noise - 0.5f) * 2f * wobbleAmplitude;

                float longitudinal = (Mathf.PerlinNoise(seed + 10f + t * 1.3f, 0.0f) - 0.5f) * (wobbleAmplitude * 0.2f);

                Vector2 pos = basePos + perp * wobble + dirNorm * longitudinal;

                pos.x = Mathf.Clamp(pos.x, 0f, width - 1);
                pos.y = Mathf.Clamp(pos.y, 0f, height - 1);

                pathPoints[p] = pos;
            }

            paths.Add(pathPoints);

            // Rasterize this path into the pathMask to make pixel lookups cheap later
            RasterizePathToMask(pathPoints);
        }
    }

    Vector2 RandomPointOnBorder() // Helper to get a random point on the terrain border
    {
        // ... (This function is unchanged) ...
        int side = Random.Range(0, 4);
        float rx = Random.Range(0f, width - 1);
        float ry = Random.Range(0f, height - 1);

        switch (side)
        {
            case 0: // left
                return new Vector2(0f, ry);
            case 1: // right
                return new Vector2(width - 1, ry);
            case 2: // top
                return new Vector2(rx, height - 1);
            default: // bottom
                return new Vector2(rx, 0f);
        }
    }

    void RasterizePathToMask(Vector2[] pathPoints)
    {
        // ... (This function is unchanged) ...
        float pathRadius = pathWidthPixels / 2f;
        int blendRadiusPixels = Mathf.Max(1, Mathf.RoundToInt(pathRadius * pathBlendFactor)); 
        
        int w = width;
        int h = height;
        
        // This is the squared radius of the *full blend zone*
        float blendRadiusSq = blendRadiusPixels * blendRadiusPixels; 
        
        // The inner radius where the path is fully flat
        float innerRadiusSq = pathRadius * pathRadius;

        // Iterate over a larger bounding box around each point to affect more pixels
        foreach (Vector2 point in pathPoints)
        {
            int px = Mathf.RoundToInt(point.x);
            int py = Mathf.RoundToInt(point.y);

            int x0 = Mathf.Max(0, px - blendRadiusPixels);
            int x1 = Mathf.Min(w - 1, px + blendRadiusPixels);
            int y0 = Mathf.Max(0, py - blendRadiusPixels);
            int y1 = Mathf.Min(h - 1, py + blendRadiusPixels);

            for (int x = x0; x <= x1; x++)
            {
                for (int y = y0; y <= y1; y++)
                {
                    float dx = x - point.x;
                    float dy = y - point.y;
                    float distSq = dx * dx + dy * dy;

                    // If within the full blend radius
                    if (distSq <= blendRadiusSq)
                    {
                        float blendValue = 0f;
                        if (distSq <= innerRadiusSq) 
                        {
                            // Inside the core path, full blend (flat)
                            blendValue = 1f;
                        }
                        else
                        {
                            // In the blending zone, calculate blend based on distance
                            // 0 at outer edge, 1 at inner edge
                            blendValue = 1f - Mathf.InverseLerp(innerRadiusSq, blendRadiusSq, distSq);
                        }
                        
                        // We take the MAX blend value if multiple path points overlap,
                        // ensuring the "flattest" effect is chosen.
                        if (blendValue > pathMask[x, y])
                        {
                            pathMask[x, y] = blendValue;
                        }
                    }
                }
            }
        }
    }
}