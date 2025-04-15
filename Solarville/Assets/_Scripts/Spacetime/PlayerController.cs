using System;
using SpacetimeDB.Types;
using UnityEngine;

namespace Solarville.Spacetime
{
    [RequireComponent(typeof(CharacterController))]
    public class PlayerController : MonoBehaviour
    {
        [Header("Movement Settings")]
        [SerializeField] private float moveSpeed = 5f;
        [SerializeField] private float rotationSpeed = 120f;
        [SerializeField] private float jumpForce = 8f;
        [SerializeField] private float gravity = 20f;

        [Header("Network Settings")]
        [SerializeField] private float positionUpdateFrequency = 0.1f; // Update position every 100ms
        [SerializeField] private Transform playerModel;
        [SerializeField] private MeshRenderer playerModelRenderer;

        // State
        private CharacterController _characterController;
        private Vector3 _moveDirection = Vector3.zero;
        private float _lastPositionUpdateTimestamp;
        private uint _playerId;
        private string _playerName;
        private Vector3 _targetPosition;
        private Vector3 _targetRotation;
        private bool _isInitialized = false;
        private float _interpolationFactor = 10f;

        // Remote player interpolation
        private Vector3 _lastSyncedPosition;
        private Vector3 _lastSyncedRotation;
        private float _syncTime;
        private float _syncInterval = 0.1f; // Same as update frequency

        // Property to check if this is the local player
        public bool IsLocalPlayer { get; private set; }
        public uint PlayerId => _playerId;
        public string PlayerName => _playerName;

        private void Awake()
        {
            _characterController = GetComponent<CharacterController>();
        }

        public void Initialize(Player player)
        {
            _playerId = player.PlayerId;
            _playerName = player.Name;
            
            // Check if this is the local player
            IsLocalPlayer = player.Identity == GameManager.LocalIdentity;

            // Set initial position and rotation
            var position = new Vector3(player.Position.X, player.Position.Y, player.Position.Z);
            var rotation = new Vector3(player.Rotation.X, player.Rotation.Y, player.Rotation.Z);
            
            transform.position = position;
            transform.eulerAngles = rotation;
            
            _targetPosition = position;
            _targetRotation = rotation;
            _lastSyncedPosition = position;
            _lastSyncedRotation = rotation;

            // Set different material color for local vs remote players
            if (playerModelRenderer != null)
            {
                playerModelRenderer.material.color = IsLocalPlayer ? Color.green : Color.blue;
            }

            _isInitialized = true;
        }

        private void Update()
        {
            if (!_isInitialized || !GameManager.IsConnected())
                return;

            if (IsLocalPlayer)
            {
                HandleLocalPlayerMovement();
                SendPositionUpdate();
            }
            else
            {
                // Interpolate remote player
                InterpolateRemotePlayer();
            }
        }

        private void HandleLocalPlayerMovement()
        {
            // Calculate movement direction
            float horizontalInput = Input.GetAxis("Horizontal");
            float verticalInput = Input.GetAxis("Vertical");

            // Move relative to camera's forward direction
            Vector3 forward = Camera.main.transform.forward;
            Vector3 right = Camera.main.transform.right;
            forward.y = 0;
            right.y = 0;
            forward.Normalize();
            right.Normalize();

            Vector3 desiredMoveDirection = forward * verticalInput + right * horizontalInput;
            
            // Set target rotation if we're moving
            if (desiredMoveDirection.magnitude > 0.1f)
            {
                transform.rotation = Quaternion.Slerp(
                    transform.rotation,
                    Quaternion.LookRotation(desiredMoveDirection),
                    Time.deltaTime * rotationSpeed
                );
            }

            // Apply movement
            _moveDirection.x = desiredMoveDirection.x * moveSpeed;
            _moveDirection.z = desiredMoveDirection.z * moveSpeed;
            
            // Apply gravity and jumping
            if (_characterController.isGrounded)
            {
                _moveDirection.y = -0.5f; // Stick to ground
                
                if (Input.GetButtonDown("Jump"))
                {
                    _moveDirection.y = jumpForce;
                }
            }
            else
            {
                _moveDirection.y -= gravity * Time.deltaTime;
            }

            _characterController.Move(_moveDirection * Time.deltaTime);
        }

        private void SendPositionUpdate()
        {
            if (Time.time - _lastPositionUpdateTimestamp >= positionUpdateFrequency)
            {
                _lastPositionUpdateTimestamp = Time.time;

                // Convert Unity Vector3 to DbVector3
                var dbPosition = new DbVector3 { 
                    X = transform.position.x, 
                    Y = transform.position.y, 
                    Z = transform.position.z 
                };
                
                var dbRotation = new DbVector3 { 
                    X = transform.eulerAngles.x, 
                    Y = transform.eulerAngles.y, 
                    Z = transform.eulerAngles.z 
                };

                // Call the reducer to update our position
                try
                {
                    GameManager.Conn.Reducers.UpdatePlayerPosition(dbPosition, dbRotation);
                }
                catch (Exception e)
                {
                    Debug.LogError($"Failed to update position: {e.Message}");
                }
            }
        }

        private void InterpolateRemotePlayer()
        {
            // Smoothly move toward target position and rotation
            transform.position = Vector3.Lerp(transform.position, _targetPosition, Time.deltaTime * _interpolationFactor);
            transform.eulerAngles = Vector3.Lerp(transform.eulerAngles, _targetRotation, Time.deltaTime * _interpolationFactor);
        }

        public void UpdateFromDatabase(Player player)
        {
            if (!_isInitialized)
                return;

            _playerName = player.Name;

            // Update target position and rotation for interpolation
            _targetPosition = new Vector3(player.Position.X, player.Position.Y, player.Position.Z);
            _targetRotation = new Vector3(player.Rotation.X, player.Rotation.Y, player.Rotation.Z);
            
            _lastSyncedPosition = _targetPosition;
            _lastSyncedRotation = _targetRotation;
            _syncTime = Time.time;
        }

        private void OnGUI()
        {
            if (!_isInitialized)
                return;

            // Draw name tag above player
            Vector3 screenPos = Camera.main.WorldToScreenPoint(transform.position + Vector3.up * 2);
            if (screenPos.z > 0)
            {
                GUI.color = IsLocalPlayer ? Color.green : Color.white;
                GUI.Label(new Rect(screenPos.x - 50, Screen.height - screenPos.y, 100, 20), _playerName);
            }
        }
    }
}
