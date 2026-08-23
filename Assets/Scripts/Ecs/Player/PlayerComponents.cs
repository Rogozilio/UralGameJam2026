using Scripts;
using TMPro;
using Unity.Cinemachine;
using Unity.Entities;
using UnityEngine;

namespace UralGameJam.Ecs.Player
{
    #region Player

    public static class PlayerAnimatorHashes
    {
        public static readonly int Move = Animator.StringToHash("move");
        public static readonly int IsJump = Animator.StringToHash("isJump");
        public static readonly int IsIdleFire = Animator.StringToHash("isIdleFire");
        public static readonly int IsTutorial = Animator.StringToHash("isTutorial");
        public static readonly int Speed = Animator.StringToHash("SpeedInZhiza");
        public static readonly int IsClimb = Animator.StringToHash("isClimb");
        public static readonly int ClimbState = Animator.StringToHash("Climb");
        public static readonly int DieState = Animator.StringToHash("Die");
        public static readonly int Respawn = Animator.StringToHash("Respawn");
    }

    public struct PlayerTag : IComponentData
    {
    }

    public sealed class PlayerViewComponent : IComponentData
    {
        public GameObject owner;
        public CharacterController characterController;
        public Transform render;
        public Transform cameraTarget;
        public Transform staticCameraTransform;
        public CinemachineCamera nextCamera;
        public FootstepAudio footstepAudio;
        public AudioClip deathSound;
        public float deathSoundVolume;
        public Material disintegrate;
        public PlayerRespawnTextureCycler respawnTextureCycler;
        public float moveSpeed;
        public float jumpHeight;
        public float gravity;
        public float jumpBufferTime;
        public float fallGravityMultiplier;
        public float coyoteTime;
        public float mouseSensitivity;
        public float gamepadSensitivity;
        public float pitchMin;
        public float pitchMax;
        public bool isStaticCamera;
        public float deathDuration;
        public float lifeTimeDuration;
        public TextMeshProUGUI lifeTimeText;
        public Vector3 restartPosition;
        public Quaternion restartRotation;
        public Quaternion restartRenderRotation;
    }

    public struct PlayerStateComponent : IComponentData
    {
        public bool isIdleFire;
    }

    #endregion

    #region PlayerMovementSystem

    public static class PlayerConstants
    {
        public const float DefaultSlowdownMultiplier = 1f;
        public const float SlowdownMultiplier = 0.4f;
    }

    public struct LockedMoveTag : IComponentData, IEnableableComponent
    {
    }

    public struct SlowdownTag : IComponentData
    {
    }

    public struct PlayerMovementComponent : IComponentData
    {
        public float moveSpeed;
        public float jumpHeight;
        public float gravity;
        public float jumpBufferTime;
        public float fallGravityMultiplier;
        public float coyoteTime;
        public float velocityY;
        public float coyoteTimeCounter;
        public float jumpBufferCounter;
        public float speedSlowdown;
        public bool blockJumpUntilRelease;
    }

    public struct PlayerChangeSpeedComponent : IComponentData, IEnableableComponent
    {
        public float speedMultiply;
    }

    public struct PlayerDisableJumpTag : IComponentData, IEnableableComponent
    {
    }

    #endregion

    #region PlayerCameraSystem

    public struct LockedCameraTag : IComponentData, IEnableableComponent
    {
    }

    public struct PlayerCameraComponent : IComponentData
    {
        public float yaw;
        public float pitch;
        public float mouseSensitivity;
        public float gamepadSensitivity;
        public float pitchMin;
        public float pitchMax;
        public bool isStatic;
    }

    #endregion

    #region PlayerClimbSystem

    public struct PlayerClimbRequestTag : IComponentData
    {
    }

    public struct PlayerFinishClimbRequest : IComponentData
    {
    }

    public struct ClimbTag : IComponentData
    {
    }

    public struct ClimbComponent : IComponentData
    {
        public Vector3 startPosition;
        public Vector3 startRight;
        public Vector3 finishPosition;
        public Vector3 finishRight;
        public Quaternion startRotation;
        public Vector3 forward;
        public Vector3 position;
        public float range;
    }

    public struct PlayerClimbTargetComponent : IComponentData
    {
        public Entity target;
    }

    #endregion

    #region PlayerDeathSystem

    public sealed class LifeTimeViewComponent : IComponentData
    {
        public TextMeshProUGUI text;
    }

    public struct LifeTimeComponent : IComponentData, IEnableableComponent
    {
        public float duration;
        public float remainingTime;
        public bool isFastTime;
    }

    public struct LifeTimePausedTag : IComponentData, IEnableableComponent
    {
    }

    public struct PlayerDeathTag : IComponentData, IEnableableComponent
    {
    }

    public struct PlayerDeathStartedTag : IComponentData, IEnableableComponent
    {
    }

    public struct PlayerDeathRequest : IComponentData
    {
    }

    public struct PlayerDeathComponent : IComponentData
    {
        public float elapsed;
        public float dissolveProgress;
    }

    #endregion

    #region PlayerBlendShapeSystem

    [System.Serializable]
    public struct GradientRendererEntry
    {
        public Renderer renderer;
        public int materialIndex;
        [Range(0f, 1f)] public float gradientStart;
        [Range(0f, 1f)] public float gradientEnd;
        [Range(-1f, 1f)] public float offsetStart;
        [Range(-1f, 1f)] public float offsetEnd;
    }

    public sealed class BlendShapeViewComponent : IComponentData
    {
        public ParticleSystem fire;
        public AnimationCurve curve;
        public SkinnedMeshRenderer[] skinnedMeshRenderers;
        public GradientRendererEntry[] gradientRenderers;
        public readonly MaterialPropertyBlock propertyBlock = new();
    }

    public struct BlendShapeComponent : IComponentData
    {
        public float blendValue;
        public bool isFireZero;
    }

    public struct RestartFireRequestTag : IComponentData
    {
    }

    #endregion

    #region PlayerRespawnSystem

    public struct PlayerFinishRespawnRequest : IComponentData
    {
    }

    public struct PlayerRespawnComponent : IComponentData, IEnableableComponent
    {
        public Vector3 position;
        public Quaternion rotation;
        public Quaternion renderRotation;
    }

    public class SpawnBoxComponent : IComponentData
    {
        public Transform target;
    }

    #endregion

    #region PlayerAshSpawnerSystem

    public sealed class AshSpawnerViewComponent : IComponentData
    {
        public GameObject owner;
        public GameObject prefab;
        public Transform spawnPoint;
        public Transform ignoreHierarchyRoot;
    }

    public struct AshSpawnerComponent : IComponentData
    {
        public LayerMask parentSearchLayers;
        public float minSurfaceUpDot;
        public float revealDuration;
        public bool detectParentBelowSpawn;
        public bool revealOnSpawn;
    }

    public sealed class AshRevealViewComponent : IComponentData
    {
        public Transform transform;
    }

    public struct AshRevealComponent : IComponentData
    {
        public Vector3 targetScale;
        public float duration;
        public float elapsed;
    }

    #endregion

    #region AreaRefire

    public struct StartFireTag : IComponentData
    {
    }

    public struct EndFireTag : IComponentData
    {
    }

    public struct AreaRefireTag : IComponentData
    {
    }
    
    public struct AreaRefireComponent : IComponentData
    {
        
    }

    #endregion
}
