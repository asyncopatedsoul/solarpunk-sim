﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Lightbug.Utilities
{

    /// <summary>
    /// This component represents a capsule collider in a 3D world.
    /// </summary>
    public class CapsuleColliderComponent3D : ColliderComponent3D
    {
        CapsuleCollider capsuleCollider = null;

        public override Vector3 Size
        {
            get
            {
                return new Vector2(2f * capsuleCollider.radius, capsuleCollider.height);
            }
            set
            {
                capsuleCollider.radius = value.x / 2f;
                capsuleCollider.height = value.y;
            }
        }

        public override Vector3 BoundsSize => capsuleCollider.bounds.size;

        public override Vector3 Offset
        {
            get => capsuleCollider.center;
            set => capsuleCollider.center = value;
        }

        protected override int InternalOverlapBody(Vector3 position, Quaternion rotation, Collider[] unfilteredResults, List<Collider> filteredResults, OverlapFilterDelegate3D filter)
        {
            var up = rotation * Vector3.up;
            var centerToTopCenter = (0.5f * capsuleCollider.height - capsuleCollider.radius) * up;
            var center = position + rotation * capsuleCollider.center;
            var bottom = center + centerToTopCenter;
            var top = center - centerToTopCenter;

            var overlaps = Physics.OverlapCapsuleNonAlloc(
                bottom,
                top,
                capsuleCollider.radius,
                unfilteredResults,
                Physics.DefaultRaycastLayers,
                QueryTriggerInteraction.Ignore
            );

            return FilterValidOverlaps(overlaps, unfilteredResults, filteredResults, filter);
        }

        protected override void Awake()
        {

            capsuleCollider = gameObject.GetOrAddComponent<CapsuleCollider>();
            Collider = capsuleCollider;

            base.Awake();

        }

    }

}
