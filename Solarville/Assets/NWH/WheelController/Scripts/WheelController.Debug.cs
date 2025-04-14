using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace NWH.WheelController3D
{
    public partial class WheelController
    {
        /// <summary>
        ///     Visual representation of the wheel and it's more important Vectors.
        /// </summary>
        private void OnDrawGizmosSelected()
        {
#if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                _suspensionLocalPosition = transform.localPosition;
                _suspensionWorldPosition = transform.position;
                _suspensionForward = transform.forward;
                _suspensionRight = transform.right;
                _suspensionUp = transform.up;
                _wheelWorldPosition = _suspensionWorldPosition - _suspensionUp * spring.maxLength * 0.5f;
                _wheelForward = transform.forward;
                _wheelRight = transform.right;
                _wheelUp = transform.up;
            }

            // Draw spring travel
            Gizmos.color = Color.white;
            Vector3 forwardOffset = _suspensionForward * 0.05f;
            Vector3 springOffset = _suspensionUp * spring.maxLength;

            Gizmos.DrawLine(_suspensionWorldPosition - forwardOffset, _suspensionWorldPosition + forwardOffset);
            Gizmos.DrawLine(_suspensionWorldPosition - springOffset - forwardOffset,
                            _suspensionWorldPosition - springOffset + forwardOffset);
            Gizmos.DrawLine(_suspensionWorldPosition, _suspensionWorldPosition - springOffset);

            // Draw force app. point
            Gizmos.color = Color.red;
            Vector3 forceAppPoint = _wheelWorldPosition + Vector3.up * (-wheel.radius + spring.maxLength * forceApplicationPointDistance);
            Gizmos.DrawSphere(forceAppPoint, wheel.radius * 0.02f);
            Handles.Label(forceAppPoint, "    Force App. Point");

            // Draw wheel
            Gizmos.color = Color.grey;
            Gizmos.DrawSphere(_wheelWorldPosition, wheel.radius * 0.04f);
            Vector3 c0 = _wheelWorldPosition - _wheelRight * wheel.width * 0.5f;
            Vector3 c1 = _wheelWorldPosition + _wheelRight * wheel.width * 0.5f;
            Handles.DrawWireDisc(c0, _wheelRight, wheel.radius);
            Handles.DrawWireDisc(c1, _wheelRight, wheel.radius);
            Handles.DrawLine(c0 - _wheelUp * wheel.radius, c1 - _wheelUp * wheel.radius);
            Handles.DrawLine(c0 + _wheelUp * wheel.radius, c1 + _wheelUp * wheel.radius);
            Handles.DrawLine(c0 - _wheelUp * wheel.radius, c0 + _wheelUp * wheel.radius);
            Handles.DrawLine(c1 - _wheelUp * wheel.radius, c1 + _wheelUp * wheel.radius);

            // Draw wheel anchor normals
            Gizmos.color = Color.green;
            Gizmos.DrawRay(new Ray(_wheelWorldPosition, _wheelUp * wheel.radius * 0.5f));
            Gizmos.color = Color.blue;
            Gizmos.DrawRay(new Ray(_wheelWorldPosition, _wheelForward * wheel.radius * 0.5f));
            Gizmos.color = Color.red;
            Gizmos.DrawRay(new Ray(_wheelWorldPosition, _wheelRight * wheel.radius * 0.5f));

            // Draw suspension directions
            Gizmos.color = Color.green;
            Gizmos.DrawRay(new Ray(_suspensionWorldPosition, _suspensionUp * wheel.radius * 0.5f));
            Gizmos.color = Color.blue;
            Gizmos.DrawRay(new Ray(_suspensionWorldPosition, _suspensionForward * wheel.radius * 0.5f));
            Gizmos.color = Color.red;
            Gizmos.DrawRay(new Ray(_suspensionWorldPosition, _suspensionRight * wheel.radius * 0.5f));

            // Draw wheel offset
            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(_wheelWorldPosition, _wheelWorldPosition + _wheelRight * wheel.rimOffset);

            if (_isGrounded)
            {
                //Draw hit forward and sideways
                Gizmos.color = Color.cyan;
                Gizmos.DrawRay(wheelHit.point,
                                _suspensionForward * (forwardFriction.force * 1e-4f));

                Gizmos.color = Color.magenta;
                Gizmos.DrawRay(wheelHit.point,
                                _suspensionRight * (sideFriction.force * 1e-4f));

                // Draw hit point
                Gizmos.color = Color.yellow;
                Gizmos.DrawWireSphere(wheelHit.point, wheel.radius * 0.044f);

                Gizmos.color = Color.white;
                Gizmos.DrawSphere(wheelHit.point, wheel.radius * 0.04f);

                Gizmos.DrawRay(wheelHit.point, wheelHit.normal * 0.5f);
            }
#endif
        }
    }
}
