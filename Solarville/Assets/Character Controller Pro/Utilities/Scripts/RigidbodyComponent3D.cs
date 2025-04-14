using UnityEngine;

namespace Lightbug.Utilities
{
    /// <summary>
    /// An implementation of RigidbodyComponent for 3D rigidbodies.
    /// </summary>
    public sealed class RigidbodyComponent3D : RigidbodyComponent
    {
        Rigidbody _rigidbody = null;

        protected override bool IsUsingContinuousCollisionDetection => _rigidbody.collisionDetectionMode > 0;

        public override HitInfo Sweep(Vector3 position, Vector3 direction, float distance)
        {
            var p = Position;
            Position = position;
            _rigidbody.SweepTest(direction, out RaycastHit raycastHit, distance);
            Position = p;
            return new HitInfo(ref raycastHit, direction);
        }

        protected override void Awake()
        {
            base.Awake();
            _rigidbody = gameObject.GetOrAddComponent<Rigidbody>();
            _rigidbody.hideFlags = HideFlags.NotEditable;

            previousContinuousCollisionDetection = IsUsingContinuousCollisionDetection;
        }


        public override bool Is2D => false;

        public override float Mass
        {
            get => _rigidbody.mass;
            set => _rigidbody.mass = value;
        }

        public override float LinearDrag
        {
#if UNITY_6000_0_OR_NEWER
            get => _rigidbody.linearDamping;
            set => _rigidbody.linearDamping = value;
#else
            get => _rigidbody.drag;
            set => _rigidbody.drag = value;
#endif
        }

        public override float AngularDrag
        {
#if UNITY_6000_0_OR_NEWER
            get => _rigidbody.angularDamping;
            set => _rigidbody.angularDamping = value;
#else
            get => _rigidbody.angularDrag;
            set => _rigidbody.angularDrag = value;            
#endif
        }


        public override bool IsKinematic
        {
            get => _rigidbody.isKinematic;
            set
            {
                if (value == IsKinematic)
                    return;

                // Since CCD can't be true for kinematic bodies, the body type must change to dynamic before setting CCD
                if (value)
                {
                    ContinuousCollisionDetection = false;
                    _rigidbody.isKinematic = true;
                }
                else
                {
                    _rigidbody.isKinematic = false;
                    ContinuousCollisionDetection = previousContinuousCollisionDetection;
                }

                InvokeOnBodyTypeChangeEvent();
            }
        }

        public override bool UseGravity
        {
            get => _rigidbody.useGravity;
            set => _rigidbody.useGravity = value;
        }

        public override bool UseInterpolation
        {
            get => _rigidbody.interpolation == RigidbodyInterpolation.Interpolate;
            set => _rigidbody.interpolation = value ? RigidbodyInterpolation.Interpolate : RigidbodyInterpolation.None;
        }

        public override bool ContinuousCollisionDetection
        {
            get => _rigidbody.collisionDetectionMode == CollisionDetectionMode.Continuous;
            set => _rigidbody.collisionDetectionMode = value ? CollisionDetectionMode.Continuous : CollisionDetectionMode.Discrete;
        }

        public override RigidbodyConstraints Constraints
        {
            get => _rigidbody.constraints;
            set => _rigidbody.constraints = value;
        }

        public override Vector3 Position
        {
            get => _rigidbody.position;
            set => _rigidbody.position = value;
        }

        public override Quaternion Rotation
        {
            get => _rigidbody.rotation;
            set => _rigidbody.rotation = value;
        }

        public override Vector3 Velocity
        {
#if UNITY_6000_0_OR_NEWER
            get => _rigidbody.linearVelocity;
            set => _rigidbody.linearVelocity = value;
#else
            get => _rigidbody.velocity;
            set => _rigidbody.velocity = value;            
#endif
        }

        public override Vector3 AngularVelocity
        {
            get => _rigidbody.angularVelocity;
            set => _rigidbody.angularVelocity = value;
        }

        public override void Interpolate(Vector3 position) => _rigidbody.MovePosition(position);
        public override void Interpolate(Quaternion rotation) => _rigidbody.MoveRotation(rotation);
        public override Vector3 GetPointVelocity(Vector3 point) => _rigidbody.GetPointVelocity(point);
        public override void AddForceToRigidbody(Vector3 force, ForceMode forceMode = ForceMode.Force) => _rigidbody.AddForce(force, forceMode);
        public override void AddExplosionForceToRigidbody(float explosionForce, Vector3 explosionPosition, float explosionRadius, float upwardsModifier = 0) => _rigidbody.AddExplosionForce(explosionForce, explosionPosition, explosionRadius, upwardsModifier);
    }

}