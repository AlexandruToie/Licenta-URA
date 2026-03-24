using UnityEngine;

[RequireComponent(typeof(Camera))] // Scriptul cere obligatoriu o componentă Cameră
public class UnityLikeCamera : MonoBehaviour
{
    [Header("References")]
    public Terrain targetTerrain;

    [Header("Movement Settings")]
    public float moveSpeed = 50f;       
    public float sprintSpeed = 150f;    
    public float movementSmoothness = 10f; 

    [Header("Rotation Settings")]
    public float mouseSensitivity = 2f;
    public bool flattenMovement = false; 

    [Header("Zoom Settings (FOV)")]
    [Tooltip("How fast the zoom changes.")]
    public float zoomSensitivity = 10f;
    
    [Tooltip("Maximum FOV.")]
    public float maxFOV = 60f;

    [Tooltip("Minimum FOV.")]
    [Range(15f, 60f)]
    public float minFOV = 15f;

    [Header("World Limits")]
    public float minHeight = 5f;
    public float maxHeight = 250f;

    private Vector3 targetPosition;
    private float rotationX = 0f; 
    private float rotationY = 0f; 
    
    private Camera cam;
    private float targetFOV;

    private float minX, maxX, minZ, maxZ;

    void Start()
    {
        // Setup Movement
        targetPosition = transform.position;
        Vector3 rot = transform.localEulerAngles;
        rotationY = rot.y;
        rotationX = rot.x;

        // Setup Zoom
        cam = GetComponent<Camera>();
        cam.fieldOfView = maxFOV;
        targetFOV = maxFOV;

        CalculateMapLimits();
    }

    void Update()
    {
        HandleRotation();
        HandleMovement();
        HandleZoomFOV();
        ApplyTransform();
    }

    void HandleRotation()
    {
        if (Input.GetMouseButton(1))
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;

            rotationY += Input.GetAxis("Mouse X") * mouseSensitivity;
            rotationX -= Input.GetAxis("Mouse Y") * mouseSensitivity;
            rotationX = Mathf.Clamp(rotationX, -89f, 89f);

            transform.localEulerAngles = new Vector3(rotationX, rotationY, 0);
        }
        else
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
    }

    void HandleMovement()
    {
        float speed = Input.GetKey(KeyCode.LeftShift) ? sprintSpeed : moveSpeed;
        Vector3 inputDir = new Vector3(Input.GetAxis("Horizontal"), 0, Input.GetAxis("Vertical"));

        if (inputDir.magnitude > 0.01f)
        {
            Vector3 forward = transform.forward;
            Vector3 right = transform.right;

            if (flattenMovement)
            {
                forward.y = 0; forward.Normalize();
                right.y = 0; right.Normalize();
            }

            Vector3 moveDir = (forward * inputDir.z + right * inputDir.x).normalized;
            targetPosition += moveDir * speed * Time.deltaTime;
        }

        if (Input.GetKey(KeyCode.E)) targetPosition += Vector3.up * speed * Time.deltaTime;
        if (Input.GetKey(KeyCode.Q)) targetPosition -= Vector3.up * speed * Time.deltaTime;
        
        EnforceLimits();
    }

    void HandleZoomFOV()
    {
        float scroll = Input.GetAxis("Mouse ScrollWheel");
        
        if (Mathf.Abs(scroll) > 0.001f)
        {
            targetFOV -= scroll * zoomSensitivity * 500f * Time.deltaTime;
        }

        float actualMinFOV = Mathf.Max(20f, minFOV);

        targetFOV = Mathf.Clamp(targetFOV, actualMinFOV, maxFOV);

        cam.fieldOfView = Mathf.Lerp(cam.fieldOfView, targetFOV, Time.deltaTime * movementSmoothness);
    }

    void EnforceLimits()
    {
        if (targetTerrain != null)
        {
            targetPosition.x = Mathf.Clamp(targetPosition.x, minX, maxX);
            targetPosition.z = Mathf.Clamp(targetPosition.z, minZ, maxZ);
        }
        targetPosition.y = Mathf.Clamp(targetPosition.y, minHeight, maxHeight);
    }

    void ApplyTransform()
    {
        transform.position = Vector3.Lerp(transform.position, targetPosition, Time.deltaTime * movementSmoothness);
    }

    void CalculateMapLimits()
    {
        if (targetTerrain != null)
        {
            Vector3 tPos = targetTerrain.transform.position;
            Vector3 tSize = targetTerrain.terrainData.size;
            minX = tPos.x; minZ = tPos.z;
            maxX = tPos.x + tSize.x; maxZ = tPos.z + tSize.z;
        }
    }

    private void OnValidate()
    {
        if (minFOV < 20f) minFOV = 20f;
        if (maxFOV < minFOV) maxFOV = minFOV;
    }
}