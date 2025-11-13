using UnityEngine;
using UnityEngine.Splines; // Make sure you've installed the Spline package
using System.Collections.Generic;

/// <summary>
/// A static helper class that implements the procedural mesh extrusion
/// and UV mapping logic described in Part III of the design doc.
/// </summary>
public static class RoadMeshGenerator
{
    /// <summary>
    /// Generates a procedural road mesh along a given spline.
    /// Implements logic from Part 3.2 (Extrusion) and 3.3 (UVs).
    /// </summary>
    /// <param name="spline">The Spline to build along.</param>
    /// <param name="roadWidth">How wide to make the road.</param>
    /// <param name="stepSize">World-space distance between mesh segments.</param>
    /// <param name="textureTileLength">How long (in meters) one texture tile is.</param>
    /// <returns>A new Mesh object.</returns>
    public static Mesh GenerateRoadMesh(Spline spline, float roadWidth, float stepSize, float textureTileLength)
    {
        List<Vector3> vertices = new List<Vector3>();
        List<Vector2> uvs = new List<Vector2>();
        List<int> triangles = new List<int>();

        float halfWidth = roadWidth / 2f;
        float currentArclength = 0f; // This is our 'v' UV coordinate
        Vector3 lastPoint = spline.EvaluatePosition(0);

        // Define the 2D cross-section of the road
        Vector2[] crossSection = new Vector2[]
        {
            new Vector2(-halfWidth, 0), // Left edge
            new Vector2(halfWidth, 0)  // Right edge
        };

        // Iterate along the spline's length, not its 't' value
        for (float dist = 0; dist <= spline.GetLength(); dist += stepSize)
        {
            // --- THIS IS THE FIXED LINE ---
            // Get the point, tangent, and up-vector at this distance
            // We use the older Evaluate signature that uses 'out' parameters
            spline.Evaluate(dist / spline.GetLength(), out var point, out var tangent, out var up);

            // Calculate the 'right' vector (binormal)
            Vector3 right = Vector3.Cross(tangent, up).normalized;

            // --- UV V-Coordinate (Arclength) ---
            // As described in Doc 3.3
            currentArclength += Vector3.Distance(lastPoint, (Vector3)point);
            lastPoint = (Vector3)point;
            float v = currentArclength / textureTileLength;

            // Add vertices for this cross-section
            for (int i = 0; i < crossSection.Length; i++)
            {
                // Generate the 3D vertex position
                Vector3 vert = (Vector3)point + (right * crossSection[i].x) + ((Vector3)up * crossSection[i].y);
                vertices.Add(vert);

                // --- UV U-Coordinate (Horizontal) ---
                // As described in Doc 3.3
                float u = (float)i / (crossSection.Length - 1);
                uvs.Add(new Vector2(u, v));
            }

            // --- Triangle Generation ---
            // Stitch this new ring of vertices to the previous one
            if (dist > 0)
            {
                int baseIndex = vertices.Count - (2 * crossSection.Length);
                int currentBaseIndex = vertices.Count - crossSection.Length;

                // This assumes a 2-point cross-section (a simple line)
                // It creates one quad
                int v0 = baseIndex;
                int v1 = currentBaseIndex;
                int v2 = baseIndex + 1;
                int v3 = currentBaseIndex + 1;

                // Triangle 1
                triangles.Add(v0);
                triangles.Add(v1);
                triangles.Add(v2);

                // Triangle 2
                triangles.Add(v2);
                triangles.Add(v1);
                triangles.Add(v3);
            }
        }

        // --- Create the final mesh ---
        Mesh mesh = new Mesh();
        mesh.vertices = vertices.ToArray();
        mesh.triangles = triangles.ToArray();
        mesh.uv = uvs.ToArray();
        mesh.RecalculateNormals(); // Let Unity handle normals for now
        mesh.RecalculateBounds();

        return mesh;
    }
}