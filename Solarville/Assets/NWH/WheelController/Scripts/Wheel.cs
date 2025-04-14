using System;
using System.ComponentModel;
using UnityEngine;
using UnityEngine.Events;

namespace NWH.WheelController3D
{
    /// <summary>
    ///     Contains everything wheel related, including rim and tire.
    /// </summary>
    [Serializable]
    public class Wheel
    {
        /// <summary>
        /// !OBSOLETE! - Check the v11 to v12 upgrade notes! Parent the visual(s) to the rotatingContainer instead.
        ///     GameObject representing the visual aspect of the wheel / wheel mesh.
        ///     Should not have any physics colliders attached to it.
        /// </summary>
        [Obsolete]
        [Tooltip(
            "!OBSOLETE! - Check the v11 to v12 upgrade notes! Parent the visual(s) to the rotatingContainer instead." +
            "GameObject representing the visual aspect of the wheel / wheel mesh.\r\nShould not have any physics colliders attached to it.")]
        public GameObject visual;

        /// <summary>
        /// !OBSOLETE! - Check the v11 to v12 upgrade notes! Parent the visual to the rotatingContainer instead.
        ///     Object representing non-rotating part of the wheel. This could be things such as brake calipers, external fenders,
        ///     etc.
        /// </summary>
        [Obsolete]
        [Tooltip(
            "!OBSOLETE! - Check the v11 to v12 upgrade notes! Parent the visual to the rotatingContainer instead." +
            "Object representing non-rotating part of the wheel. This could be things such as brake calipers, external fenders, etc.")]
        public GameObject nonRotatingVisual;

        /// <summary>
        /// Container to which all the rotating wheel parts should be parented,
        /// such as wheels, tires, and brake discs.
        /// </summary>
        [UnityEngine.Tooltip("Container to which all the rotating wheel parts should be parented, such as wheels, tires, and brake discs.")]
        public Transform rotatingContainer;

        /// <summary>
        /// Container to which all the non-rotating wheel parts should be parented,
        /// such as fenders and brake calipers.
        /// </summary>
        [UnityEngine.Tooltip("Container to which all the non-rotating wheel parts should be parented, such as fenders and brake calipers.")]
        public Transform nonRotatingContainer;

        /// <summary>
        /// Rotation of the wheel including axle rotation, camber rotation, damage rotation, and any other rotation
        /// around the wheel axle. Does not include steer rotation.
        /// </summary>
        public Quaternion axleRotation;

        /// <summary>
        /// Collider covering the top half of the wheel. 
        /// </summary>
        [UnityEngine.Tooltip("Collider covering the top half of the wheel. ")]
        public MeshCollider meshCollider;

        /// <summary>
        ///     Current angular velocity of the wheel in rad/s.
        /// </summary>
        [Tooltip("    Current angular velocity of the wheel in rad/s.")]
        public float angularVelocity;

        /// <summary>
        ///     Current wheel RPM.
        /// </summary>
        public float rpm
        {
            get { return angularVelocity * 9.55f; }
        }

        /// <summary>
        /// Total inertia of the wheel and any attached components.
        /// </summary>
        public float inertia;

        /// <summary>
        /// Cached value of the inverse of the wheel's inertia.
        /// </summary>
        public float invInertia;

        /// <summary>
        ///     Mass of the wheel. Inertia is calculated from this.
        ///     Call Initialize() after changing the value!
        /// </summary>
        [Tooltip("Mass of the wheel. Inertia is calculated from this.")]
        public float mass = 30.0f;

        /// <summary>
        ///     Total radius of the tire in [m].
        ///     Call Initialize() after changing the value!
        /// </summary>
        [Tooltip("Total radius of the tire in [m].")]
        [Min(0.001f)]
        public float radius = 0.35f;

        /// <summary>
        /// Cached value of the inverse of the wheel's radius.
        /// </summary>
        public float invRadius = 3.0f;

        /// <summary>
        ///     Current rotation angle of the wheel visual in regards to it's X axis vector.
        /// </summary>
        [Tooltip("    Current rotation angle of the wheel visual in regards to it's X axis vector.")]
        [NonSerialized]
        public float axleAngle;

        /// <summary>
        ///     Width of the tyre.
        ///     Call Initialize() after changing the value!
        /// </summary>
        [Tooltip("    Width of the tyre.")]
        [Min(0.001f)]
        public float width = 0.25f;

        /// <summary>
        /// Offset of the rim from the axle, in m.
        /// </summary>
        [Tooltip("Offset of the rim from the axle, in m.")]
        public float rimOffset = 0.1f;

        /// <summary>
        /// Angular velocity during the previus FixedUpdate().
        /// </summary>
        [NonSerialized]
        [UnityEngine.Tooltip("Angular velocity during the previus FixedUpdate().")]
        public float prevAngularVelocity;


        /// <summary>
        /// Called when either radius or width of the wheel change.
        /// </summary>
        [NonSerialized]
        [UnityEngine.Tooltip("Called when either radius or width of the wheel change.")]
        public UnityEvent onWheelDimensionsChange = new UnityEvent();
    }
}