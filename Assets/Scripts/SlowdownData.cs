using UralGameJam.Ecs.Physics3D;
using UralGameJam.Ecs.Player;
using Unity.Entities;
using UnityEngine;

namespace Scripts
{
    public sealed class SlowdownData : MonoBehaviour
    {
        private Entity _slowdownEntity;
        private EntityManager _entityManager;

        private void Awake()
        {
            _entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;
            _slowdownEntity = _entityManager.CreateEntity(typeof(SlowdownTag));

            var provider = GetComponent<ColliderAndTriggerDOTSProvider>();

            if (provider == null)
                provider = gameObject.AddComponent<ColliderAndTriggerDOTSProvider>();

            provider.entityA = _slowdownEntity;
        }

        private void OnDestroy()
        {
            if (_entityManager != default && _entityManager.Exists(_slowdownEntity))
                _entityManager.DestroyEntity(_slowdownEntity);

            _slowdownEntity = Entity.Null;
        }
    }
}
