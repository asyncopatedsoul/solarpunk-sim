
using NWH.Common.Vehicles;
using System.Collections.Generic;
using UnityEngine;
using NWH.NUI;
using NWH.WheelController3D;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace NWH.WheelController3D
{
    [DefaultExecutionOrder(60)]
    public class WheelControllerManager : MonoBehaviour
    {
        /// <summary>
        /// Should wheels on bodies connected to this one through joints
        /// be woken up when any wheel on this body is active.
        /// </summary>
        public bool wakeWheelsOnConnectedBodies = true;

        private List<WheelUAPI> _wheels = new List<WheelUAPI>();
        private int _wheelCount;
        private Rigidbody _rb;
        private Joint[] _joints;

        private void Awake()
        {
            _rb = GetComponent<Rigidbody>();
            _wheels = new List<WheelUAPI>();
            _wheelCount = 0;
        }


        private void FixedUpdate()
        {
            bool requiresWake = false;
            for (int i = 0; i < _wheels.Count; i++)
            {
                float motorTorque = _wheels[i].MotorTorque;
                if (motorTorque > Vehicle.KINDA_SMALL_NUMBER || motorTorque < -Vehicle.KINDA_SMALL_NUMBER)
                {
                    requiresWake = true;
                    break;
                }
            }

            if (requiresWake)
            {
                WakeAllWheels();
            }
        }


        public void WakeAllWheels()
        {
            int wheelCount = _wheels.Count;
            if (wheelCount > 0)
            {
                for (int i = 0; i < _wheels.Count; i++)
                {
                    _wheels[i].WakeFromSleep();
                }
            }

            // Wake wheels on connected bodies
            if (wakeWheelsOnConnectedBodies)
            {
                _joints = _rb.GetComponentsInChildren<Joint>();
                foreach (var joint in _joints)
                {
                    if (joint.connectedBody != null && joint.connectedBody != _rb)
                    {
                        WheelControllerManager connectedWCM = joint.connectedBody.GetComponent<WheelControllerManager>();
                        if (connectedWCM)
                        {
                            connectedWCM.WakeAllWheels();
                        }
                    }
                }
            }
        }


        public void Register(WheelUAPI wheel)
        {
            if (!_wheels.Contains(wheel))
            {
                _wheels.Add(wheel);
                _wheelCount++;
            }
        }


        public void Deregister(WheelUAPI wheel)
        {
            if (_wheels.Contains(wheel))
            {
                _wheels.Remove(wheel);
                _wheelCount--;
            }
        }
    }
}


#if UNITY_EDITOR
/// <summary>
/// Editor for WheelControllerManager.
/// </summary>
[CustomEditor(typeof(WheelControllerManager))]
[CanEditMultipleObjects]
public class WheelControllerManagerEditor : NUIEditor
{
    public override bool OnInspectorNUI()
    {
        if (!base.OnInspectorNUI()) return false;

        drawer.Field("wakeWheelsOnConnectedBodies");

        drawer.EndEditor(this);
        return true;
    }
}
#endif