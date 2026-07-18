using Unity.Entities;
using UnityEngine;

namespace UralGameJam.Ecs.Player
{
    [UpdateInGroup(typeof(PresentationSystemGroup))]
    [UpdateBefore(typeof(PlayerCameraSystem))]
    public sealed partial class PlayerRestartBeginSystem : SystemBase
    {
        protected override void OnUpdate()
        {
            new BeginRestartJob().Run();
        }

        [WithAll(typeof(PlayerRestartComponent))]
        public partial struct BeginRestartJob : IJobEntity
        {
            public void Execute(PlayerViewComponent view, in PlayerRestartComponent restart,
                EnabledRefRW<LockedMoveTag> lockedMove)
            {
                lockedMove.ValueRW = true;

                view.owner.transform.SetPositionAndRotation(
                    restart.position,
                    restart.rotation * Quaternion.Euler(0f, 180f, 0f));

                view.render.localRotation = restart.renderRotation;
            }
        }
    }

    [UpdateInGroup(typeof(PresentationSystemGroup))]
    [UpdateAfter(typeof(PlayerRestartBeginSystem))]
    [UpdateBefore(typeof(PlayerDeathSystem))]
    public sealed partial class PlayerRestartSystem : SystemBase
    {
        protected override void OnCreate()
        {
            RequireForUpdate<PlayerTag>();
        }

        protected override void OnStartRunning()
        {
            var player = SystemAPI.GetSingletonEntity<PlayerTag>();
            var view = EntityManager.GetComponentObject<PlayerViewComponent>(player);

            EntityManager.AddComponentData(player, new PlayerRestartComponent
            {
                position = view.restartPosition,
                rotation = view.restartRotation,
                renderRotation = view.restartRenderRotation
            });
            EntityManager.SetComponentEnabled<PlayerRestartComponent>(player, false);
        }

        protected override void OnStopRunning()
        {
            var player = SystemAPI.GetSingletonEntity<PlayerTag>();

            if (EntityManager.HasComponent<LockedMoveTag>(player))
                EntityManager.SetComponentEnabled<LockedMoveTag>(player, false);

            EntityManager.RemoveComponent<PlayerFinishRespawnRequest>(player);
            EntityManager.RemoveComponent<PlayerRestartComponent>(player);
        }

        protected override void OnUpdate()
        {
            var ecbSingleton = SystemAPI.GetSingleton<BeginPresentationEntityCommandBufferSystem.Singleton>();
            var ecb = ecbSingleton.CreateCommandBuffer(World.Unmanaged);

            new CompleteRestartJob
            {
                ecb = ecb
            }.Run();
            new FinishRespawnJob
            {
                ecb = ecb
            }.Run();
        }

        [WithAll(typeof(PlayerRestartComponent))]
        public partial struct CompleteRestartJob : IJobEntity
        {
            public EntityCommandBuffer ecb;

            public void Execute(Entity entity)
            {
                ecb.SetComponentEnabled<PlayerRestartComponent>(entity, false);
            }
        }

        [WithAll(typeof(PlayerFinishRespawnRequest))]
        public partial struct FinishRespawnJob : IJobEntity
        {
            public EntityCommandBuffer ecb;

            public void Execute(Entity entity, PlayerViewComponent view, EnabledRefRW<LockedMoveTag> lockedMove)
            {
                lockedMove.ValueRW = false;
                ecb.RemoveComponent<PlayerFinishRespawnRequest>(entity);
            }
        }
    }
}
