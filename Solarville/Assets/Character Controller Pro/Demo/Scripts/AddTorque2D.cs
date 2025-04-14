using UnityEngine;

namespace Lightbug.CharacterControllerPro.Demo
{
    public class AddTorque2D : AddTorque
    {
        Rigidbody2D _rigidbody = null;

        protected override void Awake()
        {
            base.Awake();

            _rigidbody = GetComponent<Rigidbody2D>();
        }

        protected override void AddTorqueToRigidbody()
        {
            _rigidbody.AddTorque(torque.z);
            _rigidbody.angularVelocity = Mathf.Clamp(_rigidbody.angularVelocity, -maxAngularVelocity, maxAngularVelocity);
        }
    }
}
