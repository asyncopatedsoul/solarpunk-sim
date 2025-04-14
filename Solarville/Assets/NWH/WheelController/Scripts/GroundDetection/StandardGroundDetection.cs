using UnityEngine;
using NWH.NUI;
using System.Collections.Generic;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace NWH.WheelController3D
{
    /// <summary>
    /// Standard ground detection implementation for WheelController.
    /// Optimized for performance with micro-optimizations and manual inlining.
    /// </summary>
    [RequireComponent(typeof(WheelController))]
    public class StandardGroundDetection : GroundDetectionBase
    {
        /// <summary>
        /// When true, multiple casts are used at all times.
        /// Increases the ground detection quality at the cost of performance (~30% slower).
        /// </summary>
        public bool forceMulticast;

        private RaycastHit _castResult;
        private WheelController _wheelController;
        private Transform _transform;

        private const int MaxHits = 32; // Maximum number of hits to handle
        private RaycastHit[] _raycastHits = new RaycastHit[MaxHits];
        private RaycastHit[] _spherecastHits = new RaycastHit[MaxHits];
        private List<RaycastHit> _allHits = new List<RaycastHit>(MaxHits);

#if WC3D_DEBUG
        // Debugging fields
        private List<WheelCastResult> _wheelCastResults = new List<WheelCastResult>();
        private List<WheelCastInfo> _wheelCasts = new List<WheelCastInfo>();

        [System.Serializable]
        private struct WheelCastInfo
        {
            public WheelCastInfo(Type castType, Vector3 origin, Vector3 direction,
                float distance, float radius, float width)
            {
                this.castType = castType;
                this.origin = origin;
                this.direction = direction;
                this.distance = distance;
                this.radius = radius;
                this.width = width;
            }

            public enum Type
            {
                Ray,
                Sphere
            }

            public Type castType;
            public Vector3 origin;
            public Vector3 direction;
            public float distance;
            public float radius;
            public float width;
        }

        [System.Serializable]
        private struct WheelCastResult
        {
            public WheelCastResult(Vector3 point, Vector3 normal, WheelCastInfo castInfo)
            {
                this.point = point;
                this.normal = normal;
                this.castInfo = castInfo;
            }

            public Vector3 point;
            public Vector3 normal;
            public WheelCastInfo castInfo;
        }
#endif

        private void Awake()
        {
            _wheelController = GetComponent<WheelController>();
            _transform = transform;
        }

        public override bool WheelCast(in Vector3 origin, in Vector3 direction, in float distance, in float radius, in float width, ref WheelHit wheelHit, LayerMask layerMask)
        {
#if WC3D_DEBUG
            _wheelCastResults.Clear();
            _wheelCasts.Clear();
#endif
            bool isValid = WheelCastSingleSphere(origin, direction, distance, radius, width, ref _castResult, layerMask);

            if (forceMulticast || !isValid)
            {
                isValid = WheelCastMultiSphere(origin, direction, distance, radius, width, ref _castResult, layerMask);
            }

            if (isValid)
            {
                wheelHit.point = _castResult.point;
                wheelHit.normal = _castResult.normal;
                wheelHit.collider = _castResult.collider;
            }

            return isValid;
        }

        private bool WheelCastSingleSphere(Vector3 origin, Vector3 direction, float distance, float radius, float width, ref RaycastHit hit, LayerMask layerMask)
        {
#if WC3D_DEBUG
            WheelCastInfo castInfo = new WheelCastInfo(WheelCastInfo.Type.Sphere,
              origin, direction, distance, radius, width);
            _wheelCasts.Add(castInfo);
#endif
            // Decide whether to use Raycast or SphereCast based on wheel width
            bool useRaycast = width <= 0.01f;

            int hitCount = 0;

            if (useRaycast)
            {
                // Use RaycastNonAlloc for narrow wheels
                hitCount = Physics.RaycastNonAlloc(origin, direction, _raycastHits, distance, layerMask, QueryTriggerInteraction.Ignore);
            }
            else
            {
                // Use SphereCastNonAlloc for wider wheels
                hitCount = Physics.SphereCastNonAlloc(origin, radius, direction, _spherecastHits, distance, layerMask, QueryTriggerInteraction.Ignore);
            }

            if (hitCount > 0)
            {
                float minDistance = float.MaxValue;
                RaycastHit closestHit = default(RaycastHit);
                RaycastHit[] hits = useRaycast ? _raycastHits : _spherecastHits;

                for (int i = 0; i < hitCount; i++)
                {
                    var h = hits[i];

                    // Ignore own colliders
                    if (_wheelController.vehicleColliders.Contains(h.collider))
                        continue;

                    // Check if the hit point is inside the wheel's volume
                    Vector3 offset = h.point - origin;
                    Vector3 localOffset = _transform.InverseTransformVector(offset);
                    float halfWidth = width * 0.5f;

                    if (localOffset.x < -halfWidth || localOffset.x > halfWidth || localOffset.z > radius || localOffset.z < -radius)
                        continue;

                    // Check if this hit is closer
                    if (h.distance < minDistance)
                    {
                        minDistance = h.distance;
                        closestHit = h;
                    }
                }

                if (minDistance != float.MaxValue)
                {
                    hit = closestHit;
#if WC3D_DEBUG
                    _wheelCastResults.Add(new WheelCastResult(hit.point, hit.normal, castInfo));
#endif
                    return true;
                }
            }

            return false;
        }

        private bool WheelCastMultiSphere(Vector3 origin, Vector3 direction, float distance, float radius, float width, ref RaycastHit hit, LayerMask layerMask)
        {
            float castRadius = width * 0.5f;

            // Decide whether to use Raycast or SphereCast based on wheel width
            bool useRaycast = castRadius <= 0.01f;

            int zSteps = useRaycast ? 1 : Mathf.RoundToInt((radius / castRadius) * 2f);
            zSteps = Mathf.Max(zSteps, 3); // Ensure at least 3 steps for coverage
            zSteps = zSteps % 2 == 0 ? zSteps + 1 : zSteps; // Ensure an odd number
            float stepAngle = 180f / (zSteps - 1);

            Vector3 up = _transform.up;
            Vector3 forwardOffset = _transform.forward * radius;

            Quaternion steerQuaternion = Quaternion.AngleAxis(_wheelController.SteerAngle, up);
            Quaternion xStepQuaternion = Quaternion.AngleAxis(stepAngle, _transform.right);
            Quaternion xRotationQuaternion = Quaternion.identity;

            _allHits.Clear();

            for (int z = 0; z < zSteps; z++)
            {
                Vector3 castOrigin = origin + steerQuaternion * xRotationQuaternion * forwardOffset;

#if WC3D_DEBUG
                WheelCastInfo castInfo = new WheelCastInfo(useRaycast ? WheelCastInfo.Type.Ray : WheelCastInfo.Type.Sphere,
                    castOrigin, direction, distance, castRadius, width);
                _wheelCasts.Add(castInfo);
#endif
                int hitCount = 0;
                RaycastHit[] hits = useRaycast ? _raycastHits : _spherecastHits;

                if (useRaycast)
                {
                    // Use RaycastNonAlloc for narrow wheels
                    hitCount = Physics.RaycastNonAlloc(castOrigin, direction, hits, distance, layerMask, QueryTriggerInteraction.Ignore);
                }
                else
                {
                    // Use SphereCastNonAlloc for wider wheels
                    hitCount = Physics.SphereCastNonAlloc(castOrigin, castRadius, direction, hits, distance, layerMask, QueryTriggerInteraction.Ignore);
                }

                for (int i = 0; i < hitCount; i++)
                {
                    var h = hits[i];
                    _allHits.Add(h);

#if WC3D_DEBUG
                    _wheelCastResults.Add(new WheelCastResult(h.point, h.normal, castInfo));
#endif
                }

                xRotationQuaternion *= xStepQuaternion;
            }

            if (_allHits.Count > 0)
            {
                float minDistance = float.MaxValue;
                RaycastHit closestHit = default(RaycastHit);

                for (int i = 0; i < _allHits.Count; i++)
                {
                    var currentHit = _allHits[i];

                    // Ignore own colliders
                    if (_wheelController.vehicleColliders.Contains(currentHit.collider))
                        continue;

                    // Check if the hit point is inside the wheel's volume
                    Vector3 offset = currentHit.point - origin;
                    Vector3 localOffset = _transform.InverseTransformVector(offset);
                    float halfWidth = width * 0.5f;

                    if (localOffset.x < -halfWidth || localOffset.x > halfWidth || localOffset.z > radius || localOffset.z < -radius)
                        continue;

                    // Check if this hit is closer
                    if (currentHit.distance < minDistance)
                    {
                        minDistance = currentHit.distance;
                        closestHit = currentHit;
                    }
                }

                if (minDistance != float.MaxValue)
                {
                    hit = closestHit;
                    return true;
                }
            }

            return false;
        }

#if WC3D_DEBUG
        private void OnDrawGizmos()
        {
            foreach (WheelCastInfo wheelCast in _wheelCasts)
            {
                Gizmos.color = Color.cyan;
                if (wheelCast.castType == WheelCastInfo.Type.Sphere)
                {
                    Gizmos.DrawWireSphere(wheelCast.origin, wheelCast.radius);
                }
                else
                {
                    Gizmos.DrawCube(wheelCast.origin, new Vector3(0.01f, 0.05f, 0.01f));
                }

                Gizmos.DrawRay(wheelCast.origin, wheelCast.direction * wheelCast.distance);
            }

            foreach (WheelCastResult result in _wheelCastResults)
            {
                Vector3 offset = result.point - result.castInfo.origin;
                Vector3 localOffset = _transform.InverseTransformVector(offset);
                float halfWidth = result.castInfo.width * 0.5f;

                bool isInsideWheel = localOffset.x >= -halfWidth && localOffset.x <= halfWidth
                                     && localOffset.z <= result.castInfo.radius && localOffset.z >= -result.castInfo.radius;

                Gizmos.color = isInsideWheel ? Color.green : Color.yellow;
                Gizmos.DrawWireSphere(result.point, 0.02f);
                Gizmos.DrawRay(result.point, result.normal * 0.1f);
            }
        }
#endif
    }

#if UNITY_EDITOR
    /// <summary>
    /// Editor for StandardGroundDetection.
    /// </summary>
    [CustomEditor(typeof(StandardGroundDetection))]
    [CanEditMultipleObjects]
    public class StandardGroundDetectionEditor : NUIEditor
    {
        public override bool OnInspectorNUI()
        {
            if (!base.OnInspectorNUI()) return false;

            drawer.Field("forceMulticast");

            drawer.EndEditor(this);
            return true;
        }
    }
#endif
}
