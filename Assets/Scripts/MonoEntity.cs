using Unity.Entities;
using UnityEngine;

namespace Scripts
{
    public abstract class MonoEntity : MonoBehaviour
    {
        protected Entity _entity;
        protected EntityManager _entityManager;

        protected virtual void Awake()
        {
            _entity = TryGetComponent(out GeneralEntity entity) 
                ? entity.GetOrCreate() 
                : gameObject.AddComponent<GeneralEntity>().GetOrCreate();
            
            _entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;
        }

        protected void RemoveComponent<T>() where T : IComponentData
        {
            if (_entityManager.HasComponent<T>(_entity))
                _entityManager.RemoveComponent<T>(_entity);
        }

        protected void SendAnimationRequest<T>()
            where T : unmanaged, IComponentData, IEnableableComponent
        {
            if (_entityManager.HasComponent<T>(_entity))
                _entityManager.SetComponentEnabled<T>(_entity, true);
        }

    }
}
