using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using SpacetimeDB.Types;

/// <summary>
/// Controller for robot entities in the simulation
/// </summary>
public class RobotController : MonoBehaviour
{
    [Header("Robot Configuration")]
    [SerializeField] private string robotName = "Robot";
    [SerializeField] private string robotType = "StandardRobot";
    [SerializeField] private ulong robotId;
    [SerializeField] private ulong controllerCodeId;
    
    [Header("Movement")]
    [SerializeField] private float maxSpeed = 5f;
    [SerializeField] private float rotationSpeed = 180f;
    [SerializeField] private Transform leftWheelTransform;
    [SerializeField] private Transform rightWheelTransform;
    [SerializeField] private float wheelRotationSpeed = 360f;
    
    [Header("Components")]
    [SerializeField] private WebViewDisplay faceDisplay;
    [SerializeField] private List<GameObject> ledObjects = new List<GameObject>();
    
    [Header("Debug")]
    [SerializeField] private bool showDebugGizmos = true;
    
    // Current motor values
    private float leftMotorSpeed = 0f;
    private float rightMotorSpeed = 0f;
    private bool ledState = false;
    
    // Cached components
    private Rigidbody robotRigidbody;
    
    // SpacetimeDB reference
    private SpacetimeDBManager spacetimeDBManager;
    
    private void Awake()
    {
        robotRigidbody = GetComponent<Rigidbody>();
    }
    
    private void Start()
    {
        // Get the SpacetimeDB manager
        spacetimeDBManager = SpacetimeDBManager.Instance;
        
        // Subscribe to state updates
        if (spacetimeDBManager != null)
        {
            spacetimeDBManager.OnConnected += HandleSpacetimeConnected;
            
            if (spacetimeDBManager.IsConnected)
            {
                SubscribeToStateUpdates();
            }
        }
        
        // Initialize LED state
        SetLEDState(ledState);
    }
    
    private void OnDestroy()
    {
        if (spacetimeDBManager != null)
        {
            spacetimeDBManager.OnConnected -= HandleSpacetimeConnected;
        }
    }
    
    private void FixedUpdate()
    {
        // Apply motor forces
        ApplyMotorForces();
        
        // Rotate wheels visually
        RotateWheels();
        
        // Update position in SpacetimeDB
        UpdatePositionInSpacetimeDB();
    }
    
    private void OnDrawGizmos()
    {
        if (!showDebugGizmos)
        {
            return;
        }
        
        // Draw motor force indicators
        if (Application.isPlaying)
        {
            Gizmos.color = Color.blue;
            Vector3 leftForce = transform.forward * leftMotorSpeed * maxSpeed;
            Vector3 rightForce = transform.forward * rightMotorSpeed * maxSpeed;
            
            // Left motor
            if (leftWheelTransform != null)
            {
                Gizmos.DrawLine(leftWheelTransform.position, leftWheelTransform.position + leftForce);
                Gizmos.DrawSphere(leftWheelTransform.position + leftForce, 0.1f);
            }
            
            // Right motor
            if (rightWheelTransform != null)
            {
                Gizmos.DrawLine(rightWheelTransform.position, rightWheelTransform.position + rightForce);
                Gizmos.DrawSphere(rightWheelTransform.position + rightForce, 0.1f);
            }
        }
    }
    
    /// <summary>
    /// Handle SpacetimeDB connection established
    /// </summary>
    private void HandleSpacetimeConnected()
    {
        SubscribeToStateUpdates();
    }
    
    /// <summary>
    /// Subscribe to microprocess state updates
    /// </summary>
    private void SubscribeToStateUpdates()
    {
        // Subscribe to state table events
        // (We'll integrate this with the actual SpacetimeDB implementation later)
    }
    
    /// <summary>
    /// Apply forces based on motor speeds
    /// </summary>
    private void ApplyMotorForces()
    {
        if (robotRigidbody == null)
        {
            return;
        }
        
        // Calculate movement
        float forwardSpeed = (leftMotorSpeed + rightMotorSpeed) * 0.5f * maxSpeed;
        float turnSpeed = (rightMotorSpeed - leftMotorSpeed) * rotationSpeed;
        
        // Apply forward force
        Vector3 forwardForce = transform.forward * forwardSpeed;
        robotRigidbody.AddForce(forwardForce, ForceMode.Force);
        
        // Apply rotation
        robotRigidbody.AddTorque(transform.up * turnSpeed, ForceMode.Force);
        
        // Limit velocity
        if (robotRigidbody.velocity.magnitude > maxSpeed)
        {
            robotRigidbody.velocity = robotRigidbody.velocity.normalized * maxSpeed;
        }
    }
    
    /// <summary>
    /// Rotate wheels based on motor speeds
    /// </summary>
    private void RotateWheels()
    {
        if (leftWheelTransform != null)
        {
            leftWheelTransform.Rotate(Vector3.right, leftMotorSpeed * wheelRotationSpeed * Time.deltaTime);
        }
        
        if (rightWheelTransform != null)
        {
            rightWheelTransform.Rotate(Vector3.right, rightMotorSpeed * wheelRotationSpeed * Time.deltaTime);
        }
    }
    
    /// <summary>
    /// Update the robot's position in SpacetimeDB
    /// </summary>
    private void UpdatePositionInSpacetimeDB()
    {
        if (spacetimeDBManager == null || !spacetimeDBManager.IsConnected || robotId == 0)
        {
            return;
        }
        
        // Update position in SpacetimeDB
        spacetimeDBManager.UpdateRobotPosition(robotId, transform.position, transform.eulerAngles);
    }
    
    /// <summary>
    /// Set the left motor speed
    /// </summary>
    public void SetLeftMotorSpeed(float speed)
    {
        leftMotorSpeed = Mathf.Clamp(speed, -1f, 1f);
    }
    
    /// <summary>
    /// Set the right motor speed
    /// </summary>
    public void SetRightMotorSpeed(float speed)
    {
        rightMotorSpeed = Mathf.Clamp(speed, -1f, 1f);
    }
    
    /// <summary>
    /// Set both motor speeds
    /// </summary>
    public void SetMotorSpeeds(float leftSpeed, float rightSpeed)
    {
        SetLeftMotorSpeed(leftSpeed);
        SetRightMotorSpeed(rightSpeed);
    }
    
    /// <summary>
    /// Set the LED state
    /// </summary>
    public void SetLEDState(bool state)
    {
        ledState = state;
        
        // Update LED objects
        foreach (GameObject ledObject in ledObjects)
        {
            if (ledObject != null)
            {
                ledObject.SetActive(ledState);
                
                // If the LED has a light component, update it
                Light light = ledObject.GetComponent<Light>();
                if (light != null)
                {
                    light.enabled = ledState;
                }
            }
        }
    }
    
    /// <summary>
    /// Load the face display
    /// </summary>
    public void LoadFaceDisplay(string url)
    {
        if (faceDisplay != null)
        {
            faceDisplay.LoadURL(url);
        }
    }
    
    /// <summary>
    /// Register the robot with SpacetimeDB
    /// </summary>
    public void RegisterWithSpacetimeDB()
    {
        if (spacetimeDBManager == null || !spacetimeDBManager.IsConnected)
        {
            Debug.LogWarning("Cannot register robot: SpacetimeDBManager not available or not connected");
            return;
        }
        
        // TODO: Call the appropriate reducer to register the robot
    }
    
    /// <summary>
    /// Handle microprocess state update
    /// </summary>
    public void HandleMicroprocessStateUpdate(float leftSpeed, float rightSpeed, string errorMessage, bool ledOn)
    {
        // Update motor speeds
        SetMotorSpeeds(leftSpeed, rightSpeed);
        
        // Update LED state
        SetLEDState(ledOn);
        
        // Handle error message
        if (!string.IsNullOrEmpty(errorMessage))
        {
            Debug.LogError($"Robot {robotName} (ID: {robotId}) error: {errorMessage}");
        }
    }
}

/// <summary>
/// Interface for web view displays
/// </summary>
public interface WebViewDisplay
{
    void LoadURL(string url);
    void ExecuteJavaScript(string js);
}
