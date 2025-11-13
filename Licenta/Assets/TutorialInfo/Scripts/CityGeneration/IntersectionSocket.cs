using UnityEngine;

/// <summary>
/// A helper component you add to your intersection prefabs.
/// Place this on an empty GameObject to mark the "socket" or 
/// "connection point" where a procedural road mesh should attach.
/// </summary>
public class IntersectionSocket : MonoBehaviour
{
    // You could add more data here, like road width, lane count, etc.
    // For now, its Transform (position and rotation) is all we need.

    // A simple gizmo to see your sockets in the editor
    private void OnDrawGizmos()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, 1f);
        Gizmos.DrawLine(transform.position, transform.position + transform.forward * 3f);
    }
}