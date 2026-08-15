using Scripts;
using UralGameJam.Ecs.Animation;
using Unity.Entities;
using UnityEngine;

namespace UralGameJam.Ecs.Physics3D
{
    [InternalBufferCapacity(128)]
    public struct PhysicsEventCollideComponent : IBufferElementData
    {
        public Entity entityA;
        public Entity entityB;
        public bool isExit;
    }
    [InternalBufferCapacity(128)]
    public struct PhysicsEventTriggerComponent : IBufferElementData
    {
        public Entity entityA;
        public Entity entityB;
        public bool isExit;
    }

    public static class PhysicsUtility
    {
        public static bool TryGetPair<TFirst, TSecond>(EntityManager entityManager, Entity entityA, Entity entityB,
            out Entity first, out Entity second) 
            where TFirst : unmanaged, IComponentData 
            where TSecond : unmanaged, IComponentData
        {
            if (entityA == Entity.Null || entityB == Entity.Null ||
                !entityManager.Exists(entityA) || !entityManager.Exists(entityB))
            {
                first = Entity.Null;
                second = Entity.Null;
                return false;
            }

            if (entityManager.HasComponent<TFirst>(entityA) && entityManager.HasComponent<TSecond>(entityB))
            {
                first = entityA;
                second = entityB;
                return true;
            }

            if (entityManager.HasComponent<TFirst>(entityB) && entityManager.HasComponent<TSecond>(entityA))
            {
                first = entityB;
                second = entityA;
                return true;
            }

            first = Entity.Null;
            second = Entity.Null;
            return false;
        }
    }

    public sealed class ColliderProvider : MonoEntity
    {
        private Entity _bufferTriggerEntity;
        
        private void Awake()
        {
            base.Awake();

            var query = _entityManager.CreateEntityQuery(ComponentType.ReadOnly<PhysicsEventTriggerComponent>());

            if (query.IsEmpty)
            {
                _bufferTriggerEntity = _entityManager.CreateEntity();
                _entityManager.AddBuffer<PhysicsEventTriggerComponent>(_bufferTriggerEntity);
                _entityManager.AddBuffer<PhysicsEventCollideComponent>(_bufferTriggerEntity);
                _entityManager.SetName(_bufferTriggerEntity, "CollideAndTriggerEvent");
            }
            else
            {
                _bufferTriggerEntity = query.GetSingletonEntity();
            }
            
            query.Dispose();
        }

        private void OnTriggerEnter(Collider other)
        {
            if(other.TryGetComponent<ColliderReceiver>(out var value))
                AddTriggerToBuffer(value.entity, false);
            
        }

        private void OnTriggerExit(Collider other)
        {
            if(other.TryGetComponent<ColliderReceiver>(out var value))
                AddTriggerToBuffer(value.entity, true);
        }

        private void OnCollisionEnter(Collision other)
        {
            if(other.transform.TryGetComponent<ColliderReceiver>(out var value))
                AddCollideToBuffer(value.entity, false);
        }
        
        private void OnCollisionExit(Collision other)
        {
            if(other.transform.TryGetComponent<ColliderReceiver>(out var value))
                AddCollideToBuffer(value.entity, true);
        }

        private void AddTriggerToBuffer(Entity entityB, bool isExit)
        {
            var buffer = _entityManager.GetBuffer<PhysicsEventTriggerComponent>(_bufferTriggerEntity);
            buffer.Add(new PhysicsEventTriggerComponent
            {
                entityA = _entity,
                entityB = entityB,
                isExit = isExit
            });
        }
        
        private void AddCollideToBuffer(Entity entityB, bool isExit)
        {
            var buffer = _entityManager.GetBuffer<PhysicsEventCollideComponent>(_bufferTriggerEntity);
            buffer.Add(new PhysicsEventCollideComponent
            {
                entityA = _entity,
                entityB = entityB,
                isExit = isExit
            });
        }
    }

    public class ColliderReceiver : MonoEntity
    {
        public Entity entity => _entity;
    }

    [UpdateInGroup(typeof(PresentationSystemGroup), OrderLast = true)]
    [UpdateAfter(typeof(AnimatorSystem))]
    public sealed partial class PhysicsSystem : SystemBase
    {
        protected override void OnCreate()
        {
            RequireForUpdate<PhysicsEventTriggerComponent>();
        }

        protected override void OnUpdate()
        {
            var triggerBuffer = SystemAPI.GetSingletonBuffer<PhysicsEventTriggerComponent>();
            var collideBuffer = SystemAPI.GetSingletonBuffer<PhysicsEventCollideComponent>();

            triggerBuffer.Clear();
            collideBuffer.Clear();
        }
    }
}
