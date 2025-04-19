using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using SpacetimeDB.Types;

namespace Solarville.Spacetime
{
    /// <summary>
    /// Handles the movement and control of a vehicle based on motor inputs from a microprocessor
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    public class VehicleController : MonoBehaviour
    {
        [Header("Vehicle Settings")]
        [SerializeField] private float maxSpeed = 10f;
        [SerializeField] private float accelerationForce = 10f;
        [SerializeField] private float turnSpeed = 180f;
        [SerializeField] private float groundCheckDistance = 0.3f;

        [Header("Wheel Visualization")]
        [SerializeField] private Transform leftWheel;
        [SerializeField] private Transform rightWheel;
        [SerializeField] private float wheelRotationSpeed = 500f;

        // State
        private float leftMotorSpeed = 0f;
        private float rightMotorSpeed = 0f;
        private MicroprocessState currentState;
        private Rigidbody rb;
        private bool isGrounded = true;
        
        // Cached values for wheel rotation animation
        private float leftWheelRotation = 0f;
        private float rightWheelRotation = 0f;

        // Properties
        public MicroprocessState CurrentState => currentState;
        public float LeftMotorSpeed => leftMotorSpeed;
        public float RightMotorSpeed => rightMotorSpeed;
        public bool IsGrounded => isGrounded;

        private void Awake()
        {
            rb = GetComponent<Rigidbody>();
        }

        private void Update()
        {
            CheckGrounded();
            AnimateWheels();
        }

        private void FixedUpdate()
        {
            if (isGrounded)
            {
                ApplyMotorForces();
            }
        }

        /// <summary>
        /// Updates the motor speeds based on the new microprocessor state
        /// </summary>
        public void UpdateFromMicroprocessState(MicroprocessState state)
        {
            currentState = state;
            leftMotorSpeed = state.LeftMotorSpeed;
            rightMotorSpeed = state.RightMotorSpeed;
        }

        /// <summary>
        /// Applies forces to the rigidbody based on the current motor speeds
        /// </summary>
        private void ApplyMotorForces()
        {
            // Calculate forward force from both motors
            float forwardForce = (leftMotorSpeed + rightMotorSpeed) * 0.5f * accelerationForce;
            
            // Calculate turning force from difference in motor speeds
            float turnForce = (rightMotorSpeed - leftMotorSpeed) * turnSpeed;
            
            // Apply forces
            Vector3 forwardVector = transform.forward * forwardForce;
            rb.AddForce(forwardVector, ForceMode.Force);
            
            // Apply torque for turning
            rb.AddTorque(transform.up * turnForce, ForceMode.Force);
            
            // Limit maximum velocity
            if (rb.velocity.magnitude > maxSpeed)
            {
                rb.velocity = rb.velocity.normalized * maxSpeed;
            }
        }

        /// <summary>
        /// Checks if the vehicle is grounded
        /// </summary>
        private void CheckGrounded()
        {
            isGrounded = Physics.Raycast(transform.position, -transform.up, groundCheckDistance);
        }

        /// <summary>
        /// Animates the wheel meshes based on current motor speeds
        /// </summary>
        private void AnimateWheels()
        {
            if (leftWheel != null)
            {
                leftWheelRotation += leftMotorSpeed * wheelRotationSpeed * Time.deltaTime;
                leftWheel.localRotation = Quaternion.Euler(leftWheelRotation, 0, 0);
            }
            
            if (rightWheel != null)
            {
                rightWheelRotation += rightMotorSpeed * wheelRotationSpeed * Time.deltaTime;
                rightWheel.localRotation = Quaternion.Euler(rightWheelRotation, 0, 0);
            }
        }

        /// <summary>
        /// Apply an immediate force to the vehicle (for testing or external events)
        /// </summary>
        public void ApplyImpulse(Vector3 force)
        {
            rb.AddForce(force, ForceMode.Impulse);
        }

        /// <summary>
        /// Reset the vehicle to the given position and rotation
        /// </summary>
        public void ResetVehicle(Vector3 position, Quaternion rotation)
        {
            rb.velocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            transform.position = position;
            transform.rotation = rotation;
        }
    }
}