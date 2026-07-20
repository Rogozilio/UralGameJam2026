using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;
using UralGameJam.Ecs.Animation;

namespace UralGameJam.Ecs.Player
{
    [UpdateInGroup(typeof(PresentationSystemGroup))]
    [UpdateBefore(typeof(PlayerCameraSystem))]
    public sealed partial class PlayerRespawnSystem : SystemBase
    {
        protected override void OnCreate()
        {
            RequireForUpdate<PlayerTag>();
            RequireForUpdate<SpawnBoxComponent>();
        }

        protected override void OnStartRunning()
        {
            var player = SystemAPI.GetSingletonEntity<PlayerTag>();
            var view = EntityManager.GetComponentObject<PlayerViewComponent>(player);

            EntityManager.AddComponentData(player, new PlayerRespawnComponent
            {
                position = view.restartPosition,
                rotation = view.restartRotation,
                renderRotation = view.restartRenderRotation
            });
            EntityManager.SetComponentEnabled<PlayerRespawnComponent>(player, false);
        }

        protected override void OnStopRunning()
        {
            var player = SystemAPI.GetSingletonEntity<PlayerTag>();

            if (EntityManager.HasComponent<LockedMoveTag>(player))
                EntityManager.SetComponentEnabled<LockedMoveTag>(player, false);

            EntityManager.RemoveComponent<PlayerFinishRespawnRequest>(player);
            EntityManager.RemoveComponent<PlayerRespawnComponent>(player);
        }

        protected override void OnUpdate()
        {
            var ecbSingleton = SystemAPI.GetSingleton<BeginPresentationEntityCommandBufferSystem.Singleton>();
            var ecb = ecbSingleton.CreateCommandBuffer(World.Unmanaged);
            if (SystemAPI.ManagedAPI.TryGetSingleton(out SpawnBoxComponent spawnBox))
            {
                new ChangeSpawnJob
                {
                    spawnPosition = spawnBox.target.position,
                    spawnRotation = spawnBox.target.rotation
                }.Run();
            }
            
            new RespawnJob().Run();
            new CompleteRestartJob
            {
                ecb = ecb
            }.Run();
            new FinishRespawnJob
            {
                ecb = ecb
            }.Run();
        }

        [WithAll(typeof(PlayerRespawnComponent))]
        public partial struct ChangeSpawnJob : IJobEntity
        {
            [ReadOnly] public Vector3 spawnPosition;
            [ReadOnly] public Quaternion spawnRotation;
            public void Execute(ref PlayerRespawnComponent respawn)
            {
                respawn.position = spawnPosition;
                respawn.rotation = spawnRotation;
            }
        }

        [WithAll(typeof(PlayerRespawnComponent))]
        [WithPresent(typeof(LockedMoveTag))]
        public partial struct RespawnJob : IJobEntity
        { 
            public void Execute(PlayerViewComponent view, in PlayerRespawnComponent respawn,
                EnabledRefRW<LockedMoveTag> lockedMove, DynamicBuffer<AnimatorCommand> animatorCommands)
            {
                animatorCommands.Add(AnimatorCommand.CrossFade(PlayerAnimatorHashes.Respawn, 0f));
                
                lockedMove.ValueRW = true;

                view.characterController.enabled = false;
                view.owner.transform.SetPositionAndRotation(respawn.position, respawn.rotation);

                view.render.localRotation = respawn.renderRotation;
                view.characterController.enabled = true;
            }
        }

        [WithAll(typeof(PlayerRespawnComponent))]
        public partial struct CompleteRestartJob : IJobEntity
        {
            public EntityCommandBuffer ecb;

            public void Execute(Entity entity)
            {
                ecb.SetComponentEnabled<PlayerRespawnComponent>(entity, false);
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
