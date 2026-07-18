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
            _entityManager.RemoveComponent<T>(_entity);
        }

        protected void AddDisabledComponent<T>()
            where T : unmanaged, IComponentData, IEnableableComponent
        {
            _entityManager.AddComponent<T>(_entity);
            _entityManager.SetComponentEnabled<T>(_entity, false);
        }

        protected void AddDisabledComponent<T>(T component)
            where T : unmanaged, IComponentData, IEnableableComponent
        {
            _entityManager.AddComponentData(_entity, component);
            _entityManager.SetComponentEnabled<T>(_entity, false);
        }

        protected void EnableComponent<T>()
            where T : unmanaged, IComponentData, IEnableableComponent
        {
            if (_entityManager.HasComponent<T>(_entity))
                _entityManager.SetComponentEnabled<T>(_entity, true);
        }

        protected void AddRequest<T>()
            where T : unmanaged, IComponentData
        {
            _entityManager.AddComponent<T>(_entity);
        }

    }
}
