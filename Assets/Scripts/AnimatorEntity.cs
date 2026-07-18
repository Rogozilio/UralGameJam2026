using Unity.Entities;
using UnityEngine;
using UralGameJam.Ecs.Animation;

namespace Scripts
{
    [DisallowMultipleComponent]
    public sealed class AnimatorEntity : MonoEntity
    {
        public Animator animator;

        [SerializeField]
        private bool controlRootMotion = true;

        public Animator Animator => animator;

        protected override void Awake()
        {
            animator ??= GetComponentInChildren<Animator>();

            if (animator == null)
                throw new MissingReferenceException(
                    $"{nameof(AnimatorEntity)}.{nameof(animator)} is not assigned on {name}");

            base.Awake();

            if (controlRootMotion)
                animator.applyRootMotion = false;

            _entityManager.AddComponentObject(_entity, new AnimatorViewComponent { animator = animator });
            _entityManager.AddComponentData(_entity, new AnimatorStateComponent());
            _entityManager.AddBuffer<AnimatorCommand>(_entity);
        }

        private void OnDestroy()
        {
            _entityManager.RemoveComponent(_entity, ComponentType.ReadWrite<AnimatorCommand>());
            RemoveComponent<AnimatorStateComponent>();
            RemoveComponent<AnimatorViewComponent>();
        }

        private void OnAnimatorMove()
        {
            if (!controlRootMotion || !_entityManager.HasComponent<AnimatorStateComponent>(_entity))
            {
                return;
            }

            var state = _entityManager.GetComponentData<AnimatorStateComponent>(_entity);
            if (state.applyRootMotion)
                animator.ApplyBuiltinRootMotion();
        }
    }
}
