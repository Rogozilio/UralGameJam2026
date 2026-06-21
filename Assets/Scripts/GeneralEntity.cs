using Unity.Entities;
using UnityEngine;

namespace Scripts
{
    public sealed class GeneralEntity : MonoBehaviour
    {
        private Entity _entity;
        private EntityManager _entityManager;

        private void Awake()
        {
            _entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;
        }

        public Entity GetOrCreate()
        {
            if (_entityManager.Exists(_entity))
                return _entity;
            
            _entity = _entityManager.CreateEntity();
            _entityManager.SetName(_entity, gameObject.name);

            return _entity;
        }

        private void OnDestroy()
        {
            if (_entityManager.Exists(_entity))
                _entityManager.DestroyEntity(_entity);
        }
    }
}
