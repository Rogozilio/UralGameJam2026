using Unity.Burst;
using Unity.Entities;
using Unity.Jobs;
using UralGameJam.Ecs.Physics3D;

namespace UralGameJam.Ecs.Player
{
    [UpdateAfter(typeof(PlayerBlendShapeSystem))]
    public partial class AreaRefireSystem : SystemBase
    {
        protected override void OnCreate()
        {
            RequireForUpdate<AreaRefireTag>();
        }

        protected override void OnUpdate()
        {
            var ecbSingleton = SystemAPI.GetSingleton<EndFixedStepSimulationEntityCommandBufferSystem.Singleton>();
            var ecb = ecbSingleton.CreateCommandBuffer(EntityManager.WorldUnmanaged);
            
            if (SystemAPI.TryGetSingletonBuffer<PhysicsEventTriggerComponent>(out var triggerEvents, true))
                HandleAreaRefireTriggers(ecb, triggerEvents);

            new StartFirePlayerJob()
            {
                ecb = ecb
            }.Run();
            new EndFirePlayerJob()
            {
                ecb = ecb
            }.Run();
        }
        
        private void HandleAreaRefireTriggers(EntityCommandBuffer ecb, DynamicBuffer<PhysicsEventTriggerComponent> triggerEvents)
        {
            foreach (var triggerEvent in triggerEvents)
            {
                if (PhysicsUtility.TryGetPair<PlayerTag, AreaRefireTag>(EntityManager, triggerEvent.entityA, 
                        triggerEvent.entityB, out var player, out _))
                {
                    if (triggerEvent.isExit)
                    {
                        ecb.AddComponent<StartFireTag>(player);
                    }
                    else
                    {
                        ecb.AddComponent<EndFireTag>(player);
                    }
                }
            }
        }

        [BurstCompile]
        [WithAll(typeof(StartFireTag))]
        public partial struct StartFirePlayerJob : IJobEntity
        {
            public EntityCommandBuffer ecb;
            public void Execute(Entity entity, ref BlendShapeComponent blendShape, BlendShapeViewComponent view)
            {
                ecb.SetComponentEnabled<LifeTimePausedTag>(entity, false);
                view.fire.Play();
                
                ecb.RemoveComponent<StartFireTag>(entity);
            }
        }
        
        [BurstCompile]
        [WithAll(typeof(EndFireTag))]
        public partial struct EndFirePlayerJob : IJobEntity
        {
            public EntityCommandBuffer ecb;
            public void Execute(Entity entity, ref BlendShapeComponent blendShape, ref LifeTimeComponent liefTime, BlendShapeViewComponent view)
            {
                liefTime.remainingTime = liefTime.duration;
                
                ecb.SetComponentEnabled<LifeTimePausedTag>(entity, true);
                view.fire.Stop();
                view.fire.Clear();
                
                ecb.RemoveComponent<EndFireTag>(entity);
            }
        }
    }
}