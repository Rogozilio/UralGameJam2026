using Unity.Collections;
using Unity.Entities;
using UnityEngine;
using UralGameJam.Ecs.Animation;
using UralGameJam.Ecs.Game;
using UralGameJam.Ecs.Physics3D;

namespace UralGameJam.Ecs.Player
{
    [UpdateInGroup(typeof(PresentationSystemGroup))]
    [UpdateAfter(typeof(PlayerClimbSystem))]
    public sealed partial class PlayerMovementSystem : SystemBase
    {
        protected override void OnCreate()
        {
            RequireForUpdate<InputComponent>();
            RequireForUpdate<PlayerTag>();
        }

        protected override void OnStartRunning()
        {
            var player = SystemAPI.GetSingletonEntity<PlayerTag>();
            var view = EntityManager.GetComponentObject<PlayerViewComponent>(player);

            EntityManager.AddComponentData(player, new PlayerMovementComponent
            {
                moveSpeed = view.moveSpeed,
                jumpHeight = view.jumpHeight,
                gravity = view.gravity,
                jumpBufferTime = view.jumpBufferTime,
                fallGravityMultiplier = view.fallGravityMultiplier,
                coyoteTime = view.coyoteTime,
                speedSlowdown = PlayerConstants.DefaultSlowdownMultiplier
            });
            EntityManager.AddComponent<LockedMoveTag>(player);
            EntityManager.AddComponent<PlayerChangeSpeedComponent>(player);
            EntityManager.AddComponent<PlayerDisableJumpTag>(player);
            EntityManager.SetComponentEnabled<LockedMoveTag>(player, false);
            EntityManager.SetComponentEnabled<PlayerChangeSpeedComponent>(player, false);
            EntityManager.SetComponentEnabled<PlayerDisableJumpTag>(player, false);
            new InitializeAnimatorJob().Run();
        }

        public partial struct InitializeAnimatorJob : IJobEntity
        {
            public void Execute(DynamicBuffer<AnimatorCommand> animatorCommands, in PlayerStateComponent state)
            {
                animatorCommands.Add(AnimatorCommand.SetBool(PlayerAnimatorHashes.IsIdleFire, state.isIdleFire));
            }
        }

        protected override void OnStopRunning()
        {
            var player = SystemAPI.GetSingletonEntity<PlayerTag>();
            EntityManager.RemoveComponent<PlayerDisableJumpTag>(player);
            EntityManager.RemoveComponent<PlayerChangeSpeedComponent>(player);
            EntityManager.RemoveComponent<LockedMoveTag>(player);
            EntityManager.RemoveComponent<PlayerMovementComponent>(player);
        }

        protected override void OnUpdate()
        {
            if (SystemAPI.TryGetSingletonBuffer<PhysicsEventTriggerComponent>(out var triggerEvents, true))
                HandleSlowdownTriggers(triggerEvents);

            new PlayerChangeSpeedJob().Run();
            new RestartMovementStateJob().Run();
            new PlayerMovementJob
            {
                input = SystemAPI.GetSingleton<InputComponent>(),
                deltaTime = SystemAPI.Time.DeltaTime,
                disableJumpLookup = SystemAPI.GetComponentLookup<PlayerDisableJumpTag>(true)
            }.Run();
            new RestartMovementJob().Run();
        }

        private void HandleSlowdownTriggers(DynamicBuffer<PhysicsEventTriggerComponent> triggerEvents)
        {
            var playerEntity = Entity.Null;
            var isExit = false;

            for (var i = 0; i < triggerEvents.Length; i++)
            {
                var triggerEvent = triggerEvents[i];

                if (PhysicsUtility.TryGetPair<SlowdownTag, PlayerTag>(
                        EntityManager, triggerEvent.entityA, triggerEvent.entityB,
                        out _, out var currentPlayerEntity))
                {
                    playerEntity = currentPlayerEntity;
                    isExit = triggerEvent.isExit;
                }
            }

            if (playerEntity == Entity.Null)
                return;

            EntityManager.SetComponentData(playerEntity, new PlayerChangeSpeedComponent
            {
                speedMultiply = isExit
                    ? PlayerConstants.DefaultSlowdownMultiplier
                    : PlayerConstants.SlowdownMultiplier
            });
            EntityManager.SetComponentEnabled<PlayerChangeSpeedComponent>(playerEntity, true);
            EntityManager.SetComponentEnabled<PlayerDisableJumpTag>(playerEntity, !isExit);
        }

        public partial struct PlayerChangeSpeedJob : IJobEntity
        {
            public void Execute(DynamicBuffer<AnimatorCommand> animatorCommands, ref PlayerMovementComponent movement,
                ref PlayerChangeSpeedComponent changeSpeed, EnabledRefRW<PlayerChangeSpeedComponent> changeSpeedEnabled)
            {
                movement.speedSlowdown = changeSpeed.speedMultiply;
                animatorCommands.Add(
                    AnimatorCommand.SetFloat(PlayerAnimatorHashes.Speed, changeSpeed.speedMultiply));

                changeSpeedEnabled.ValueRW = false;
            }
        }

        [WithAll(typeof(PlayerRestartComponent))]
        public partial struct RestartMovementStateJob : IJobEntity
        {
            public void Execute(EnabledRefRW<PlayerDisableJumpTag> disableJump)
            {
                disableJump.ValueRW = false;
            }
        }

        [WithDisabled(typeof(PlayerDeathTag), typeof(LockedMoveTag))]
        public partial struct PlayerMovementJob : IJobEntity
        {
            [ReadOnly] public InputComponent input;
            [ReadOnly] public float deltaTime;
            [ReadOnly] public ComponentLookup<PlayerDisableJumpTag> disableJumpLookup;

            public void Execute(Entity entity, PlayerViewComponent view, 
                DynamicBuffer<AnimatorCommand> animatorCommands, ref PlayerMovementComponent movement,
                in PlayerCameraComponent camera)
            {
                var controller = view.characterController;

                //TODO: когда менять буду characterControlller на другой, тогда возможно это уберется
                if (!controller.enabled)
                    return;

                var isGrounded = controller.isGrounded;

                if (!input.jumpHeld)
                    movement.blockJumpUntilRelease = false;

                movement.jumpBufferCounter = input.jumpPressed
                    ? movement.jumpBufferTime
                    : Mathf.Max(0f, movement.jumpBufferCounter - deltaTime);

                movement.coyoteTimeCounter = isGrounded ? movement.coyoteTime : movement.coyoteTimeCounter - deltaTime;

                if (isGrounded && movement.velocityY < 0f)
                    movement.velocityY = -2f;

                var cameraYaw = camera.isStatic && view.staticCameraTransform != null
                    ? view.staticCameraTransform.eulerAngles.y
                    : camera.yaw;
                var direction = Quaternion.Euler(0f, cameraYaw, 0f) * new Vector3(input.move.x, 0f, input.move.y);
                controller.Move(direction * movement.moveSpeed * movement.speedSlowdown * deltaTime);

                if (direction.sqrMagnitude > 0.01f)
                {
                    var targetRotation = Quaternion.LookRotation(direction) * Quaternion.Euler(270f, 90f, 0f);

                    view.render.rotation = Quaternion.Slerp(view.render.rotation, targetRotation, 15f * deltaTime);
                }

                if (movement.jumpBufferCounter > 0f && movement.coyoteTimeCounter > 0f &&
                    !disableJumpLookup.IsComponentEnabled(entity) && !movement.blockJumpUntilRelease)
                {
                    movement.velocityY = Mathf.Sqrt(movement.jumpHeight * -2f * movement.gravity);
                    movement.coyoteTimeCounter = 0f;
                    movement.jumpBufferCounter = 0f;
                    isGrounded = false;
                }

                var gravityMultiplier = !isGrounded && movement.velocityY < 0f
                    ? movement.fallGravityMultiplier
                    : 1f;
                movement.velocityY += movement.gravity * gravityMultiplier * deltaTime;
                controller.Move(Vector3.up * movement.velocityY * deltaTime);

                var isMoving = input.move.sqrMagnitude > 0f;
                animatorCommands.Add(AnimatorCommand.SetBool(PlayerAnimatorHashes.IsJump, !isGrounded));
                animatorCommands.Add(AnimatorCommand.SetInteger(PlayerAnimatorHashes.Move, isMoving ? 1 : 0));
            }
        }

        [WithAll(typeof(PlayerRestartComponent))]
        public partial struct RestartMovementJob : IJobEntity
        {
            public void Execute(DynamicBuffer<AnimatorCommand> animatorCommands, ref PlayerMovementComponent movement)
            {
                movement.velocityY = 0f;
                movement.coyoteTimeCounter = 0f;
                movement.jumpBufferCounter = 0f;
                movement.speedSlowdown = PlayerConstants.DefaultSlowdownMultiplier;
                movement.blockJumpUntilRelease = false;

                animatorCommands.Add(AnimatorCommand.SetFloat(PlayerAnimatorHashes.Speed,
                    PlayerConstants.DefaultSlowdownMultiplier));
                animatorCommands.Add(AnimatorCommand.SetBool(PlayerAnimatorHashes.IsJump, false));
                animatorCommands.Add(AnimatorCommand.SetInteger(PlayerAnimatorHashes.Move, 0));
            }
        }
    }
}
