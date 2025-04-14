using NWH.Common.Vehicles;
using System;
using UnityEngine;
using System.Collections.Generic;
using Unity.Collections;
using System.Linq;


#if UNITY_EDITOR
using UnityEditor;
using NWH.NUI;
#endif


namespace NWH.WheelController3D
{
    [DisallowMultipleComponent]
    [DefaultExecutionOrder(100)]
    public partial class WheelController : WheelUAPI
    {
        [Tooltip("    Instance of the spring.")]
        [SerializeField]
        public Spring spring = new Spring();

        [Tooltip("    Instance of the damper.")]
        [SerializeField]
        public Damper damper = new Damper();

        [Tooltip("    Instance of the wheel.")]
        [SerializeField]
        public Wheel wheel = new Wheel();

        [Tooltip("    Side (lateral) friction info.")]
        [SerializeField]
        public Friction sideFriction = new Friction();

        [Tooltip("    Forward (longitudinal) friction info.")]
        [SerializeField]
        public Friction forwardFriction = new Friction();

        /// <summary>
        ///     Contains data about the ground contact point. 
        ///     Not valid if !_isGrounded.
        /// </summary>
        [Tooltip("    Contains point in which wheel touches ground. Not valid if !_isGrounded.")]
        [NonSerialized]
        private WheelHit wheelHit;

        /// <summary>
        ///     Current active friction preset.
        /// </summary>
        [Tooltip("    Current active friction preset.")]
        [SerializeField]
        private FrictionPreset activeFrictionPreset;

        /// <summary>
        ///     Motor torque applied to the wheel in Nm.
        ///     Can be positive or negative.
        /// </summary>
        [Tooltip(
            "Motor torque applied to the wheel. Since NWH Vehicle Physics 2 the value is readonly and setting it will have no effect\r\nsince torque calculation is done inside powertrain solver.")]
        private float motorTorque;

        /// <summary>
        ///     Brake torque applied to the wheel in Nm.
        ///     Must be positive.
        /// </summary>
        [Tooltip("    Brake torque applied to the wheel in Nm.")]
        private float brakeTorque;

        /// <summary>
        ///     The amount of torque returned by the wheel.
        ///     Under no-slip conditions this will be equal to the torque that was input.
        ///     When there is wheel spin, the value will be less than the input torque.
        /// </summary>
        [Tooltip("    The amount of torque returned by the wheel.\r\n    Under perfect grip conditions this will be equal to the torque that was put down.\r\n    While in air the value will be equal to the source torque minus torque that is result of dW of the wheel.")]
        private float counterTorque;

        /// <summary>
        ///     Current steer angle of the wheel, in deg.
        /// </summary>
        [Tooltip("    Current steer angle of the wheel.")]
        private float steerAngle;

        /// <summary>
        /// Current camber value.
        /// </summary>
        [SerializeField]
        private float camber;

        /// <summary>
        ///     Tire load in Nm.
        /// </summary>
        [Tooltip("    Tire load in Nm.")]
        [NonSerialized]
        private float load;

        /// <summary>
        ///     Maximum load the tire is rated for in [N]. 
        ///     Used to calculate friction. Default value is adequate for most cars but 
        ///     larger and heavier vehicles such as semi trucks will use higher values.
        ///     A good rule of the thumb is that this value should be 2x the Load (Debug tab) 
        ///     while vehicle is stationary.
        /// </summary>
        [SerializeField]
        private float loadRating = 5400;

        /// <summary>
        ///     Amount of torque transferred from wheel to the chassis.
        /// </summary>
        [Range(-2, 2)]
        [Tooltip("Amount of torque transferred from wheel to the chassis.")]
        public float chassisTorqueCoefficient = 1.0f;

        /// <summary>
        ///     Constant torque acting similar to brake torque.
        ///     Imitates rolling resistance.
        /// </summary>
        [Range(0, 500)]
        [Tooltip("    Constant torque acting similar to brake torque.\r\n    Imitates rolling resistance.")]
        public float rollingResistanceTorque = 30f;

        /// <summary>
        /// Higher the number, higher the effect of longitudinal friction on lateral friction.
        /// If 1, when wheels are locked up or there is wheel spin it will be impossible to steer.
        /// If 0 doughnuts or power slides will be impossible.
        /// The 'accurate' value is 1 but might not be desirable for arcade games.
        /// </summary>
        [Tooltip("Higher the number, higher the effect of longitudinal friction on lateral friction.\r\n" +
            "If 1, when wheels are locked up or there is wheel spin it will be impossible to steer." +
            "\r\nIf 0 doughnuts or power slides will be impossible.\r\n" +
            "The 'accurate' value is 1 but might not be desirable for arcade games.")]
        [Range(0, 1)]
        [SerializeField]
        private float frictionCircleStrength = 1f;

        /// <summary>
        /// Stiffness of the friction circle. Higher shape value will have more sudden effect, but the effect will come into play with higher slip.
        /// Default is 3.
        /// </summary>
        [Range(1f, 5f)]
        [SerializeField]
        [Tooltip("Stiffness of the friction circle. Higher shape value will have more sudden effect, but the effect will come into play with higher slip.")]
        private float frictionCirclePower = 3f;

        /// <summary>
        ///     True if wheel touching ground.
        /// </summary>
        [Tooltip("    True if wheel touching ground.")]
        private bool _isGrounded;

        /// <summary>
        ///     Rigidbody to which the forces will be applied.
        /// </summary>
        [Tooltip("    Rigidbody to which the forces will be applied.")]
        [SerializeField]
        private Rigidbody targetRigidbody;

        /// <summary>
        /// Distance as a percentage of the max spring length. Value of 1 means that the friction force will
        /// be applied 1 max spring length above the contact point, and value of 0 means that it will be applied at the
        /// ground level. Value can be >1.
        /// Can be used instead of the anti-roll bar to prevent the vehicle from tipping over in corners
        /// and can be useful in low framerate applications where anti-roll bar might induce jitter.
        /// </summary>
        [Tooltip("Distance as a percentage of the max spring length. Value of 1 means that the friction force will\r\nbe applied 1 max spring length above the contact point, and value of 0 means that it will be applied at the\r\nground level. Value can be >1.\r\nCan be used instead of the anti-roll bar to prevent the vehicle from tipping over in corners\r\nand can be useful in low framerate applications where anti-roll bar might induce jitter.")]
        public float forceApplicationPointDistance = 0.8f;

        /// <summary>
        /// Disables the motion vectors on the wheel visual to prevent artefacts due to 
        /// the wheel rotation when using PostProcessing.
        /// </summary>
        [Tooltip("Disables the motion vectors on the wheel visual to prevent artefacts due to \r\nthe wheel rotation when using PostProcessing.")]
        public bool disableMotionVectors = true;

        /// <summary>
        /// The speed coefficient of the spring / suspension extension when not on the ground.
        /// wheel.perceivedPowertrainInertia.e. how fast the wheels extend when in the air.
        /// The setting of 1 will result in suspension fully extending in 1 second, 2 in 0.5s, 3 in 0.333s, etc.
        /// Recommended value is 6-10.
        /// </summary>
        [Range(0.0001f, 30f)]
        [Tooltip("The speed coefficient of the spring / suspension extension when not on the ground.\r\nwheel.perceivedPowertrainInertia.e. how fast the wheels extend when in the air.\r\nThe setting of 1 will result in suspension fully extending in 1 second, 2 in 0.5s, 3 in 0.333s, etc.\r\nRecommended value is 6-10.")]
        public float suspensionExtensionSpeedCoeff = 6f;

        /// <summary>
        /// The amount of wobble around the X-axis the wheel will have when fully damaged.
        /// Part of the damage visualization and does not affect handling.
        /// </summary>
        [Range(0f, 90f)]
        [Tooltip("The amount of wobble around the X-axis the wheel will have when fully damaged.\r\nPart of the damage visualization and does not affect handling.")]
        public float damageMaxWobbleAngle = 30f;

        /// <summary>
        /// Scales the forces applied to other Rigidbodies. Useful for interacting
        /// with lightweight objects and prevents them from flying away or glitching out.
        /// </summary>
        [Tooltip("Scales the forces applied to other Rigidbodies. Useful for interacting\r\nwith lightweight objects and prevents them from flying away or glitching out.")]
        public float otherBodyForceScale = 1f;

        /// <summary>
        /// Layers that will be detected by the wheel cast.
        /// </summary>
        [Tooltip("Layers that will be detected by the wheel cast.")]
        public LayerMask layerMask = 1 << 0;

        /// <summary>
        /// Layer the mesh collider of the wheel is on.
        /// </summary>
        [Tooltip("Layer the mesh collider of the wheel is on.")]
        public int meshColliderLayer = 2;

        /// <summary>
        /// Experimental!
        /// Uses contacts modification API to soften or ignore collisions in the wheel forward direction.
        /// </summary>
        [Tooltip("Experimental! Uses contacts modification API to soften or ignore collisions in the wheel forward direction.")]
        public bool useContactModification = true;

        /// <summary>
        /// Number of friction substeps per global physics step.
        /// Has low impact on performance but higher value can improve stability.
        /// </summary>
        [Tooltip("Number of friction substeps per global physics step. Has low impact on performance but higher value can improve stability.")]
        public int frictionSubsteps = 20;

        /// <summary>
        /// Stores vehicle colliders for cast filtering. 
        /// Updated each time Initialize() is called.
        /// Add colliders manually or call Initialize() after adding or removing colliders from the vehicle.
        /// </summary>
        public HashSet<Collider> vehicleColliders;

        /// <summary>
        /// Should the wheel update on FixedUpdate()?
        /// If false the update has to be manually called.
        /// </summary>
        private bool _autoSimulate = true;

        // Parent transforms
        private Matrix4x4 _parentWorldMatrix = Matrix4x4.identity;
        private Matrix4x4 _parentLocalMatrix = Matrix4x4.identity;

        // Suspension transforms
        private Matrix4x4 _initSuspensionLocalMatrix = Matrix4x4.identity;
        private Matrix4x4 _suspensionLocalMatrix = Matrix4x4.identity;
        private Matrix4x4 _suspensionWorldMatrix = Matrix4x4.identity;
        private Matrix4x4 _suspensionInvWorldMatrix = Matrix4x4.identity;
        private Vector3 _suspensionLocalPosition = Vector3.zero;
        private Vector3 _suspensionWorldPosition = Vector3.zero;
        private Quaternion _suspensionWorldRotation = Quaternion.identity;
        private Vector3 _suspensionUp = Vector3.up;
        private Vector3 _suspensionForward = Vector3.forward;
        private Vector3 _suspensionRight = Vector3.right;
        private Vector3 _suspensionLocalUp = Vector3.up;
        private Vector3 _suspensionLocalForward = Vector3.forward;
        private Vector3 _suspensionLocalRight = Vector3.right;

        // Wheel transforms
        private Matrix4x4 _wheelWorldMatrix = Matrix4x4.identity;
        private Vector3 _wheelWorldPosition = Vector3.zero;
        private Quaternion _wheelWorldRotation = Quaternion.identity;
        private Vector3 _wheelUp = Vector3.up;
        private Vector3 _wheelForward = Vector3.forward;
        private Vector3 _wheelRight = Vector3.right;

        private Quaternion steerRotationQuaternion = Quaternion.identity;
        private Matrix4x4 steerRotationMatrix = Matrix4x4.identity;

        private Vector3 _hitContactVelocity;
        private Vector3 _hitSurfaceVelocity;
        private Rigidbody _hitRigidbody;
        private Transform _parentTransform;
        private Vector3 _frictionForce;
        private Vector3 _suspensionForce;
        private float _damage;
        private bool _initialized;
        private float _dt;
        private float _invDt;
        private float _localAxleRotation;
        private float _localDamageRotation;
        private GroundDetectionBase _groundDetection;
        private WheelControllerManager _wheelControllerManager;
        private bool _lowSpeedReferenceIsSet;
        private Vector3 _lowSpeedReferencePosition;
        private Vector3 _zeroVector;
        private Vector3 _upVector;
        private Vector3 _castOrigin;
        private Vector3 _castDirection;
        private Vector3 _hitLocalPoint;
        private int _targetRigidbodyId;
        private bool _wakeOneFrame;


        private void Awake()
        {
            targetRigidbody = GetComponentInParent<Rigidbody>();
            Debug.Assert(targetRigidbody != null, $"Parent Rigidbody not found on {name}.");

            _zeroVector = Vector3.zero;
            _upVector = Vector3.up;
        }


        private void Start()
        {
            // Show a warning if Time.fixedDeltaTime is low
#if UNITY_EDITOR
            if (!SessionState.GetBool("WC3D_ShownDtWarning", false) && Time.fixedDeltaTime > 0.017f)
            {
                Debug.LogWarning($"Time.fixedDeltaTime of {Time.fixedDeltaTime} detected. Recommended value is 0.01 (100Hz) or lower for " +
                    $"best vehicle physics behaviour. On mobile games no higher than 0.02 (50Hz) is recommended. Higher physics update rate" +
                    $" results in higher physics fidelity.");
                SessionState.SetBool("WC3D_ShownDtWarning", true);
            }
#endif
            // Sets the defaults if needed
            SetRuntimeDefaultsIfNeeded();

            // Find (or spawn) the visual containers
            FindOrSpawnVisualContainers();

            // Find (or add) the wheel manager
            FindOrAddWheelControllerManager();

            // Initialize temp values
            Initialize();

            // Initialize spring length to starting value.
            if (spring.maxLength > 0)
            {
                spring.length = spring.maxLength * 0.7f;
                spring.prevLength = spring.length;
            }

            // Initialize ground detection
            _groundDetection = GetComponent<GroundDetectionBase>();
            if (_groundDetection == null) _groundDetection = gameObject.AddComponent<StandardGroundDetection>();
            wheelHit = new WheelHit();

            // Disable motion vectors to avoid issues with post processing blur
            DisableMotionVectors();

            // Register the contact modification event
            if (useContactModification)
            {
                Physics.ContactModifyEvent += OnContactModifyEvent;
                _targetRigidbodyId = targetRigidbody.GetInstanceID();
            }
        }

        /// <summary>
        /// Initializes the WheelController.
        /// Call after changing the position or rotation, or physical properties (radius, with, mass, etc.) of the WheelController.
        /// </summary>
        public void Initialize()
        {
            _parentTransform = TargetRigidbody.transform;

            // Get matrices and directions that do not change
            UpdateSuspensionTransforms();

            // Updates transform from the local suspension transform
            UpdateWheelTransforms();

            // Update physical properties
            if (wheel.radius < Vehicle.KINDA_SMALL_NUMBER) wheel.radius = Vehicle.KINDA_SMALL_NUMBER;
            wheel.invRadius = 1f / wheel.radius;

            if (wheel.inertia < Vehicle.SMALL_NUMBER) wheel.inertia = Vehicle.SMALL_NUMBER;
            wheel.inertia = wheel.mass * wheel.radius * wheel.radius;
            wheel.invInertia = 1f / wheel.inertia;

            if (wheel.mass < Vehicle.KINDA_SMALL_NUMBER) wheel.mass = Vehicle.KINDA_SMALL_NUMBER;


            frictionSubsteps = frictionSubsteps > 0 ? frictionSubsteps : 1;

            // Generate new collider mesh
            SetupWheelCollider();

            // Grab vehicle colliders for hit filtering (after adding wheel colliders)
            vehicleColliders = new HashSet<Collider>(targetRigidbody.transform.GetComponentsInChildren<Collider>(true));

            // Initialization done, wheel can be updated now
            _initialized = true;
        }


        private void FixedUpdate()
        {
            if (_autoSimulate)
            {
                Step();
            }
        }


        private void OnEnable()
        {
            RegisterWithWheelControllerManager();
        }


        private void OnDisable()
        {
            DeregisterWithWheelControllerManager();
        }


        public override void Step()
        {
            if (!_initialized || !isActiveAndEnabled) return;

            // Cache values
            _dt = Time.fixedDeltaTime;
            if (_dt < Vehicle.SMALL_NUMBER) _dt = Vehicle.SMALL_NUMBER;
            _invDt = 1f / _dt;
            wheel.prevAngularVelocity = wheel.angularVelocity;

            // Reset temp values
            _suspensionForce = _zeroVector;
            _frictionForce = _zeroVector;
            _hitSurfaceVelocity = _zeroVector;
            _hitContactVelocity = _zeroVector;
            wheelHit.point = _zeroVector;
            wheelHit.normal = _upVector;
            load = 0f;
            spring.force = 0f;
            damper.force = 0f;
            forwardFriction.speed = 0f;
            sideFriction.speed = 0f;
            forwardFriction.slip = 0f;
            sideFriction.slip = 0f;
            _isGrounded = false;


            // --------------------------- SUSPENSION TRANSFORMS---------------------------
            _parentWorldMatrix = _parentTransform.localToWorldMatrix;
            _parentLocalMatrix = _parentTransform.worldToLocalMatrix;
            UpdateSuspensionTransforms();
            UpdateWheelTransforms();

            // --------------------------- WHEEL CASTING ---------------------------
            // Check for ground hits
            bool hasSuspension = spring.maxLength > 0f;
            float offset = hasSuspension ? wheel.radius * 1.1f : wheel.radius * 0.1f;
            float castLength = hasSuspension ? wheel.radius * 2.2f + spring.maxLength : wheel.radius * 0.02f + offset;

            _castOrigin.x = _suspensionWorldPosition.x + _suspensionUp.x * offset;
            _castOrigin.y = _suspensionWorldPosition.y + _suspensionUp.y * offset;
            _castOrigin.z = _suspensionWorldPosition.z + _suspensionUp.z * offset;

            _castDirection.x = -_suspensionUp.x;
            _castDirection.y = -_suspensionUp.y;
            _castDirection.z = -_suspensionUp.z;

            // Do the cast
            if (_groundDetection.WheelCast(_castOrigin, _castDirection, castLength, wheel.radius, wheel.width, ref wheelHit, layerMask))
            {
                _isGrounded = true;

                _hitContactVelocity = targetRigidbody.GetPointVelocity(wheelHit.point);
                _hitRigidbody = wheelHit.collider?.attachedRigidbody;

                if (_hitRigidbody != null)
                {
                    _hitSurfaceVelocity = _hitRigidbody.GetPointVelocity(wheelHit.point);
                    _hitContactVelocity -= _hitSurfaceVelocity;
                }

                // Get forward and side friction speed components
                forwardFriction.speed = Vector3.Dot(_hitContactVelocity, _wheelForward);
                sideFriction.speed = Vector3.Dot(_hitContactVelocity, _wheelRight);
            }

            // --------------------------- SUSPENSION ---------------------------
            spring.prevLength = spring.length;
            float targetLength;

            if (_isGrounded)
            {
                _hitLocalPoint = _suspensionInvWorldMatrix.MultiplyPoint3x4(wheelHit.point);
                float hitAngle = Mathf.Asin(Mathf.Clamp(_hitLocalPoint.z / wheel.radius, -1f, 1f));
                float localGroundedYPosition = _hitLocalPoint.y + wheel.radius * Mathf.Cos(hitAngle);
                targetLength = Mathf.Clamp(-localGroundedYPosition, 0f, spring.maxLength);
            }
            else
            {
                targetLength = spring.maxLength;
            }

            if (targetLength > spring.length)
            {
                // Extension: Apply extension speed limit
                float extensionSpeed = suspensionExtensionSpeedCoeff * _dt;
                spring.length = Mathf.MoveTowards(spring.length, targetLength, extensionSpeed);
            }
            else
            {
                // Compression: Instantaneous adjustment
                spring.length = targetLength;
            }

            spring.compressionVelocity = (spring.prevLength - spring.length) / _dt;
            spring.compression = spring.maxLength == 0 ? 1f : (spring.maxLength - spring.length) / spring.maxLength;
            spring.force = _isGrounded ? spring.maxForce * spring.forceCurve.Evaluate(spring.compression) : 0f;
            damper.force = _isGrounded ? damper.CalculateDamperForce(spring.compressionVelocity) : 0f;

            if (_isGrounded)
            {
                if (spring.maxLength > 0f && spring.maxForce > 0f)
                {
                    load = spring.force + damper.force;
                    load = load < 0f ? 0f : load;
                    _suspensionForce = load * wheelHit.normal;
                    targetRigidbody.AddForceAtPosition(_suspensionForce, _suspensionWorldPosition);
                }
                else
                {
                    load = loadRating;
                }
            }

            // Update transforms once more after updating the suspension from the hit length
            UpdateWheelTransforms();

            // Update visual rotation
            wheel.axleAngle = wheel.axleAngle % 360.0f + wheel.angularVelocity * Mathf.Rad2Deg * _dt;
            Quaternion axleRotation = Quaternion.AngleAxis(wheel.axleAngle, _wheelRight);
            float camberAngle = camber * (_suspensionLocalPosition.x < 0 ? 1f : -1f);
            Quaternion camberRotation = Quaternion.AngleAxis(camberAngle, _wheelForward);
            Quaternion damageWobbleRotation = Quaternion.AngleAxis(_damage * damageMaxWobbleAngle, axleRotation * (axleRotation * _wheelUp));
            wheel.axleRotation = camberRotation * axleRotation * _wheelWorldRotation;


            // --------------------------- FRICTION ---------------------------
            UpdateFriction();


            // --------------------------- APPLY FORCES ---------------------------
            // Update axle torque
            Vector3 torque = Vector3.Cross(HitPoint - TargetRigidbody.centerOfMass, forwardFriction.force * _wheelRight) * chassisTorqueCoefficient;
            Vector3 axleTorque = Vector3.Project(torque, _wheelRight);
            targetRigidbody.AddTorque(axleTorque);


            if (_isGrounded)
            {
                // Apply friction force to self
                // Avoid adding calculated friction when using native friction
                Vector3 forcePosition = wheelHit.point + _suspensionUp * forceApplicationPointDistance * spring.maxLength;
                targetRigidbody.AddForceAtPosition(_frictionForce, forcePosition);

                // Apply forces to the hit body
                if (_hitRigidbody != null)
                {
                    Vector3 totalForce;
                    totalForce.x = -(_frictionForce.x + _suspensionForce.x) * otherBodyForceScale;
                    totalForce.y = -(_frictionForce.y + _suspensionForce.y) * otherBodyForceScale;
                    totalForce.z = -(_frictionForce.z + _suspensionForce.z) * otherBodyForceScale;

                    _hitRigidbody.AddForceAtPosition(totalForce, wheelHit.point);
                }
            }

            // --------------------------- APPLY TRANSFORMS ---------------------------
            wheel.rotatingContainer.SetPositionAndRotation(_wheelWorldPosition, wheel.axleRotation);
            wheel.nonRotatingContainer.SetPositionAndRotation(_wheelWorldPosition, camberRotation * _wheelWorldRotation);
            wheel.meshCollider.transform.SetPositionAndRotation(_suspensionWorldPosition, camberRotation * _suspensionWorldRotation);
        }


        private void UpdateSuspensionTransforms()
        {
            _suspensionLocalMatrix = TargetRigidbody.transform.worldToLocalMatrix * transform.localToWorldMatrix;

            _suspensionLocalUp.x = _suspensionLocalMatrix.m01;
            _suspensionLocalUp.y = _suspensionLocalMatrix.m11;
            _suspensionLocalUp.z = _suspensionLocalMatrix.m21;

            _suspensionLocalForward.x = _suspensionLocalMatrix.m02;
            _suspensionLocalForward.y = _suspensionLocalMatrix.m12;
            _suspensionLocalForward.z = _suspensionLocalMatrix.m22;

            _suspensionLocalRight.x = _suspensionLocalMatrix.m00;
            _suspensionLocalRight.y = _suspensionLocalMatrix.m10;
            _suspensionLocalRight.z = _suspensionLocalMatrix.m20;

            _suspensionLocalPosition.x = _suspensionLocalMatrix.m03;
            _suspensionLocalPosition.y = _suspensionLocalMatrix.m13;
            _suspensionLocalPosition.z = _suspensionLocalMatrix.m23;

            // Calculate quaternion from angle axis
            steerRotationQuaternion = Quaternion.AngleAxis(steerAngle, _suspensionLocalUp);

            // Create the rotation matrix from the quaternion
            float x2 = steerRotationQuaternion.x * steerRotationQuaternion.x;
            float y2 = steerRotationQuaternion.y * steerRotationQuaternion.y;
            float z2 = steerRotationQuaternion.z * steerRotationQuaternion.z;
            float xy = steerRotationQuaternion.x * steerRotationQuaternion.y;
            float xz = steerRotationQuaternion.x * steerRotationQuaternion.z;
            float yz = steerRotationQuaternion.y * steerRotationQuaternion.z;
            float wx = steerRotationQuaternion.w * steerRotationQuaternion.x;
            float wy = steerRotationQuaternion.w * steerRotationQuaternion.y;
            float wz = steerRotationQuaternion.w * steerRotationQuaternion.z;

            steerRotationMatrix.m00 = 1.0f - 2.0f * (y2 + z2);
            steerRotationMatrix.m01 = 2.0f * (xy - wz);
            steerRotationMatrix.m02 = 2.0f * (xz + wy);
            steerRotationMatrix.m03 = 0.0f;

            steerRotationMatrix.m10 = 2.0f * (xy + wz);
            steerRotationMatrix.m11 = 1.0f - 2.0f * (x2 + z2);
            steerRotationMatrix.m12 = 2.0f * (yz - wx);
            steerRotationMatrix.m13 = 0.0f;

            steerRotationMatrix.m20 = 2.0f * (xz - wy);
            steerRotationMatrix.m21 = 2.0f * (yz + wx);
            steerRotationMatrix.m22 = 1.0f - 2.0f * (x2 + y2);
            steerRotationMatrix.m23 = 0.0f;

            steerRotationMatrix.m30 = 0.0f;
            steerRotationMatrix.m31 = 0.0f;
            steerRotationMatrix.m32 = 0.0f;
            steerRotationMatrix.m33 = 1.0f;

            float offsetX = -wheel.rimOffset * (_suspensionLocalPosition.x < 0f ? -1f : 1f);
            steerRotationMatrix.m03 = (_suspensionLocalPosition.x + offsetX) * (1 - steerRotationMatrix.m00) + _suspensionLocalPosition.y * (-steerRotationMatrix.m01) + _suspensionLocalPosition.z * (-steerRotationMatrix.m02);
            steerRotationMatrix.m13 = (_suspensionLocalPosition.x + offsetX) * (-steerRotationMatrix.m10) + _suspensionLocalPosition.y * (1 - steerRotationMatrix.m11) + _suspensionLocalPosition.z * (-steerRotationMatrix.m12);
            steerRotationMatrix.m23 = (_suspensionLocalPosition.x + offsetX) * (-steerRotationMatrix.m20) + _suspensionLocalPosition.y * (-steerRotationMatrix.m21) + _suspensionLocalPosition.z * (1 - steerRotationMatrix.m22);

            _suspensionWorldMatrix = _parentWorldMatrix * (steerRotationMatrix * _suspensionLocalMatrix);
            _suspensionInvWorldMatrix = Matrix4x4.Inverse(_suspensionWorldMatrix);

            _suspensionWorldPosition.x = _suspensionWorldMatrix.m03;
            _suspensionWorldPosition.y = _suspensionWorldMatrix.m13;
            _suspensionWorldPosition.z = _suspensionWorldMatrix.m23;

            _suspensionUp.x = _suspensionWorldMatrix.m01;
            _suspensionUp.y = _suspensionWorldMatrix.m11;
            _suspensionUp.z = _suspensionWorldMatrix.m21;

            _suspensionForward.x = _suspensionWorldMatrix.m02;
            _suspensionForward.y = _suspensionWorldMatrix.m12;
            _suspensionForward.z = _suspensionWorldMatrix.m22;

            _suspensionRight.x = _suspensionWorldMatrix.m00;
            _suspensionRight.y = _suspensionWorldMatrix.m10;
            _suspensionRight.z = _suspensionWorldMatrix.m20;

            _suspensionWorldRotation = Quaternion.LookRotation(_suspensionForward, _suspensionUp);
        }


        /// <summary>
        /// Updates the wheel positions and rotations.
        /// </summary>
        private void UpdateWheelTransforms()
        {
            // Wheel
            Vector3 translation = -_suspensionUp * spring.length;
            _wheelWorldMatrix = _suspensionWorldMatrix;
            _wheelWorldMatrix.m03 += translation.x;
            _wheelWorldMatrix.m13 += translation.y;
            _wheelWorldMatrix.m23 += translation.z;

            _wheelWorldPosition.x = _wheelWorldMatrix.m03;
            _wheelWorldPosition.y = _wheelWorldMatrix.m13;
            _wheelWorldPosition.z = _wheelWorldMatrix.m23;

            _wheelUp.x = _suspensionWorldMatrix.m01;
            _wheelUp.y = _suspensionWorldMatrix.m11;
            _wheelUp.z = _suspensionWorldMatrix.m21;

            _wheelForward.x = _suspensionWorldMatrix.m02;
            _wheelForward.y = _suspensionWorldMatrix.m12;
            _wheelForward.z = _suspensionWorldMatrix.m22;

            _wheelRight.x = _suspensionWorldMatrix.m00;
            _wheelRight.y = _suspensionWorldMatrix.m10;
            _wheelRight.z = _suspensionWorldMatrix.m20;

            if (_wheelForward != _zeroVector && _wheelUp != _zeroVector)
            {
                _wheelWorldRotation = Quaternion.LookRotation(_wheelForward, _wheelUp);
            }
            else
            {
                _wheelWorldRotation = _suspensionWorldRotation;
            }
        }


        private void SetupWheelCollider()
        {
            // Check for any existing colliders and remove them as they might be out of date
            GameObject colliderGameObject = transform.Find("Collider")?.gameObject;
            if (colliderGameObject != null)
            {
                Destroy(colliderGameObject);
            }

            // Add wheel mesh collider
            GameObject newColliderGameObject = new GameObject("Collider");
            if (newColliderGameObject == null)
            {
                Debug.LogError("Failed go spawn a wheel collider!");
                return;
            }
            Transform colliderTransform = newColliderGameObject.transform;
            colliderTransform.SetParent(transform);
            colliderTransform.localPosition = Vector3.zero;
            colliderTransform.localRotation = Quaternion.identity;

            wheel.meshCollider = newColliderGameObject.AddComponent<MeshCollider>();
            if (wheel.meshCollider == null)
            {
                Debug.LogError("Failed to add a MeshCollider to the Collider object!");
                return;
            }
            wheel.meshCollider.convex = true;
            wheel.meshCollider.gameObject.layer = meshColliderLayer; // Ignore self raycast hit.
            wheel.meshCollider.material.bounceCombine = PhysicMaterialCombine.Minimum;
            wheel.meshCollider.material.frictionCombine = PhysicMaterialCombine.Minimum;
            wheel.meshCollider.material.bounciness = 0;
            wheel.meshCollider.material.staticFriction = 0;
            wheel.meshCollider.material.dynamicFriction = 0;
            wheel.meshCollider.hasModifiableContacts = true;

            if (wheel.meshCollider != null)
            {
                Destroy(wheel.meshCollider.sharedMesh);
                wheel.meshCollider.sharedMesh = WheelControllerUtility.CreateCylinderMesh(
                    12,
                    wheel.width * 1.05f,
                    wheel.radius * 0.95f
                );
            }
            else
            {
                Debug.LogError("Failed to set up wheel collider!");
                return;
            }
        }


        private static float GetSuspensionLengthFromWheelHit(in float wheelRadius, in Vector3 hitWorldPoint, in Matrix4x4 l2wMat, ref Vector3 hitLocalPoint)
        {
            // Convert the hit point to local coordinates
            hitLocalPoint.x = l2wMat.m00 * hitWorldPoint.x + l2wMat.m01 * hitWorldPoint.y + l2wMat.m02 * hitWorldPoint.z + l2wMat.m03;
            hitLocalPoint.y = l2wMat.m10 * hitWorldPoint.x + l2wMat.m11 * hitWorldPoint.y + l2wMat.m12 * hitWorldPoint.z + l2wMat.m13;
            hitLocalPoint.z = l2wMat.m20 * hitWorldPoint.x + l2wMat.m21 * hitWorldPoint.y + l2wMat.m22 * hitWorldPoint.z + l2wMat.m23;

            // Calculate the hit angle
            float hitLocalPointZDivided = hitLocalPoint.z / wheelRadius;

            // Clamp to -1, 1
            if (hitLocalPointZDivided < -1f)
            {
                hitLocalPointZDivided = -1f;
            }
            else if (hitLocalPointZDivided > 1f)
            {
                hitLocalPointZDivided = 1f;
            }

            float hitAngle = 0f;
            if (hitLocalPointZDivided >= -1f && hitLocalPointZDivided <= 1f)
            {
                hitAngle = Mathf.Asin(hitLocalPointZDivided);
            }

            // Calculate hit distance
            return -hitLocalPoint.y + wheelRadius * Mathf.Cos(hitAngle);
        }


        protected virtual void UpdateFriction()
        {
            float lngLoad = load * forwardFriction.loadFactor;
            float latLoad = load * sideFriction.loadFactor;
            float lngLoadClamped = lngLoad < 0f ? 0f : lngLoad > loadRating ? loadRating : lngLoad;
            float latLoadClamped = latLoad < 0f ? 0f : latLoad > loadRating ? loadRating : latLoad;
            float mass = targetRigidbody.mass;
            float absForwardSpeed = forwardFriction.speed < 0 ? -forwardFriction.speed : forwardFriction.speed;
            float absSideSpeed = sideFriction.speed < 0 ? -sideFriction.speed : sideFriction.speed;
            float forwardSpeedClamp = 1.5f * (_dt / 0.005f);
            forwardSpeedClamp = forwardSpeedClamp < 1.5f ? 1.5f : forwardSpeedClamp > 10f ? 10f : forwardSpeedClamp;
            float clampedAbsForwardSpeed = absForwardSpeed < forwardSpeedClamp ? forwardSpeedClamp : absForwardSpeed;
            float invClampedAbsForwardSpeed = 1.0f / clampedAbsForwardSpeed;

            // Calculate effect of camber on friction
            float camberFrictionCoeff = Vector3.Dot(_suspensionUp, wheelHit.normal);
            camberFrictionCoeff = camberFrictionCoeff < 0f ? 0f : camberFrictionCoeff;


            // *******************************
            // ******** LONGITUDINAL ********* 
            // *******************************

            float peakForwardFrictionForce = activeFrictionPreset.BCDE.z * lngLoadClamped;
            float combinedBrakeTorque = brakeTorque + rollingResistanceTorque;

            float invSubsteps = 1.0f / (float)frictionSubsteps;
            float sdt = _dt * invSubsteps;
            float invSdt = 1.0f / sdt;
            float lngFrictionForceSum = 0.0f;
            float invInertia = 1.0f / wheel.inertia;
            float totalWheelTorque = 0.0f;

            for (int substep = 0; substep < frictionSubsteps; substep++)
            {
                float stepMotorTorque = motorTorque * sdt;
                wheel.angularVelocity += stepMotorTorque * invInertia;
                totalWheelTorque += stepMotorTorque;

                if (_isGrounded)
                {

                    forwardFriction.slip = -(wheel.angularVelocity * wheel.radius - forwardFriction.speed) * invClampedAbsForwardSpeed * forwardFriction.stiffness;
                    float slipSign = forwardFriction.slip >= 0.0f ? 1.0f : -1.0f;
                    float absSlip = forwardFriction.slip < 0.0f ? -forwardFriction.slip : forwardFriction.slip;

                    float unclampedFrictionForce = -slipSign * activeFrictionPreset.Curve.Evaluate(absSlip) * lngLoadClamped;
                    float frictionForce = unclampedFrictionForce > peakForwardFrictionForce ?
                        peakForwardFrictionForce : unclampedFrictionForce < -peakForwardFrictionForce ? -peakForwardFrictionForce : unclampedFrictionForce;
                    float stepFrictionTorque = frictionForce * wheel.radius * sdt;
                    wheel.angularVelocity -= stepFrictionTorque * invInertia;
                    totalWheelTorque += stepFrictionTorque;
                    lngFrictionForceSum += frictionForce * invSubsteps;
                }

                if (combinedBrakeTorque > 0.0f)
                {
                    float prevAngVel = wheel.angularVelocity;
                    float angVelSign = wheel.angularVelocity >= 0.0f ? 1.0f : -1.0f;
                    float stepBrakeTorque = angVelSign * combinedBrakeTorque * sdt;
                    wheel.angularVelocity -= stepBrakeTorque * invInertia;
                    if (wheel.angularVelocity >= 0.0f && prevAngVel < 0.0f ||
                        wheel.angularVelocity < 0.0f && prevAngVel >= 0.0f)
                    {
                        wheel.angularVelocity = 0.0f;
                    }
                    totalWheelTorque -= stepBrakeTorque;
                }
            }

            forwardFriction.force = lngFrictionForceSum;
            forwardFriction.slip = _isGrounded ?
                -(wheel.angularVelocity * wheel.radius - forwardFriction.speed) * invClampedAbsForwardSpeed * forwardFriction.stiffness : 0.0f;
            forwardFriction.slip = Mathf.Clamp(forwardFriction.slip, -1.0f, 1.0f);
            counterTorque = -totalWheelTorque;

            // *******************************
            // ********** LATERAL ************ 
            // *******************************
            sideFriction.slip = (Mathf.Atan2(sideFriction.speed, clampedAbsForwardSpeed) * Mathf.Rad2Deg) * 0.01111f * sideFriction.stiffness;
            float sideSlipSign = sideFriction.slip < 0 ? -1f : 1f;
            float absSideSlip = sideFriction.slip < 0 ? -sideFriction.slip : sideFriction.slip;
            float peakSideFrictionForce = activeFrictionPreset.BCDE.z * latLoadClamped;
            sideFriction.force = -sideSlipSign * activeFrictionPreset.Curve.Evaluate(absSideSlip) * latLoadClamped * camberFrictionCoeff;

            // *******************************
            // ******* ANTI - CREEP **********
            // *******************************

            // Get the error to the reference point and apply the force to keep the wheel at that point
            if (_isGrounded && !_wakeOneFrame && absForwardSpeed < 0.12f && absSideSpeed < 0.12f)
            {
                float verticalOffset = spring.length + wheel.radius;
                Vector3 currentPosition = _suspensionWorldPosition - _suspensionUp * verticalOffset;

                if (!_lowSpeedReferenceIsSet)
                {
                    _lowSpeedReferenceIsSet = true;
                    _lowSpeedReferencePosition = currentPosition;
                }
                else
                {
                    Vector3 referenceError = _lowSpeedReferencePosition - currentPosition;
                    Vector3 correctiveForce = _invDt * load * referenceError;

                    if (Mathf.Abs(wheel.angularVelocity) < 0.5f)
                    {
                        forwardFriction.force += Vector3.Dot(correctiveForce, _suspensionForward);
                    }

                    sideFriction.force += Vector3.Dot(correctiveForce, _suspensionRight);
                }
            }
            else
            {
                _lowSpeedReferenceIsSet = false;
            }


            // Clamp the forces
            forwardFriction.force = forwardFriction.force > peakForwardFrictionForce ? peakForwardFrictionForce
                : forwardFriction.force < -peakForwardFrictionForce ? -peakForwardFrictionForce : forwardFriction.force;
            sideFriction.force = sideFriction.force > peakSideFrictionForce ? peakSideFrictionForce
                : sideFriction.force < -peakSideFrictionForce ? -peakSideFrictionForce : sideFriction.force;

            // Apply grip coefficients after the clamp
            forwardFriction.force *= forwardFriction.grip;
            sideFriction.force *= sideFriction.grip;

            // Slip / friction circle
            if (frictionCircleStrength > 0)
            {
                sideFriction.force *= 1f - (Mathf.Pow(Mathf.Clamp01(Mathf.Abs(forwardFriction.slip)), frictionCirclePower) * frictionCircleStrength);
            }

            // Combined force
            _frictionForce = _suspensionRight * sideFriction.force + _suspensionForward * forwardFriction.force;

            // Reset the wake flag
            if (_wakeOneFrame) _wakeOneFrame = false;
        }

        private void OnValidate()
        {
            // Check scale
            Debug.Assert(transform.localScale == Vector3.one,
                $"{name}: WheelController scale is not 1. WheelController scale should be [1,1,1].");

            // Check the visual
            if (wheel.rotatingContainer == gameObject.transform)
            {
                Debug.LogError($"{name}: Visual and WheelController are the same GameObject. This will result in unknown behaviour." +
                    $"The controller and the visual should be separate GameObjects.");
            }

            // Check for existing colliders
            if (!Application.isPlaying && wheel.rotatingContainer != null && wheel.rotatingContainer.GetComponentsInChildren<Collider>().Length > 0)
            {
                Debug.LogWarning($"{name}: Visual object already contains a Collider. Visual should have no colliders attached to it or its children" +
                    $" as they can prevent the wheel from functioning properly.");
            }

            if (targetRigidbody == null)
            {
                targetRigidbody = GetComponentInParent<Rigidbody>();
            }

            if (targetRigidbody != null)
            {
                string prefix = $"{targetRigidbody.name} > {name}:";

                // Check parent scale
                Debug.Assert(transform.localScale == Vector3.one, $"{prefix} WheelController parent Rigidbody scale is not 1. Rigidbody transform scale should be [1,1,1].");

                // Load rating
                float minLoadRating = targetRigidbody.mass * -Physics.gravity.y * 0.05f;
                float maxLoadRating = targetRigidbody.mass * -Physics.gravity.y * 4f;
                if (loadRating < minLoadRating)
                {
                    Debug.LogWarning($"{prefix} Load rating of the tyre might be too low. This can cause the vehicle to slide around. Current: {loadRating}, min. recommended: {minLoadRating}.");
                }
                else if (loadRating > maxLoadRating)
                {
                    Debug.LogWarning($"{prefix} Load rating of the tyre might be too high. This can cause the vehicle friction to not work properly. Current: {loadRating}, max. recommended: {maxLoadRating}.");
                }

                // Has suspension
                if (spring.length > 0)
                {
                    float minForce = targetRigidbody.mass * -Physics.gravity.y * 0.25f;
                    if (spring.maxForce < minForce)
                    {
                        Debug.LogWarning($"{prefix} spring.maxForce is most likely too low for the given Rigidbody mass. Current: {spring.maxForce}, min. recommended: {minForce}, recommended: {minForce * 3f}" +
                            $"With the current values the suspension might not be strong enough to support the weight of the vehicle and might bottom out.");
                    }

                    float minLength = Time.fixedDeltaTime;
                    if (spring.maxLength < minLength)
                    {
                        Debug.LogWarning($"{prefix} spring.maxLength is shorter than recommended for the given Time.fixedDeltaTime. Current: {spring.maxLength}, min. recommended: {minLength}. With " +
                            $"the current values the suspension might bottom out frequently and cause a harsh ride.");
                    }
                }
            }
            else
            {
                Debug.LogWarning($"Target Rigidbody on {name} is null!");
            }
        }


        private void RegisterWithWheelControllerManager()
        {
            FindOrAddWheelControllerManager();
            _wheelControllerManager.Register(this);
        }


        private void DeregisterWithWheelControllerManager()
        {
            FindOrAddWheelControllerManager();
            _wheelControllerManager.Deregister(this);
        }


        private void FindOrAddWheelControllerManager()
        {
            if (_wheelControllerManager == null)
            {
                _wheelControllerManager = GetComponentInParent<WheelControllerManager>();
                if (_wheelControllerManager == null)
                {
                    _wheelControllerManager = targetRigidbody.gameObject.AddComponent<WheelControllerManager>();
                }
            }
        }


        private void DisableMotionVectors()
        {
            // Disable motion vectors as these can cause issues with PP when wheel is rotating
            if (disableMotionVectors && wheel.rotatingContainer != null)
            {
                MeshRenderer[] meshRenderers = wheel.rotatingContainer.GetComponentsInChildren<MeshRenderer>();
                foreach (MeshRenderer mr in meshRenderers)
                {
                    mr.motionVectorGenerationMode = MotionVectorGenerationMode.ForceNoMotion;
                }
            }
        }


        private void Reset()
        {
            SetRuntimeDefaultsIfNeeded();
            FindOrSpawnVisualContainers();

            // Assume 4 as the component count might be wrong at this
            // point and wheels added at a later time.
            int wheelCount = 4;

            float gravity = -Physics.gravity.y;
            float weightPerWheel = targetRigidbody.mass * gravity / wheelCount;

            spring.maxForce = weightPerWheel * 6f;
            damper.bumpRate = weightPerWheel * 0.15f;
            damper.reboundRate = weightPerWheel * 0.15f;
            loadRating = weightPerWheel * 2f;
        }


        public void FindOrSpawnVisualContainers()
        {
            Vector3 localPosition = new Vector3(0f, -spring.maxLength * 0.5f, 0f);

            wheel.rotatingContainer = transform.Find("Rotating");
            if (wheel.rotatingContainer == null)
            {
                GameObject visualGameObject = new GameObject("Rotating");
                visualGameObject.transform.parent = transform;
                visualGameObject.transform.localPosition = localPosition;
                visualGameObject.transform.localRotation = Quaternion.identity;
                wheel.rotatingContainer = visualGameObject.transform;
            }

            wheel.nonRotatingContainer = transform.Find("NonRotating");
            if (wheel.nonRotatingContainer == null)
            {
                GameObject nonRotatingGameObject = new GameObject("NonRotating");
                nonRotatingGameObject.transform.parent = transform;
                nonRotatingGameObject.transform.localPosition = localPosition;
                nonRotatingGameObject.transform.localRotation = Quaternion.identity;
                wheel.nonRotatingContainer = nonRotatingGameObject.transform;
            }
        }


        /// <summary>
        ///     Sets default values if they have not already been set.
        ///     Gets called each time Reset() is called in editor - such as adding the script to a GameObject.
        /// </summary>
        /// <param name="reset">Sets default values even if they have already been set.</param>
        /// <param name="findWheelVisuals">Should script attempt to find wheel visuals automatically by name and position?</param>
        public void SetRuntimeDefaultsIfNeeded(bool reset = false, bool findWheelVisuals = true)
        {
            // Find parent Rigidbody
            if (targetRigidbody == null) targetRigidbody = gameObject.GetComponentInParent<Rigidbody>();
            Debug.Assert(targetRigidbody != null, "Parent does not contain a Rigidbody.");

            if (wheel == null || reset) wheel = new Wheel();
            if (spring == null || reset) spring = new Spring();
            if (damper == null || reset) damper = new Damper();

            if (forwardFriction == null || reset)
            {
                forwardFriction = new Friction()
                {
                    grip = 1.2f,
                    loadFactor = 1.8f,
                    stiffness = 0.7f,
                };
            }

            if (sideFriction == null || reset)
            {
                sideFriction = new Friction()
                {
                    grip = 1.0f,
                    loadFactor = 1.4f,
                    stiffness = 1.0f,
                };
            }

            if (activeFrictionPreset == null || reset)
                activeFrictionPreset = Resources.Load<FrictionPreset>("Wheel Controller 3D/Defaults/DefaultFrictionPreset");
            if (spring.forceCurve == null || spring.forceCurve.keys.Length == 0 || reset)
                spring.forceCurve = GenerateDefaultSpringCurve();
        }


        private AnimationCurve GenerateDefaultSpringCurve()
        {
            AnimationCurve ac = new AnimationCurve();
            ac.AddKey(0.0f, 0.0f);
            ac.AddKey(1.0f, 1.0f);
            return ac;
        }


        /// <summary>
        /// Places the WheelController roughly to the position it should be in, in relation to the first rotating container child.
        /// </summary>
        public void PositionToVisual()
        {
            if (wheel.rotatingContainer.childCount == 0)
            {
                Debug.LogWarning("Rotating container does not have any children assigned. Cannot position the WheelController as there is nothing to position to.");
            }

            if (wheel.rotatingContainer == null)
            {
                Debug.LogError("Wheel visual not assigned.");
                return;
            }

            Rigidbody rb = GetComponentInParent<Rigidbody>();
            if (rb == null)
            {
                Debug.LogError("Rigidbody not found in parent.");
                return;
            }

            int wheelCount = GetComponentInParent<Rigidbody>().GetComponentsInChildren<WheelController>().Length;
            if (wheelCount == 0) return;

            // Approximate static load on the wheel.
            float approxStaticLoad = (rb.mass * -Physics.gravity.y) / wheelCount;

            // Approximate the spring travel, not taking spring curve into account.
            float approxSpringTravel = Mathf.Clamp01(approxStaticLoad / spring.maxForce) * spring.maxLength;

            // Use first child as reference
            Transform referenceTransform = wheel.rotatingContainer.GetChild(0);

            // Position the WheelController transform above the wheel
            Vector3 initPosition = transform.position;
            transform.position = wheel.rotatingContainer.GetChild(0).position + rb.transform.up * (spring.maxLength - approxSpringTravel);

            // Offset the positions of the child transforms so that they stay in place
            Vector3 offset = transform.position - initPosition;
            foreach (Transform child in wheel.rotatingContainer)
            {
                child.position -= offset;
            }
            foreach (Transform child in wheel.nonRotatingContainer)
            {
                child.position -= offset;
            }
        }

        private void OnContactModifyEvent(PhysicsScene scene, NativeArray<ModifiableContactPair> pairs)
        {
            if (!useContactModification) return;

            if (_suspensionLocalMatrix == Matrix4x4.zero)
            {
                Debug.LogWarning("Suspension Local Matrix is not initialized.");
                return;
            }

            foreach (var pair in pairs)
            {
                // Check if our object and if not skip
                if (pair.bodyInstanceID != _targetRigidbodyId
                    && pair.otherBodyInstanceID != _targetRigidbodyId)
                {
                    continue;
                }

                bool isThisBody = pair.bodyInstanceID == _targetRigidbodyId;

                for (int contactIdx = 0; contactIdx < pair.contactCount; contactIdx++)
                {
                    Vector3 localContactPoint = _suspensionLocalMatrix.MultiplyPoint3x4(pair.GetPoint(contactIdx));
                    if (localContactPoint.y < 0)
                    {
                        Vector3 contactNormal = pair.GetNormal(contactIdx);
                        Vector3 localContactNormal = _suspensionLocalMatrix.MultiplyVector(contactNormal);
                        float dot = Mathf.Abs(Vector3.Dot(localContactNormal, _suspensionLocalForward));
                        if (dot > 0.3f)
                        {
                            float separation = pair.GetSeparation(contactIdx);
                            if (separation < 0)
                            {
                                // 1 at the axle position, 0 at the bottom of the wheel (y direction).
                                // The idea is to reduce the impact force at the lower parts of the collider.
                                float gradient = 1f - Mathf.Clamp01(-localContactPoint.y / wheel.radius);
                                pair.SetSeparation(contactIdx, separation * gradient);
                            }
                        }
                    }
                }
            }
        }
    }
}



#if UNITY_EDITOR
namespace NWH.WheelController3D
{
    /// <summary>
    ///     Editor for WheelController.
    /// </summary>
    [CustomEditor(typeof(WheelController))]
    [CanEditMultipleObjects]
    public class WheelControllerEditor : NUIEditor
    {
        public override bool OnInspectorNUI()
        {
            if (!base.OnInspectorNUI()) return false;

            WheelController wc = target as WheelController;
            if (wc == null) return false;

            float logoHeight = 40f;
            Rect texRect = drawer.positionRect;
            texRect.height = logoHeight;
            drawer.DrawEditorTexture(texRect, "Wheel Controller 3D/Editor/logo_wc3d", ScaleMode.ScaleToFit);
            drawer.Space(logoHeight + 4);


            int tabIndex = drawer.HorizontalToolbar("wc3dMenu",
                                     new[] { "Wheel", "Suspension", "Friction", "Misc", "Debug" }, true, true);

            if (tabIndex == 0) // WHEEL
            {
                drawer.BeginSubsection("Wheel");
                drawer.Field("wheel.radius", true, "m");
                drawer.Field("wheel.width", true, "m");
                drawer.Field("wheel.mass", true, "kg");
                drawer.Field("wheel.rimOffset", true, "m");
                drawer.Field("loadRating", true, "N");
                drawer.Info("It is important to set the load rating correctly as it affects friction drastically.\r\n" +
                    "A value of about 2x of the Load at rest (Debug tab) is a good guidance.");
                drawer.Field("rollingResistanceTorque", true, "Nm");
                drawer.EndSubsection();

                drawer.BeginSubsection("Wheel Model");
                drawer.Info("Visual and NonRotatingVisual have been replaced with GameObject containers attached to WheelController " +
                    "to which any rotating and non-rotating meshes can be assigned. If upgrading, use the button below to generate those or use " +
                    "the automatic upgrade button (Misc tab).");
                drawer.EndSubsection();
            }
            else if (tabIndex == 1) // SUSPENSION
            {
                drawer.BeginSubsection("Spring");
                drawer.Field("spring.maxForce", true, "N@100%");
                if (Application.isPlaying)
                    if (wc != null)
                    {
                        float minRecommended = wc.TargetRigidbody.mass * -Physics.gravity.y / 4f;
                        if (wc.SpringMaxForce < minRecommended)
                            drawer.Info(
                                "MaxForce of Spring is most likely too low for the vehicle mass. Minimum recommended for current configuration is" +
                                $" {minRecommended}N.", MessageType.Warning);
                    }

                if (drawer.Field("spring.maxLength", true, "m").floatValue < Time.fixedDeltaTime * 10f)
                    drawer.Info(
                        $"Minimum recommended spring length for Time.fixedDeltaTime of {Time.fixedDeltaTime} is {Time.fixedDeltaTime * 10f}");

                drawer.Field("spring.forceCurve");
                drawer.Info("X: Spring compression [%], Y: Force coefficient");
                drawer.EndSubsection();

                drawer.BeginSubsection("Damper");
                drawer.Field("damper.bumpRate", true, "Ns/m");
                drawer.Field("damper.slowBump", true, "slope");
                drawer.Field("damper.fastBump", true, "slope");
                drawer.Field("damper.bumpDivisionVelocity", true, "m/s");
                drawer.Space();
                drawer.Field("damper.reboundRate", true, "Ns/m");
                drawer.Field("damper.slowRebound", true, "slope");
                drawer.Field("damper.fastRebound", true, "slope");
                drawer.Field("damper.reboundDivisionVelocity", true, "m/s");
                drawer.EndSubsection();

                drawer.BeginSubsection("General");
                drawer.Field("chassisTorqueCoefficient", true, "x100%");
                drawer.Field("suspensionExtensionSpeedCoeff");
                drawer.Field("forceApplicationPointDistance", true, null, "Force App. Point Distance");
                drawer.Field("camber", true, "deg");
                drawer.Info("For dynamic camber, use the CamberController component.");
                drawer.EndSubsection();
            }
            else if (tabIndex == 2) // FRICTION
            {
                drawer.BeginSubsection("Friction");
                drawer.Field("activeFrictionPreset");
                drawer.EmbeddedObjectEditor<NUIEditor>(((WheelController)target).FrictionPreset,
                                                       drawer.positionRect);

                drawer.BeginSubsection("Friction Circle");
                drawer.Field("frictionCircleStrength", true, null, "Strength");
                drawer.Field("frictionCirclePower", true, null, "Power");
                drawer.EndSubsection();

                drawer.BeginSubsection("Longitudinal / Forward");
                drawer.Field("forwardFriction.stiffness", true, "x100 %");
                drawer.Field("forwardFriction.grip", true, "x100 %");
                drawer.Field("forwardFriction.loadFactor", true, "x100 %");
                drawer.Field("frictionSubsteps");
                drawer.EndSubsection();

                drawer.BeginSubsection("Lateral / Sideways");
                drawer.Field("sideFriction.stiffness", true, "x100 %");
                drawer.Field("sideFriction.grip", true, "x100 %");
                drawer.Field("sideFriction.loadFactor", true, "x100 %");
                drawer.EndSubsection();
                drawer.EndSubsection();
            }
            else if (tabIndex == 3) // MISC
            {
                drawer.BeginSubsection("Actions");
                if (drawer.Button("Position WheelController to the first child of the rotating container"))
                {
                    foreach (WheelController targetWC in targets)
                    {
                        Undo.RecordObject(targetWC, "WC3D: PositionToVisual");
                        targetWC.PositionToVisual();
                        EditorUtility.SetDirty(targetWC);
                    }
                }
                if (drawer.Button("Generate Visual Containers"))
                {
                    foreach (WheelController targetWC in targets)
                    {
                        Undo.RecordObject(targetWC, "WC3D: FindOrSpawnVisualContainers");
                        WheelController wheelController = (WheelController)target;
                        wheelController.FindOrSpawnVisualContainers();
                        EditorUtility.SetDirty(targetWC);
                    }
                }
                if (drawer.Button("Upgrade from v11"))
                {
                    foreach (WheelController targetWC in targets)
                    {
                        Undo.RecordObject(targetWC, "WC3D: UpdateFromV11");
                        WheelController wheelController = (WheelController)target;
                        UpgradeFromV11(wheelController);
                        EditorUtility.SetDirty(targetWC);
                    }
                }
                drawer.EndSubsection();


                drawer.BeginSubsection("Damage");
                {
                    drawer.Field("damageMaxWobbleAngle");
                }
                drawer.EndSubsection();


                drawer.BeginSubsection("Rendering");
                {
                    drawer.Field("disableMotionVectors");
                }
                drawer.EndSubsection();


                drawer.BeginSubsection("Layers");
                {
                    drawer.Field("layerMask");
                    drawer.Field("meshColliderLayer");
                }
                drawer.EndSubsection();



                drawer.BeginSubsection("Other");
                {
                    drawer.Field("useContactModification");
                    drawer.Field("otherBodyForceScale");
                }
                drawer.EndSubsection();
            }
            else
            {
                drawer.Label($"Is Grounded: {wc.IsGrounded}");
                if (wc.IsGrounded)
                {
                    drawer.Label($"Hit Collider: {wc.HitCollider}");
                }
                drawer.Space();

                drawer.Label("Wheel");
                drawer.Label($"\tSteer Angle: {wc.SteerAngle}");
                drawer.Label($"\tMotor Torque: {wc.MotorTorque}");
                drawer.Label($"\tBrake Torque: {wc.BrakeTorque}");
                drawer.Label($"\tAng. Vel: {wc.AngularVelocity}");

                drawer.Label("Friction");
                drawer.Label($"\tLng. Slip: {wc.LongitudinalSlip}");
                drawer.Label($"\tLng. Speed: {wc.forwardFriction.speed}");
                drawer.Label($"\tLng. Force: {wc.forwardFriction.force}");
                drawer.Label($"\tLat. Slip: {wc.LateralSlip}");
                drawer.Label($"\tLat. Speed: {wc.sideFriction.speed}");
                drawer.Label($"\tLat. Force: {wc.sideFriction.force}");

                drawer.Label("Suspension");
                drawer.Label($"\tLoad: {wc.Load}");
                drawer.Label($"\tSpring Length: {wc.SpringLength}");
                drawer.Label($"\tSpring Force: {wc.spring.force}");
                drawer.Label($"\tSpring Velocity: {wc.spring.compressionVelocity}");
                drawer.Label($"\tDamper Force: {wc.damper.force}");
            }

            drawer.EndEditor(this);
            return true;
        }


        public override bool UseDefaultMargins()
        {
            return false;
        }

        private void UpgradeFromV11(WheelController wheelController)
        {
            //Disable warning for Obsolete
#pragma warning disable CS0612
            if (wheelController.wheel.rotatingContainer == null)
            {
                Debug.Log("Spawning visual containers.");
                wheelController.FindOrSpawnVisualContainers();
            }

            if (wheelController.wheel.nonRotatingVisual != null)
            {
                Debug.Log("Parenting the NonRotatingVisual to the nonRotatingContainer.");
                Debug.Assert(wheelController.wheel.nonRotatingContainer != null);
                wheelController.wheel.nonRotatingVisual.transform.SetParent(wheelController.wheel.nonRotatingContainer, true);

                Debug.Log("Updating the NonRotatingVisual transform.");
                Vector3 localOffset = wheelController.wheel.nonRotatingVisual.transform.localPosition - wheelController.wheel.nonRotatingContainer.localPosition;
                wheelController.NonRotatingVisual.transform.localPosition = localOffset;
            }

            if (wheelController.wheel.visual != null)
            {
                Debug.Log("Parenting the Visual to the rotatingContainer.");
                Debug.Assert(wheelController.wheel.rotatingContainer != null);
                wheelController.wheel.visual.transform.SetParent(wheelController.wheel.rotatingContainer, true);

                Debug.Log("Updating the Visual transform.");
                wheelController.wheel.visual.transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
            }

            Debug.Log("Removing references to obsolete visual and nonRotatingVisual.");
            wheelController.wheel.visual = null;
            wheelController.wheel.nonRotatingVisual = null;

            Debug.Log("Upgrade from v11 complete.");
#pragma warning restore CS0612
        }
    }
}

#endif