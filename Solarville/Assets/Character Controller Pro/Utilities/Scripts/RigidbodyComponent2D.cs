using System.Collections;
using UnityEngine;

namespace Lightbug.Utilities
{
    /// <summary>
    /// An implementation of RigidbodyComponent for 2D rigidbodies.
    /// </summary>
    public sealed class RigidbodyComponent2D : RigidbodyComponent
    {
        Rigidbody2D _rigidbody = null;
        RaycastHit2D[] _sweepBuffer = new RaycastHit2D[10];

        protected override bool IsUsingContinuousCollisionDetection => _rigidbody.collisionDetectionMode > 0;

        public override HitInfo Sweep(Vector3 position, Vector3 direction, float distance)
        {
            var p = Position;
            Position = position;
            int length = _rigidbody.Cast(direction, _sweepBuffer, distance);
            Position = p;

            _sweepBuffer.GetClosestHit(out RaycastHit2D raycastHit, length, null);

            return new HitInfo(ref raycastHit, direction);
        }

        protected override void Awake()
        {
            base.Awake();
            _rigidbody = gameObject.GetOrAddComponent<Rigidbody2D>();
            _rigidbody.hideFlags = HideFlags.NotEditable;

            previousContinuousCollisionDetection = IsUsingContinuousCollisionDetection;
        }


        public override bool Is2D => true;

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
            get => _rigidbody.bodyType == RigidbodyType2D.Kinematic;
            set
            {
                if (value == IsKinematic)
                    return;

                // Since CCD can't be true for kinematic bodies, the body type must change to dynamic before setting CCD
                if (value)
                {
                    ContinuousCollisionDetection = false;
                    _rigidbody.bodyType = RigidbodyType2D.Kinematic;
                }
                else
                {
                    
                    _rigidbody.bodyType = RigidbodyType2D.Dynamic;
                    ContinuousCollisionDetection = previousContinuousCollisionDetection;
                }

                InvokeOnBodyTypeChangeEvent();
            }
        }

        public override bool UseGravity
        {
            get => _rigidbody.gravityScale != 0f;
            set => _rigidbody.gravityScale = value ? 1f : 0f;
        }

        public override bool UseInterpolation
        {
            get => _rigidbody.interpolation == RigidbodyInterpolation2D.Interpolate;
            set => _rigidbody.interpolation = value ? RigidbodyInterpolation2D.Interpolate : RigidbodyInterpolation2D.None;
        }

        public override bool ContinuousCollisionDetection
        {
            get => _rigidbody.collisionDetectionMode == CollisionDetectionMode2D.Continuous;
            set => _rigidbody.collisionDetectionMode = value ? CollisionDetectionMode2D.Continuous : CollisionDetectionMode2D.Discrete;
        }

        public override RigidbodyConstraints Constraints
        {
            get
            {
                switch (_rigidbody.constraints)
                {
                    case RigidbodyConstraints2D.None:
                        return RigidbodyConstraints.None;

                    case RigidbodyConstraints2D.FreezeAll:
                        return RigidbodyConstraints.FreezeAll;

                    case RigidbodyConstraints2D.FreezePosition:
                        return RigidbodyConstraints.FreezePosition;

                    case RigidbodyConstraints2D.FreezePositionX:
                        return RigidbodyConstraints.FreezePositionX;

                    case RigidbodyConstraints2D.FreezePositionY:
                        return RigidbodyConstraints.FreezePositionY;

                    case RigidbodyConstraints2D.FreezeRotation:
                        return RigidbodyConstraints.FreezeRotationZ;

                    default:
                        return RigidbodyConstraints.None;
                }

            }
            set
            {
                switch (value)
                {
                    case RigidbodyConstraints.None:
                        _rigidbody.constraints = RigidbodyConstraints2D.None;
                        break;
                    case RigidbodyConstraints.FreezeAll:
                        _rigidbody.constraints = RigidbodyConstraints2D.FreezeAll;
                        break;
                    case RigidbodyConstraints.FreezePosition:
                        _rigidbody.constraints = RigidbodyConstraints2D.FreezePosition;
                        break;
                    case RigidbodyConstraints.FreezePositionX:
                        _rigidbody.constraints = RigidbodyConstraints2D.FreezePositionX;
                        break;
                    case RigidbodyConstraints.FreezePositionY:
                        _rigidbody.constraints = RigidbodyConstraints2D.FreezePositionY;
                        break;
                    case RigidbodyConstraints.FreezeRotation:
                        _rigidbody.constraints = RigidbodyConstraints2D.FreezeRotation;
                        break;
                    case RigidbodyConstraints.FreezeRotationZ:
                        _rigidbody.constraints = RigidbodyConstraints2D.FreezeRotation;
                        break;
                    default:
                        _rigidbody.constraints = RigidbodyConstraints2D.None;
                        break;
                }
            }
        }

        public override Vector3 Position
        {
            get => new Vector3(_rigidbody.position.x, _rigidbody.position.y, transform.position.z);
            set => _rigidbody.position = value;
        }

        public override Quaternion Rotation
        {
            get => Quaternion.Euler(0f, 0f, _rigidbody.rotation);
            set => _rigidbody.rotation = value.eulerAngles.z;
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
            get => new Vector3(0f, 0f, _rigidbody.angularVelocity);
            set => _rigidbody.angularVelocity = value.z;
        }

        public override void Interpolate(Vector3 position) => _rigidbody.MovePosition(position);
        public override void Interpolate(Quaternion rotation) => _rigidbody.MoveRotation(rotation.eulerAngles.z);

        public override Vector3 GetPointVelocity(Vector3 point) => _rigidbody.GetPointVelocity(point);

        public override void AddForceToRigidbody(Vector3 force, ForceMode forceMode = ForceMode.Force)
        {
            ForceMode2D forceMode2D = ForceMode2D.Force;

            if (forceMode == ForceMode.Impulse || forceMode == ForceMode.VelocityChange)
                forceMode2D = ForceMode2D.Impulse;

            _rigidbody.AddForce(force, forceMode2D);
        }

        public override void AddExplosionForceToRigidbody(float explosionForce, Vector3 explosionPosition, float explosionRadius, float upwardsModifier = 0) 
            => Debug.LogWarning("AddExplosionForce is not available for 2D physics");

    }

}
