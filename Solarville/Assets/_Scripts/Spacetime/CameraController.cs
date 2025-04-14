using UnityEngine;

namespace Solarville.Spacetime
{
    /// <summary>
    /// Controls the camera following the player
    /// </summary>
    public class CameraController : MonoBehaviour
    {
        [Header("Target")]
        [SerializeField] private Transform target;
        
        [Header("Position Settings")]
        [SerializeField] private Vector3 offset = new Vector3(0, 2, -5);
        [SerializeField] private float smoothSpeed = 10f;
        [SerializeField] private float lookAtOffset = 1f;
        
        [Header("Rotation Settings")]
        [SerializeField] private bool enableRotation = true;
        [SerializeField] private float rotationSensitivity = 3f;
        [SerializeField] private float minVerticalAngle = -30f;
        [SerializeField] private float maxVerticalAngle = 60f;
        
        private Vector3 _currentRotation;
        private Vector3 _smoothVelocity = Vector3.zero;
        private float _rotX = 0f;
        private float _rotY = 0f;

        private void Start()
        {
            // Initialize rotation
            _rotY = transform.eulerAngles.y;
            _rotX = transform.eulerAngles.x;
            
            // Find local player if target is not set
            if (target == null)
            {
                FindLocalPlayer();
                
                // Register for connection events to find the player when connected
                GameManager.OnSubscriptionApplied += OnSubscriptionApplied;
            }
        }

        private void OnDestroy()
        {
            GameManager.OnSubscriptionApplied -= OnSubscriptionApplied;
        }

        private void OnSubscriptionApplied()
        {
            FindLocalPlayer();
        }

        private void FindLocalPlayer()
        {
            // Find the local player controller
            foreach (var player in GameManager.Players.Values)
            {
                if (player.IsLocalPlayer)
                {
                    target = player.transform;
                    break;
                }
            }
        }

        private void LateUpdate()
        {
            if (target == null)
            {
                FindLocalPlayer();
                return;
            }

            // Handle camera rotation with mouse input
            if (enableRotation)
            {
                _rotY += Input.GetAxis("Mouse X") * rotationSensitivity;
                _rotX -= Input.GetAxis("Mouse Y") * rotationSensitivity;
                _rotX = Mathf.Clamp(_rotX, minVerticalAngle, maxVerticalAngle);
                
                _currentRotation = new Vector3(_rotX, _rotY, 0);
                Quaternion rotation = Quaternion.Euler(_currentRotation);
                
                // Calculate desired position based on rotation and offset
                Vector3 desiredPosition = target.position + rotation * offset;
                transform.position = Vector3.SmoothDamp(transform.position, desiredPosition, ref _smoothVelocity, 1f / smoothSpeed);
                
                // Look at the target
                Vector3 lookAtPosition = target.position + Vector3.up * lookAtOffset;
                transform.LookAt(lookAtPosition);
            }
            else
            {
                // Simple follow without rotation control
                Vector3 desiredPosition = target.position + offset;
                transform.position = Vector3.Lerp(transform.position, desiredPosition, Time.deltaTime * smoothSpeed);
                
                // Look at the target
                Vector3 lookAtPosition = target.position + Vector3.up * lookAtOffset;
                transform.LookAt(lookAtPosition);
            }
        }
    }
}
