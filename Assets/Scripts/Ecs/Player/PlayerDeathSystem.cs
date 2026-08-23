using Scripts;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using UralGameJam.Ecs.Animation;
using UralGameJam.Ecs.Physics3D;

namespace UralGameJam.Ecs.Player
{
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(PlayerMovementSystem))]
    [UpdateBefore(typeof(PlayerBlendShapeSystem))]
    public sealed partial class PlayerDeathSystem : SystemBase
    {
        protected override void OnCreate()
        {
            RequireForUpdate<PlayerTag>();
        }

        protected override void OnStartRunning()
        {
            var player = SystemAPI.GetSingletonEntity<PlayerTag>();
            var view = EntityManager.GetComponentObject<PlayerViewComponent>(player);

            EntityManager.AddComponentObject(player, new LifeTimeViewComponent
            {
                text = view.lifeTimeText
            });
            EntityManager.AddComponentData(player, new LifeTimeComponent
            {
                duration = view.lifeTimeDuration,
                remainingTime = view.lifeTimeDuration,
                isFastTime = false
            });
            EntityManager.AddComponent<LifeTimePausedTag>(player);
            EntityManager.SetComponentEnabled<LifeTimePausedTag>(player, false);
            EntityManager.AddComponent<RestartFireRequestTag>(player);
            EntityManager.AddComponentData(player, new PlayerDeathComponent());
            EntityManager.AddComponent<PlayerDeathTag>(player);
            EntityManager.AddComponent<PlayerDeathStartedTag>(player);
            EntityManager.SetComponentEnabled<PlayerDeathTag>(player, false);
            EntityManager.SetComponentEnabled<PlayerDeathStartedTag>(player, false);
        }

        protected override void OnStopRunning()
        {
            var player = SystemAPI.GetSingletonEntity<PlayerTag>();

            EntityManager.RemoveComponent<RestartFireRequestTag>(player);
            EntityManager.RemoveComponent<LifeTimePausedTag>(player);
            EntityManager.RemoveComponent<LifeTimeComponent>(player);
            EntityManager.RemoveComponent<LifeTimeViewComponent>(player);
            EntityManager.RemoveComponent<PlayerDeathRequest>(player);
            EntityManager.RemoveComponent<PlayerDeathStartedTag>(player);
            EntityManager.RemoveComponent<PlayerDeathTag>(player);
            EntityManager.RemoveComponent<PlayerDeathComponent>(player);
        }

        protected override void OnUpdate()
        {
            if (SystemAPI.TryGetSingletonBuffer<PhysicsEventTriggerComponent>(out var triggerEvents, true))
                HandleKillBoxTriggers(triggerEvents);

            var ecbSingleton = SystemAPI.GetSingleton<BeginPresentationEntityCommandBufferSystem.Singleton>();
            var ecb = ecbSingleton.CreateCommandBuffer(World.Unmanaged);

            //LifeTime
            new LifeTimeJob
            {
                fixedDeltaTime = SystemAPI.Time.fixedDeltaTime,
                fastTimeMultiplier = 40f
            }.Run();
            new RestartLifeTimeJob
            {
                ecb = ecb
            }.Run();
            new EndLifeTimeStartDeathJob
            {
                ecb = ecb
            }.Run();
            new LifeTimeViewJob().Run();

            //Death
            new StartDeathWhenGroundedJob
            {
                ecb = ecb
            }.Run();
            new DeathAccelerateLifeTimeJob().Run();
            new DeathProgressJob
            {
                deltaTime = SystemAPI.Time.DeltaTime
            }.Run();
            new StopDeathFireJob().Run();
        }

        private void HandleKillBoxTriggers(DynamicBuffer<PhysicsEventTriggerComponent> triggerEvents)
        {
            var playerEntity = Entity.Null;

            for (var i = 0; i < triggerEvents.Length; i++)
            {
                var triggerEvent = triggerEvents[i];

                if (triggerEvent.isExit)
                    continue;

                if (PhysicsUtility.TryGetPair<KillBoxTag, PlayerTag>(
                        EntityManager, triggerEvent.entityA, triggerEvent.entityB,
                        out _, out var currentPlayerEntity))
                {
                    playerEntity = currentPlayerEntity;
                }
            }

            if (playerEntity != Entity.Null)
                EntityManager.AddComponent<PlayerDeathRequest>(playerEntity);
        }

        [WithDisabled(typeof(LifeTimeComponent), typeof(PlayerDeathTag))]
        public partial struct EndLifeTimeStartDeathJob : IJobEntity
        {
            public EntityCommandBuffer ecb;

            public void Execute(Entity entity)
            {
                ecb.AddComponent<PlayerDeathRequest>(entity);
            }
        }

        [WithOptions(EntityQueryOptions.IgnoreComponentEnabledState)]
        public partial struct RestartLifeTimeJob : IJobEntity
        {
            public EntityCommandBuffer ecb;

            public void Execute(Entity entity, EnabledRefRO<PlayerRespawnComponent> restartEnabled,
                ref LifeTimeComponent lifeTime, EnabledRefRW<LifeTimeComponent> lifeTimeEnabled,
                EnabledRefRW<LifeTimePausedTag> lifeTimePaused)
            {
                if (!restartEnabled.ValueRO)
                    return;

                lifeTime.remainingTime = lifeTime.duration;
                lifeTime.isFastTime = false;
                lifeTimeEnabled.ValueRW = true;
                lifeTimePaused.ValueRW = false;

                ecb.AddComponent<RestartFireRequestTag>(entity);
            }
        }

        [WithNone(typeof(LifeTimePausedTag))]
        public partial struct LifeTimeJob : IJobEntity
        {
            [ReadOnly] public float fixedDeltaTime;
            [ReadOnly] public float fastTimeMultiplier;

            public void Execute(ref LifeTimeComponent lifeTime, EnabledRefRW<LifeTimeComponent> enabled)
            {
                if (lifeTime.remainingTime <= 0f)
                    enabled.ValueRW = false;

                var multiplier = lifeTime.isFastTime ? fastTimeMultiplier : 1f;
                lifeTime.remainingTime = math.clamp(
                    lifeTime.remainingTime - fixedDeltaTime * multiplier, 0f, lifeTime.duration);
            }
        }

        public partial struct LifeTimeViewJob : IJobEntity
        {
            public void Execute(LifeTimeViewComponent view, in LifeTimeComponent lifeTime)
            {
                view.text.text = lifeTime.remainingTime <= 0f ? "0" : lifeTime.remainingTime.ToString("00");
            }
        }

        [WithAll(typeof(PlayerDeathRequest))]
        [WithDisabled(typeof(PlayerDeathTag), typeof(PlayerDeathStartedTag))]
        public partial struct StartDeathWhenGroundedJob : IJobEntity
        {
            public EntityCommandBuffer ecb;

            public void Execute(Entity entity, PlayerViewComponent view,
                DynamicBuffer<AnimatorCommand> animatorCommands, ref PlayerDeathComponent death,
                EnabledRefRW<PlayerDeathTag> deathEnabled, EnabledRefRW<PlayerDeathStartedTag> deathStarted)
            {
                if (!view.characterController.isGrounded)
                    return;

                death.elapsed = 0f;
                death.dissolveProgress = 0f;

                //TODO: все что связанно с аудио надо будет переделать
                view.footstepAudio?.ResetSurfaceTypeToDefault();
                //TODO: все что связанно с аудио надо будет переделать
                if (view.deathSound != null)
                    AudioSource.PlayClipAtPoint(view.deathSound, view.owner.transform.position, view.deathSoundVolume);

                animatorCommands.Add(AnimatorCommand.SetInteger(PlayerAnimatorHashes.Move, 0));
                animatorCommands.Add(AnimatorCommand.CrossFade(PlayerAnimatorHashes.DieState, 0.3f));
                deathEnabled.ValueRW = true;
                deathStarted.ValueRW = true;
                ecb.SetComponentEnabled<PlayerDeathStartedTag>(entity, false);
                ecb.RemoveComponent<PlayerDeathRequest>(entity);
            }
        }

        [WithAll(typeof(PlayerDeathTag), typeof(PlayerDeathStartedTag))]
        public partial struct StopDeathFireJob : IJobEntity
        {
            public void Execute(BlendShapeViewComponent view)
            {
                view.fire.Stop();
                view.fire.Clear();
            }
        }

        [WithAll(typeof(PlayerDeathTag))]
        public partial struct DeathAccelerateLifeTimeJob : IJobEntity
        {
            public void Execute(ref LifeTimeComponent lifeTime)
            {
                lifeTime.isFastTime = true;
            }
        }

        [WithAll(typeof(PlayerDeathTag))]
        [WithDisabled(typeof(PlayerRespawnComponent))]
        public partial struct DeathProgressJob : IJobEntity
        {
            [ReadOnly] public float deltaTime;

            public void Execute(PlayerViewComponent view, ref PlayerDeathComponent death,
                ref BlendShapeComponent blendShape, EnabledRefRW<PlayerDeathTag> deathEnabled,
                EnabledRefRW<PlayerRespawnComponent> restartEnabled)
            {
                death.elapsed += deltaTime;
                ApplyDissolve(view, ref death, blendShape);
                blendShape.isFireZero = death.dissolveProgress > 0f;

                if (death.elapsed < view.deathDuration)
                    return;

                var material = GetDisintegrateMaterial(view);

                if (material != null)
                    material.SetFloat("_DissolveProgress", 0f);

                death.elapsed = 0f;
                death.dissolveProgress = 0f;

                view.respawnTextureCycler?.AdvanceTexture();
                restartEnabled.ValueRW = true;
                deathEnabled.ValueRW = false;
            }

            private void ApplyDissolve(PlayerViewComponent view, ref PlayerDeathComponent death,
                in BlendShapeComponent blendShape)
            {
                if (blendShape.blendValue < 0.98f)
                    return;

                var material = GetDisintegrateMaterial(view);

                if (material == null)
                    return;

                death.dissolveProgress = Mathf.Clamp01(death.dissolveProgress + deltaTime * 0.5f);
                material.SetFloat("_DissolveProgress", death.dissolveProgress);
            }

            private static Material GetDisintegrateMaterial(PlayerViewComponent view)
            {
                return view.respawnTextureCycler?.TargetMaterial ?? view.disintegrate;
            }
        }
    }
}
