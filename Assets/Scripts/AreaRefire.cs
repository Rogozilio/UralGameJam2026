using System;
using Scripts;
using UnityEngine;
using UralGameJam.Ecs.Player;

namespace DefaultNamespace
{
    public class AreaRefire : MonoEntity
    {
        public SphereCollider sphereCollider;

        public float radius;

        protected override void Awake()
        {
            base.Awake();

            _entityManager.AddComponent<AreaRefireTag>(_entity);
        }

        private void OnValidate()
        {
            sphereCollider ??= GetComponent<SphereCollider>();

            sphereCollider.radius = radius;
        }

        private void OnDestroy()
        {
            _entityManager.RemoveComponent<AreaRefireTag>(_entity);
        }
    }
}