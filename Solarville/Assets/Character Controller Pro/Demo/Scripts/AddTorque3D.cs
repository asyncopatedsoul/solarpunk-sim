using UnityEngine;


namespace Lightbug.CharacterControllerPro.Demo
{
    public class AddTorque3D : AddTorque
    {
        Rigidbody _rigidbody = null;

        protected override void Awake()
        {
            base.Awake();

            _rigidbody = GetComponent<Rigidbody>();
            _rigidbody.maxAngularVelocity = maxAngularVelocity;
        }

        protected override void AddTorqueToRigidbody()
        {
            _rigidbody.AddRelativeTorque(torque);
        }
    }
}
