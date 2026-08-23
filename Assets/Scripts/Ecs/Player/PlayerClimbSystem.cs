using Unity.Collections;
using Unity.Entities;
using UnityEngine;
using UralGameJam.Ecs.Animation;
using UralGameJam.Ecs.Game;
using UralGameJam.Ecs.Physics3D;

namespace UralGameJam.Ecs.Player
{
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(PlayerCameraSystem))]
    public sealed partial class PlayerClimbSystem : SystemBase
    {
        protected override void OnCreate()
        {
            RequireForUpdate<InputComponent>();
            RequireForUpdate<PlayerTag>();
        }

        protected override void OnStartRunning()
        {
            var player = SystemAPI.GetSingletonEntity<PlayerTag>();

            EntityManager.AddComponent<PlayerClimbTargetComponent>(player);
        }

        protected override void OnStopRunning()
        {
            var player = SystemAPI.GetSingletonEntity<PlayerTag>();

            EntityManager.RemoveComponent<PlayerFinishClimbRequest>(player);
            EntityManager.RemoveComponent<PlayerClimbRequestTag>(player);
            EntityManager.RemoveComponent<PlayerClimbTargetComponent>(player);
        }

        protected override void OnUpdate()
        {
            if (SystemAPI.TryGetSingletonBuffer<PhysicsEventTriggerComponent>(out var triggerEvents, true))
                HandleClimbTriggers(triggerEvents);

            var ecbSingleton = SystemAPI.GetSingleton<BeginPresentationEntityCommandBufferSystem.Singleton>();
            var ecb = ecbSingleton.CreateCommandBuffer(World.Unmanaged);
            
            var input = SystemAPI.GetSingleton<InputComponent>();

            var climbLookup = SystemAPI.GetComponentLookup<ClimbComponent>(true);
            
            new PlayerClimbJob
            {
                ecb = ecb,
                input = input,
                climbLookup = climbLookup
            }.Run();
            new PlayerFinishClimbJob
            {
                ecb = ecb,
                input = input,
                climbLookup = climbLookup
            }.Run();
            new RestartClimbJob
            {
                ecb = ecb
            }.Run();
        }

        private void HandleClimbTriggers(DynamicBuffer<PhysicsEventTriggerComponent> triggerEvents)
        {
            var climbEntity = Entity.Null;
            var playerEntity = Entity.Null;

            for (var i = 0; i < triggerEvents.Length; i++)
            {
                var triggerEvent = triggerEvents[i];

                if (triggerEvent.isExit)
                    continue;

                if (PhysicsUtility.TryGetPair<ClimbTag, PlayerTag>(
                        EntityManager, triggerEvent.entityA, triggerEvent.entityB,
                        out var currentClimbEntity, out var currentPlayerEntity))
                {
                    climbEntity = currentClimbEntity;
                    playerEntity = currentPlayerEntity;
                }
            }

            if (playerEntity == Entity.Null)
                return;

            EntityManager.SetComponentData(playerEntity, new PlayerClimbTargetComponent
            {
                target = climbEntity
            });
            EntityManager.AddComponent<PlayerClimbRequestTag>(playerEntity);
        }

        [WithAll(typeof(PlayerTag), typeof(PlayerClimbRequestTag))]
        [WithDisabled(typeof(LockedMoveTag), typeof(LifeTimePausedTag))]
        public partial struct PlayerClimbJob : IJobEntity
        {
            public EntityCommandBuffer ecb;
            [ReadOnly] public InputComponent input;
            [ReadOnly] public ComponentLookup<ClimbComponent> climbLookup;

            public void Execute(Entity entity, ref PlayerClimbTargetComponent climbTarget, PlayerViewComponent view,
                DynamicBuffer<AnimatorCommand> animatorCommands, ref AnimatorStateComponent animatorState, 
                ref PlayerMovementComponent movement, EnabledRefRW<LockedMoveTag> lockedMove,
                EnabledRefRW<LifeTimePausedTag> lifeTimePaused)
            {
                ecb.RemoveComponent<PlayerClimbRequestTag>(entity);

                var target = climbLookup[climbTarget.target];

                var isLookingAt = Vector3.Dot(-view.render.right, target.forward) > 0.5f;
                var isPlayerHigher = view.render.position.y > target.position.y;

                if (!isLookingAt || isPlayerHigher)
                    return;

                lockedMove.ValueRW = true;
                animatorState.applyRootMotion = true;
                lifeTimePaused.ValueRW = true;
                view.characterController.enabled = false;

                animatorCommands.Add(AnimatorCommand.CrossFade(PlayerAnimatorHashes.ClimbState, 0.1f));
                view.owner.transform.position = GetPointClimb(target.startPosition, target.startRight, target.range,
                    view.owner.transform.position);
                var targetRenderRotation = target.startRotation * Quaternion.Euler(270f, 90f, 0f);
                view.owner.transform.rotation =
                    targetRenderRotation * Quaternion.Inverse(view.restartRenderRotation);
                
                ResetMovementState(ref movement, input.jumpHeld);
            }
        }

        [WithAll(typeof(PlayerTag), typeof(PlayerFinishClimbRequest))]
        [WithOptions(EntityQueryOptions.IgnoreComponentEnabledState)]
        public partial struct PlayerFinishClimbJob : IJobEntity
        {
            public EntityCommandBuffer ecb;
            [ReadOnly] public InputComponent input;
            [ReadOnly] public ComponentLookup<ClimbComponent> climbLookup;

            public void Execute(Entity entity, PlayerViewComponent view, ref PlayerClimbTargetComponent climbTarget,
                DynamicBuffer<AnimatorCommand> animatorCommands, ref AnimatorStateComponent animatorState, 
                ref PlayerMovementComponent movement, EnabledRefRW<LockedMoveTag> lockedMove,
                EnabledRefRW<LifeTimePausedTag> lifeTimePaused)
            {
                ecb.RemoveComponent<PlayerFinishClimbRequest>(entity);

                var target = climbLookup[climbTarget.target];
                
                climbTarget.target = Entity.Null;
                
                lockedMove.ValueRW = false;
                animatorState.applyRootMotion = false;
                lifeTimePaused.ValueRW = false;
               
                animatorCommands.Add(AnimatorCommand.SetTrigger(PlayerAnimatorHashes.IsClimb));
                view.owner.transform.position = GetPointClimb(target.finishPosition, target.finishRight, target.range,
                    view.owner.transform.position);
                view.characterController.enabled = true;
                
                ResetMovementState(ref movement, input.jumpHeld);
            }
        }

        private static Vector3 GetPointClimb(Vector3 position, Vector3 right, float range, Vector3 playerPosition)
        {
            var offset = Vector3.Dot(playerPosition - position, right);
            var clampedOffset = Mathf.Clamp(offset, -range, range);

            return position + right * clampedOffset;
        }

        private static void ResetMovementState(ref PlayerMovementComponent movement, bool blockJumpUntilRelease)
        {
            movement.velocityY = 0f;
            movement.coyoteTimeCounter = 0f;
            movement.jumpBufferCounter = 0f;
            movement.blockJumpUntilRelease = blockJumpUntilRelease;
        }
        
        [WithAll(typeof(PlayerRespawnComponent))]
        public partial struct RestartClimbJob : IJobEntity
        {
            public EntityCommandBuffer ecb;

            public void Execute(Entity entity, ref PlayerClimbTargetComponent climbTarget,
                ref AnimatorStateComponent animatorState)
            {
                animatorState.applyRootMotion = false;
                climbTarget.target = Entity.Null;
                ecb.RemoveComponent<PlayerClimbRequestTag>(entity);
                ecb.RemoveComponent<PlayerFinishClimbRequest>(entity);
            }
        }
    }
}
