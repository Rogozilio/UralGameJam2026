using Unity.Collections;
using Unity.Entities;
using UnityEngine;
using UralGameJam.Ecs.Game;

namespace UralGameJam.Ecs.Player
{
    [UpdateInGroup(typeof(PresentationSystemGroup))]
    public sealed partial class PlayerCameraSystem : SystemBase
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
            var yaw = view.cameraTarget != null ? view.cameraTarget.eulerAngles.y : 0f;
            var rawPitch = view.cameraTarget != null ? view.cameraTarget.eulerAngles.x : 0f;

            EntityManager.AddComponentData(player, new PlayerCameraComponent
            {
                yaw = yaw,
                pitch = rawPitch > 180f ? rawPitch - 360f : rawPitch,
                mouseSensitivity = view.mouseSensitivity,
                gamepadSensitivity = view.gamepadSensitivity,
                pitchMin = view.pitchMin,
                pitchMax = view.pitchMax,
                isStatic = view.isStaticCamera
            });
            EntityManager.AddComponent<LockedCameraTag>(player);
            EntityManager.SetComponentEnabled<LockedCameraTag>(player, false);
        }

        protected override void OnStopRunning()
        {
            var player = SystemAPI.GetSingletonEntity<PlayerTag>();
            EntityManager.RemoveComponent<LockedCameraTag>(player);
            EntityManager.RemoveComponent<PlayerCameraComponent>(player);
        }

        protected override void OnUpdate()
        {
            new PlayerCameraJob
            {
                input = SystemAPI.GetSingleton<InputComponent>(),
                deltaTime = SystemAPI.Time.DeltaTime
            }.Run();
            new RestartCameraJob().Run();
        }

        [WithAll(typeof(PlayerRestartComponent))]
        public partial struct RestartCameraJob : IJobEntity
        {
            public void Execute(PlayerViewComponent view, ref PlayerCameraComponent camera)
            {
                camera.isStatic = false;

                if (view.cameraTarget != null)
                {
                    camera.yaw = view.cameraTarget.eulerAngles.y;
                    var rawPitch = view.cameraTarget.eulerAngles.x;
                    camera.pitch = rawPitch > 180f ? rawPitch - 360f : rawPitch;
                }

                if (view.nextCamera != null)
                    view.nextCamera.Priority = -1;
            }
        }

        [WithDisabled(typeof(LockedCameraTag))]
        public partial struct PlayerCameraJob : IJobEntity
        {
            [ReadOnly] public InputComponent input;
            [ReadOnly] public float deltaTime;

            public void Execute(PlayerViewComponent view, ref PlayerCameraComponent camera)
            {
                if (view.cameraTarget == null)
                    return;

                var sensitivity = input.isGamepad
                    ? camera.gamepadSensitivity * input.stickSensitivityMultiplier
                    : camera.mouseSensitivity * input.mouseSensitivityMultiplier;

                camera.yaw += input.look.x * sensitivity * deltaTime;
                camera.pitch -= input.look.y * sensitivity * deltaTime;
                camera.pitch = Mathf.Clamp(camera.pitch, camera.pitchMin, camera.pitchMax);
                view.cameraTarget.localRotation = Quaternion.Euler(
                    camera.pitch,
                    camera.yaw,
                    0f);
            }
        }
    }
}
