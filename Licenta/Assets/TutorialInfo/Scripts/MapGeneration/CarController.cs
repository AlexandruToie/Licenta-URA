using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class CarController : MonoBehaviour
{
    [Header("Movement Settings")]
    public float maxSpeed = 8f;
    public float rotationSpeed = 5f;
    public float laneOffset = 0.35f;

    [Header("Sensor Array (Arc de Cerc)")]
    [Tooltip("The distance of the central sensor.")]
    public float detectionDistance = 2.5f;
    [Tooltip("The angle (in degrees) for the left and right sensors.")]
    public float sensorAngle = 20f;
    [Tooltip("The height of the sensors above the ground.")]
    public float sensorHeight = 0.2f;
    
    [Header("Traffic Logic")]
    [Tooltip("The maximum time a car can stay still before despawning.")]
    public float stuckTimeout = 3.0f; 
    public LayerMask obstacleLayer;

    private TrafficNode currentNode;
    private TrafficNode targetNode;
    private TrafficNode previousNode;

    private Vector3 targetPosition;
    private bool isStopped = false;
    private float currentSpeed;
    private float timeStayedStill = 0f;

    [Header("Smoothness")]
    [Tooltip("The distance at which the car starts to turn towards the next point (curve cutting).")]
    public float cornerCutDistance = 1.0f;

    [Header("Visual Effects")]
    public ParticleSystem exhaustParticles;

    public void Setup(TrafficNode startNode) // Called right after instantiation
    {
        currentNode = startNode;
        transform.position = startNode.WorldPosition; // Initial position
        
        previousNode = null;
        SetNextDestination();

        if (targetNode != null)
        {
            CalculateTargetPosition();
            Vector3 dir = (targetPosition - transform.position).normalized; //Direction to the target
            if (dir != Vector3.zero)
            {
                transform.rotation = Quaternion.LookRotation(dir);
                Vector3 right = Vector3.Cross(Vector3.up, dir).normalized;
                transform.position = currentNode.WorldPosition + (right * laneOffset);
            }
        }
    }

    void Update()
    {
        if (currentNode == null || targetNode == null) return;

        HandleSensorArray();
        MoveAndRotate();
        CheckDespawnCondition();
        HandleExhaust();
    }

    void HandleExhaust()
    {
        if (exhaustParticles == null) return;

        if (currentSpeed > 0.1f && !isStopped)
        {
            if (!exhaustParticles.isEmitting) 
            {
                exhaustParticles.Play();
            }
        }
        else
        {
            if (exhaustParticles.isEmitting)
            {
                exhaustParticles.Stop(); 
            }
        }
    }
    void HandleSensorArray() //The 3-ray sensor array
    {
        Vector3 sensorOrigin = transform.position + Vector3.up * sensorHeight;
        bool obstacleDetected = false;
        float distToObstacle = float.MaxValue;
        Vector3 forward = transform.forward;
        Vector3 leftDir = Quaternion.Euler(0, -sensorAngle, 0) * forward;
        Vector3 rightDir = Quaternion.Euler(0, sensorAngle, 0) * forward;

        if (CastRay(sensorOrigin, forward, detectionDistance, out float d1)) 
        { 
            obstacleDetected = true; 
            if(d1 < distToObstacle) distToObstacle = d1;
        }
        if (CastRay(sensorOrigin, leftDir, detectionDistance * 0.7f, out float d2)) // Lateralul e puțin mai scurt
        { 
            obstacleDetected = true; 
            if(d2 < distToObstacle) distToObstacle = d2;
        }
        if (CastRay(sensorOrigin, rightDir, detectionDistance * 0.7f, out float d3)) 
        { 
            obstacleDetected = true; 
            if(d3 < distToObstacle) distToObstacle = d3;
        }

        if (obstacleDetected)
        {
            isStopped = true;

            if (distToObstacle < 1.0f)
            {
                currentSpeed = 0f; //Forced stop
            }
            else
            {
                currentSpeed = Mathf.Lerp(currentSpeed, 0f, Time.deltaTime * 8f); //Slow down smoothly
            }
        }
        else
        {
            isStopped = false;
            currentSpeed = Mathf.Lerp(currentSpeed, maxSpeed, Time.deltaTime * 3f); //Accelerate smoothly
        }
    }
    bool CastRay(Vector3 origin, Vector3 dir, float length, out float hitDist)
    {
        hitDist = float.MaxValue;
        
        if (Physics.Raycast(origin, dir, out RaycastHit hit, length, obstacleLayer))
        {
            Debug.DrawRay(origin, dir * length, Color.red);
            hitDist = hit.distance;
            return true;
        }
        
        Debug.DrawRay(origin, dir * length, Color.green);
        return false;
    }

    void MoveAndRotate()
    {
        float distanceToTarget = Vector3.Distance(transform.position, targetPosition); //1. Movment

        if (distanceToTarget < cornerCutDistance) //2. Arrived at Node
        {
            ArrivedAtNode();
            return;
        }

        transform.position = Vector3.MoveTowards(transform.position, targetPosition, currentSpeed * Time.deltaTime);

        //3. Rotation
        Vector3 directionToTarget = (targetPosition - transform.position).normalized;
        Vector3 flatDirection = new Vector3(directionToTarget.x, 0, directionToTarget.z).normalized;

        if (flatDirection.magnitude > 0.1f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(flatDirection); // Desired rotation
            float dynamicRotSpeed = rotationSpeed;
            if (distanceToTarget < 2f) dynamicRotSpeed *= 2f; 

            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, dynamicRotSpeed * Time.deltaTime); // Smooth rotation
        }
    }

    void ArrivedAtNode() // Called when the car reaches the target node
    {
        previousNode = currentNode;
        currentNode = targetNode;
        SetNextDestination();
    }

    void SetNextDestination() // Decides the next node to go to
    {
        if (currentNode.Neighbors.Count == 0)
        {
            Destroy(gameObject); 
            return;
        }

        // 1. We filter out the previous node
        List<TrafficNode> validNeighbors = new List<TrafficNode>();

        foreach (var neighbor in currentNode.Neighbors)
        {
            // Add a new neighbor only if it's not the previous node
            if (neighbor != previousNode)
            {
                validNeighbors.Add(neighbor);
            }
        }

        // 2. We make the decision
        if (validNeighbors.Count > 0)
        {
            // We have a path ahead (or left/right)! Choose one at random.
            targetNode = validNeighbors[Random.Range(0, validNeighbors.Count)];
        }
        else
        {
            // Dead end (only possible path is backwards), so we go back.
            Destroy(gameObject);
            return; 
        }

        CalculateTargetPosition();
    }
    void CalculateTargetPosition() // Calculates the exact position on the road with lane offset
    {
        Vector3 roadDirection = (targetNode.WorldPosition - currentNode.WorldPosition).normalized;
        Vector3 rightSide = Vector3.Cross(Vector3.up, roadDirection).normalized;

        Vector3 rawPos = targetNode.WorldPosition + (rightSide * laneOffset);
        targetPosition = new Vector3(rawPos.x, rawPos.y, rawPos.z);
    }

    void CheckDespawnCondition() // Despawns the car if stuck for too long
    {
        if (isStopped)
        {
            timeStayedStill += Time.deltaTime;
            if (timeStayedStill > stuckTimeout)
            {
                Destroy(gameObject);
            }
        }
        else
        {
            timeStayedStill = 0f;
        }
    }

    void Start()
    {
        StartCoroutine(SensorRoutine());
    }

    IEnumerator SensorRoutine()
    {
        float randomStart = Random.Range(0f, 0.2f);
        yield return new WaitForSeconds(randomStart);

        while (true)
        {
            HandleSensorArray();
            yield return new WaitForSeconds(0.1f); 
        }
    }
}