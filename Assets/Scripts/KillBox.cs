using Unity.Entities;
using UnityEngine;
using UralGameJam.Ecs.Physics3D;

namespace Scripts
{
    public struct KillBoxTag : IComponentData
    {
    }
    public class KillBox : MonoBehaviour
    {
        private Entity _killBoxEntity;
        private EntityManager _entityManager;

        private void Awake()
        {
            _entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;
            _killBoxEntity = _entityManager.CreateEntity(typeof(KillBoxTag));
        
            EnsureTriggerBridge();
        }
        
        private void OnDestroy()
        {
            if (_entityManager.Exists(_killBoxEntity))
                _entityManager.DestroyEntity(_killBoxEntity);
        }

        private void EnsureTriggerBridge()
        {
            var provider = GetComponent<ColliderAndTriggerDOTSProvider>();

            if (provider == null)
                provider = gameObject.AddComponent<ColliderAndTriggerDOTSProvider>();

            provider.entityA = _killBoxEntity;
        }
    }
}
