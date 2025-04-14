using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Lightbug.CharacterControllerPro.Demo
{
    [RequireComponent(typeof(Rigidbody))]
    public class CustomGravity : MonoBehaviour
    {
        public Transform planet;
        public float gravity = 10f;

        Rigidbody _rigidbody;

        private void Awake()
        {
            if (planet == null)
            {
                enabled = false;
                return;
            }

            _rigidbody = GetComponent<Rigidbody>();
            _rigidbody.useGravity = false;
        }

        void FixedUpdate()
        {
            Vector3 dir = (planet.position - transform.position).normalized;

#if UNITY_6000_0_OR_NEWER
            _rigidbody.linearVelocity += dir * gravity * Time.deltaTime;
#else
            _rigidbody.velocity += dir * gravity * Time.deltaTime;
#endif

        }

    }

}

