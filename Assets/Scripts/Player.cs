using Scripts;
using TMPro;
using Unity.Cinemachine;
using Unity.Entities;
using UnityEngine;
using UralGameJam.Ecs.Animation;
using UralGameJam.Ecs.Player;
using UralGameJam.Ecs.Restart;

[RequireComponent(typeof(AnimatorEntity))]
public class Player : PhysicsMonoEntity, IRestart
{
    public Transform render;
    public CharacterController characterController;
    public FootstepAudio footstepAudio;

    public float moveSpeed = 5f;
    public float jumpHeight = 1.5f;
    public float gravity = -20f;
    public float jumpBufferTime = 0.12f;
    public float fallGravityMultiplier = 1.8f;
    public float coyoteTime = 0.15f;

    public Transform cameraTarget;
    public float mouseSensitivity = 0.15f;
    public float gamepadSensitivity = 150f;
    public float pitchMin = -80f;
    public float pitchMax = 80f;
    public bool isStaticCamera;
    public Transform staticCameraTransform;
    public CinemachineCamera nextCamera;

    public AudioClip deathSound;
    [Range(0f, 1f)] public float deathSoundVolume = 1f;
    [Min(0f)] public float deathDuration = 2f;
    public Material disintegrate;
    [Min(0f)] public float lifeTimeDuration = 30f;
    public TextMeshProUGUI lifeTimeText;
    public ParticleSystem fire;
    public AnimationCurve fireSizeCurve;
    public SkinnedMeshRenderer[] skinnedMeshRenderers;
    [Range(0f, 1f)] public float blendValue;
    public bool isFireZero;
    public GradientRendererEntry[] gradientRenderers;

    [SerializeField]
    private bool isIdleFire;

    public bool IsTutorial
    {
        set
        {
            isIdleFire = !value;

            if (TryGetAnimatorCommands(out var commands))
            {
                commands.Add(AnimatorCommand.SetBool(PlayerAnimatorHashes.IsTutorial, value));
                commands.Add(AnimatorCommand.SetBool(PlayerAnimatorHashes.IsIdleFire, isIdleFire));
            }

            if (TryGetState(out var state))
            {
                state.isIdleFire = isIdleFire;
                _entityManager.SetComponentData(_entity, state);
            }
        }
    }

    public bool IsActive
    {
        get => !IsComponentEnabled<LockedMoveTag>() && !IsComponentEnabled<LockedCameraTag>();
        set
        {
            SetComponentEnabled<LockedMoveTag>(!value);
            SetComponentEnabled<LockedCameraTag>(!value);
        }
    }

    public bool IsStaticCamera
    {
        set
        {
            isStaticCamera = value;

            if (_entityManager.HasComponent<PlayerViewComponent>(_entity))
                _entityManager.GetComponentObject<PlayerViewComponent>(_entity).isStaticCamera = value;

            if (_entityManager.HasComponent<PlayerCameraComponent>(_entity))
            {
                var camera = _entityManager.GetComponentData<PlayerCameraComponent>(_entity);
                camera.isStatic = value;
                _entityManager.SetComponentData(_entity, camera);
            }

            if (nextCamera != null)
                nextCamera.Priority = value ? 1 : -1;
        }
    }

    protected override void Awake()
    {
        ValidateReferences();
        base.Awake();

        if (footstepAudio == null)
            footstepAudio = GetComponentInChildren<FootstepAudio>();

        _entityManager.AddComponent<PlayerTag>(_entity);
        _entityManager.AddComponentObject(_entity, new PlayerViewComponent
        {
            owner = gameObject,
            characterController = characterController,
            render = render,
            cameraTarget = cameraTarget,
            staticCameraTransform = staticCameraTransform,
            nextCamera = nextCamera,
            footstepAudio = footstepAudio,
            deathSound = deathSound,
            deathSoundVolume = deathSoundVolume,
            disintegrate = disintegrate,
            respawnTextureCycler = GetComponent<PlayerRespawnTextureCycler>(),
            moveSpeed = moveSpeed,
            jumpHeight = jumpHeight,
            gravity = gravity,
            jumpBufferTime = jumpBufferTime,
            fallGravityMultiplier = fallGravityMultiplier,
            coyoteTime = coyoteTime,
            mouseSensitivity = mouseSensitivity,
            gamepadSensitivity = gamepadSensitivity,
            pitchMin = pitchMin,
            pitchMax = pitchMax,
            isStaticCamera = isStaticCamera,
            deathDuration = deathDuration,
            lifeTimeDuration = lifeTimeDuration,
            lifeTimeText = lifeTimeText,
            restartPosition = transform.position,
            restartRotation = transform.rotation,
            restartRenderRotation = render.localRotation
        });
        _entityManager.AddComponentData(_entity, new PlayerStateComponent
        {
            isIdleFire = isIdleFire
        });
        _entityManager.AddComponentObject(_entity, new BlendShapeViewComponent
        {
            fire = fire,
            curve = fireSizeCurve,
            skinnedMeshRenderers = skinnedMeshRenderers,
            gradientRenderers = gradientRenderers
        });
        _entityManager.AddComponentData(_entity, new BlendShapeComponent
        {
            blendValue = blendValue,
            isFireZero = isFireZero
        });

        IsStaticCamera = isStaticCamera;
    }

    private void ValidateReferences()
    {
        if (render == null)
            throw new MissingReferenceException($"{nameof(Player)}.{nameof(render)} is not assigned on {name}");

        if (characterController == null)
            throw new MissingReferenceException(
                $"{nameof(Player)}.{nameof(characterController)} is not assigned on {name}");

        if (lifeTimeText == null)
            throw new MissingReferenceException($"{nameof(Player)}.{nameof(lifeTimeText)} is not assigned on {name}");

        if (fire == null)
            throw new MissingReferenceException($"{nameof(Player)}.{nameof(fire)} is not assigned on {name}");
    }

    private void OnDestroy()
    {
        RemoveComponent<BlendShapeComponent>();
        RemoveComponent<BlendShapeViewComponent>();
        RemoveComponent<PlayerStateComponent>();
        RemoveComponent<PlayerViewComponent>();
        RemoveComponent<PlayerTag>();
    }

    public void Restart()
    {
        EnableComponent<PlayerRespawnComponent>();
    }

    public void FinishClimb()
    {
        AddComponent<PlayerFinishClimbRequest>();
    }

    public void FinishRespawn()
    {
        AddComponent<PlayerFinishRespawnRequest>();
    }

    public void PauseLifeTimer()
    {
        SetComponentEnabled<LifeTimePausedTag>(true);
    }

    public void ResumeLifeTimer()
    {
        SetComponentEnabled<LifeTimePausedTag>(false);
    }

    public void RestartLifeTimer()
    {
        var lifeTime = _entityManager.GetComponentData<LifeTimeComponent>(_entity);
        lifeTime.remainingTime = lifeTime.duration;
        lifeTime.isFastTime = false;

        _entityManager.SetComponentData(_entity, lifeTime);
        _entityManager.SetComponentEnabled<LifeTimeComponent>(_entity, true);
        _entityManager.SetComponentEnabled<LifeTimePausedTag>(_entity, false);
        _entityManager.AddComponent<RestartFireRequestTag>(_entity);
    }

    private bool TryGetState(out PlayerStateComponent state)
    {
        if (_entityManager.HasComponent<PlayerStateComponent>(_entity))
        {
            state = _entityManager.GetComponentData<PlayerStateComponent>(_entity);
            return true;
        }

        state = default;
        return false;
    }

    private bool IsComponentEnabled<T>() where T : unmanaged, IComponentData, IEnableableComponent
    {
        return _entityManager.HasComponent<T>(_entity) && _entityManager.IsComponentEnabled<T>(_entity);
    }

    private void SetComponentEnabled<T>(bool value) where T : unmanaged, IComponentData, IEnableableComponent
    {
        if (_entityManager.HasComponent<T>(_entity))
            _entityManager.SetComponentEnabled<T>(_entity, value);
    }

    private bool TryGetAnimatorCommands(out DynamicBuffer<AnimatorCommand> commands)
    {
        if (_entityManager != default && _entityManager.Exists(_entity) &&
            _entityManager.HasComponent<AnimatorCommand>(_entity))
        {
            commands = _entityManager.GetBuffer<AnimatorCommand>(_entity);
            return true;
        }

        commands = default;
        return false;
    }

}
