using Scripts;
using Unity.Cinemachine;
using Unity.Entities;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Serialization;
using UralGameJam.Ecs.Game;
using UralGameJam.Ecs.BlendShape;
using UralGameJam.Ecs.LifeTime;
using UralGameJam.Ecs.Physics3D;
using UralGameJam.Ecs.Player;
using UralGameJam.Ecs.Restart;

public class Player : MonoEntity, IRestart
{
    public const float DefaultSlowdownMultiplier = 1f;
    public const float SlowdownMultiplier = 0.4f;
    public const float DefaultSpeedInZhiza = 1f;
    public const float SlowdownSpeedInZhiza = 0.6f;

    //[Inject] private UIMenu _uiMenu;

    [Header("References")]
    public Animator animator;
    public Transform render;
    public FootstepAudio footstepAudio;
    public AudioClip deathSound;
    [Range(0f, 1f)] public float deathSoundVolume = 1f;
    public bool isActive = true;

    public bool IsTutorial
    {
        set
        {
            animator.SetBool("isTutorial", value);
            isIdleFire = !value;
        }
    }

    public bool IsActive
    {
        get => isActive;
        set
        {
            isActive = value;
        }
    }

    public CharacterController characterController;
    public float moveSpeed = 5f;
    public float jumpHeight = 1.5f;
    public float gravity = -20f;

    [Header("Jump Feel")]
    public float jumpBufferTime = 0.12f;
    public float fallGravityMultiplier = 1.8f;
    public float jumpCutGravityMultiplier = 2.3f;

    private float _velocityY;
    
    [Header("Camera")]
    public Transform cameraTarget;
    public float mouseSensitivity = 0.15f;
    public float gamepadSensitivity = 150f;
    public float pitchMin = -80f;
    public float pitchMax = 80f;
    
    [Header("Camera Modes")]
    public bool isStaticCamera = false;
    public Transform staticCameraTransform;
    public CinemachineCamera nextCamera;

    public bool IsStaticCamera
    {
        set
        {
            isStaticCamera = value;
            nextCamera.Priority = value ? 1 : -1;
        }
    }

    [Header("Coyote Time")]
    public float coyoteTime = 0.15f;
    private float _coyoteTimeCounter;
    private float _jumpBufferCounter;
    
    [HideInInspector] public bool isOnPlatform;
    private float _pitch;
    private float _yaw;
    private bool _blockJumpUntilRelease;
    private PlayerRespawnTextureCycler _respawnTextureCycler;
    private EntityManager _entityManager;
    private bool _playerEcsInitialized;

    [SerializeField]
    private bool isIdleFire;

    public Transform tempPointMove;

    [Header("Death")]
    public UnityEvent onStartDeath;
    public UnityEvent onEndDeath;
    [Min(0f)] public float deathDuration = 2f;
    [FormerlySerializedAs("isDeath")]
    [SerializeField]
    private bool _isDeath;

    public bool isDeath
    {
        get
        {
            if (!_playerEcsInitialized)
                return _isDeath;

            return _entityManager.GetComponentData<PlayerDeathData>(_entity).IsDeath;
        }
        set
        {
            _isDeath = value;

            if (!_playerEcsInitialized)
                return;

            var death = _entityManager.GetComponentData<PlayerDeathData>(_entity);
            death.IsDeath = value;
            _entityManager.SetComponentData(_entity, death);
        }
    }
    public Material disintegrate;

    public bool isMove => ReadInput().Move.magnitude > 0f;

    public bool SetIsPushAnim
    {
        set => animator.SetBool("isPush", value);
        get => animator.GetBool("isPush");
    }

    public bool IsAnimationPlaying
    {
        get => _isAnimation;
        set => _isAnimation = value;
    }

    public ClimbData CurrentClimbData { get; set; }

    private void InitializePlayerEcs()
    {
        if (_playerEcsInitialized && _entityManager.Exists(_entity))
            return;

        if (!_entityManager.HasComponent<PlayerTag>(_entity))
            _entityManager.AddComponent<PlayerTag>(_entity);

        if (!_entityManager.HasComponent<PlayerViewData>(_entity))
        {
            _entityManager.AddComponentObject(_entity, new PlayerViewData
            {
                Owner = gameObject,
                View = this,
                CharacterController = characterController,
                Animator = animator,
                Render = render,
                TempPointMove = tempPointMove,
                CameraTarget = cameraTarget
            });
        }

        if (!_entityManager.HasComponent<PlayerMovementData>(_entity))
            _entityManager.AddComponentData(_entity, CreateMovementData());

        if (!_entityManager.HasComponent<PlayerDeathData>(_entity))
            _entityManager.AddComponentData(_entity, new PlayerDeathData { IsDeath = _isDeath });

        if (!_entityManager.HasComponent<PlayerCameraData>(_entity))
            _entityManager.AddComponentData(_entity, CreateCameraData());

        if (!_entityManager.HasComponent<PlayerTriggerStateData>(_entity))
            _entityManager.AddComponentData(_entity, new PlayerTriggerStateData());

        _playerEcsInitialized = true;
    }

    private PlayerMovementData CreateMovementData()
    {
        return new PlayerMovementData
        {
            MoveSpeed = moveSpeed,
            JumpHeight = jumpHeight,
            Gravity = gravity,
            JumpBufferTime = jumpBufferTime,
            FallGravityMultiplier = fallGravityMultiplier,
            CoyoteTime = coyoteTime,
            VelocityY = _velocityY,
            CoyoteTimeCounter = _coyoteTimeCounter,
            JumpBufferCounter = _jumpBufferCounter,
            SpeedSlowdown = DefaultSlowdownMultiplier,
            DisableJump = false,
            BlockJumpUntilRelease = _blockJumpUntilRelease
        };
    }

    private PlayerCameraData CreateCameraData()
    {
        return new PlayerCameraData
        {
            Yaw = _yaw,
            Pitch = _pitch,
            MouseSensitivity = mouseSensitivity,
            GamepadSensitivity = gamepadSensitivity,
            PitchMin = pitchMin,
            PitchMax = pitchMax
        };
    }

    private void DestroyPlayerEcs()
    {
        if (_playerEcsInitialized && _entityManager.Exists(_entity))
        {
            if (_entityManager.HasComponent<PlayerTag>(_entity))
                _entityManager.RemoveComponent<PlayerTag>(_entity);

            if (_entityManager.HasComponent<PlayerDeathTag>(_entity))
                _entityManager.RemoveComponent<PlayerDeathTag>(_entity);

            if (_entityManager.HasComponent<PlayerClimbStartTag>(_entity))
                _entityManager.RemoveComponent<PlayerClimbStartTag>(_entity);

            if (_entityManager.HasComponent<PlayerSlowdownEnterTag>(_entity))
                _entityManager.RemoveComponent<PlayerSlowdownEnterTag>(_entity);

            if (_entityManager.HasComponent<PlayerSlowdownExitTag>(_entity))
                _entityManager.RemoveComponent<PlayerSlowdownExitTag>(_entity);

            if (_entityManager.HasComponent<PlayerViewData>(_entity))
                _entityManager.RemoveComponent<PlayerViewData>(_entity);

            if (_entityManager.HasComponent<PlayerMovementData>(_entity))
                _entityManager.RemoveComponent<PlayerMovementData>(_entity);

            if (_entityManager.HasComponent<PlayerDeathData>(_entity))
                _entityManager.RemoveComponent<PlayerDeathData>(_entity);

            if (_entityManager.HasComponent<PlayerCameraData>(_entity))
                _entityManager.RemoveComponent<PlayerCameraData>(_entity);

            if (_entityManager.HasComponent<PlayerTriggerStateData>(_entity))
                _entityManager.RemoveComponent<PlayerTriggerStateData>(_entity);
        }

        _entity = Entity.Null;
        _playerEcsInitialized = false;
    }

    private void ResetPlayerEcsMovementState(bool blockJumpUntilRelease = false)
    {
        _velocityY = 0f;
        _coyoteTimeCounter = 0f;
        _jumpBufferCounter = 0f;
        _blockJumpUntilRelease = blockJumpUntilRelease;

        if (!_playerEcsInitialized)
            return;

        var movement = _entityManager.GetComponentData<PlayerMovementData>(_entity);
        movement.VelocityY = 0f;
        movement.CoyoteTimeCounter = 0f;
        movement.JumpBufferCounter = 0f;
        movement.BlockJumpUntilRelease = blockJumpUntilRelease;
        _entityManager.SetComponentData(_entity, movement);
    }

    private void SetPlayerJustResumed()
    {
        if (!_playerEcsInitialized)
            return;

        var movement = _entityManager.GetComponentData<PlayerMovementData>(_entity);
        movement.JustResumed = true;
        _entityManager.SetComponentData(_entity, movement);
    }

    protected override void Awake()
    {
        base.Awake();

        _yaw = cameraTarget.eulerAngles.y;
        float rawPitch = cameraTarget.eulerAngles.x;
        _pitch = rawPitch > 180f ? rawPitch - 360f : rawPitch;
        
        // Cursor.lockState = CursorLockMode.Locked;
        // Cursor.visible = false;

        animator.applyRootMotion = false;
        ResetOriginPositionAndRotation();
        _respawnTextureCycler = GetComponent<PlayerRespawnTextureCycler>();
        InitializePlayerEcs();
        EnsureTriggerBridge();
        SetSlowdownState(false);

        if (footstepAudio == null)
            footstepAudio = GetComponentInChildren<FootstepAudio>();
        
        //_uiMenu.OnResumed += HandleResumed;
        
    }
    
    private void OnDestroy()
    {
        //_uiMenu.OnResumed -= HandleResumed;

        DestroyPlayerEcs();
    }

    public void BeginDeathSequence()
    {
        footstepAudio?.ResetSurfaceTypeToDefault();
        if (deathSound != null)
            AudioSource.PlayClipAtPoint(deathSound, transform.position, deathSoundVolume);

        animator.CrossFade("Die", 0.3f);
        onStartDeath?.Invoke();
    }

    public void CompleteDeathSequence()
    {
        isDeath = false;
        GetComponent<PlayerRespawnTextureCycler>()?.AdvanceTexture();
        onEndDeath?.Invoke();
    }
    
    private void HandleResumed() => SetPlayerJustResumed();

    private void Update()
    {
        SyncPlayerViewData();

        if (Time.timeScale == 0f || !isActive)
        { 
            animator.SetInteger("move", 0);
            return;
        }

        animator.SetBool("isIdleFire", isIdleFire);
    }

    #region Restart

    private Vector3 _originPosition;
    private Quaternion _originRotation;
    private Quaternion _originRenderRotation;

    private void ResetOriginPositionAndRotation()
    {
        _originPosition = transform.position;
        _originRotation = transform.rotation;
        _originRenderRotation = render.localRotation;
    }

    public void Death(bool isDeathNow = false)
    {
        if ((!isDeath && !isDeathNow) ||
            !_playerEcsInitialized ||
            _entityManager.HasComponent<PlayerDeathTag>(_entity))
            return;

        isDeath = true;
        _entityManager.AddComponent<PlayerDeathTag>(_entity);
    }

    public void RestartNow()
    {
        isDeath = true;
    }

    public void Restart()
    {
        if (!_playerEcsInitialized || characterController == null)
            return;

        characterController.enabled = false;
        transform.SetPositionAndRotation(_originPosition, _originRotation);

        if (render != null)
            render.localRotation = _originRenderRotation;

        ResetPlayerEcsMovementState();
        var movement = _entityManager.GetComponentData<PlayerMovementData>(_entity);
        movement.SpeedSlowdown = DefaultSlowdownMultiplier;
        movement.DisableJump = false;
        movement.JustResumed = false;
        movement.IsOnPlatform = false;
        _entityManager.SetComponentData(_entity, movement);

        if (cameraTarget != null)
        {
            var camera = _entityManager.GetComponentData<PlayerCameraData>(_entity);
            camera.Yaw = cameraTarget.eulerAngles.y;
            var rawPitch = cameraTarget.eulerAngles.x;
            camera.Pitch = rawPitch > 180f ? rawPitch - 360f : rawPitch;
            _entityManager.SetComponentData(_entity, camera);
        }

        var triggerState = _entityManager.GetComponentData<PlayerTriggerStateData>(_entity);
        triggerState.ClimbEntity = Entity.Null;
        triggerState.SlowdownContacts = 0;
        _entityManager.SetComponentData(_entity, triggerState);

        RestartLifeTime();
        isStaticCamera = false;

        if (nextCamera != null)
            nextCamera.Priority = -1;

        if (animator != null)
        {
            animator.SetFloat("SpeedInZhiza", DefaultSpeedInZhiza);
            animator.SetBool("isJump", false);
            animator.SetInteger("move", 0);
        }
    }

    #endregion

    #region Animations

    private bool _isAnimation;
    
    private void OnAnimatorMove()
    {
        if (!_isAnimation) return;
        
        animator.ApplyBuiltinRootMotion();
    }

    public void FinishRespawn()
    {
        characterController.enabled = true;
        transform.rotation *= Quaternion.Euler(0f, 180f, 0f);
    }

    public void FinishClimb()
    {
        var climbData = CurrentClimbData;
        CurrentClimbData = null;

        if (climbData == null)
            return;

        IsAnimationPlaying = false;
        transform.position = climbData.GetPointFinishClimb(transform);
        characterController.enabled = true;
        ResetPlayerEcsMovementState(ReadInput().JumpHeld);
        var movement = _entityManager.GetComponentData<PlayerMovementData>(_entity);
        movement.ResumeLifeTimeRequested = true;
        _entityManager.SetComponentData(_entity, movement);
        animator.SetTrigger("isClimb");
    }

    #endregion

    #region Collisions&Triggers

    public void SetSlowdownState(bool isSlowdown)
    {
        animator.SetFloat("SpeedInZhiza", isSlowdown ? SlowdownSpeedInZhiza : DefaultSpeedInZhiza);

        if (!_playerEcsInitialized)
            return;

        var movement = _entityManager.GetComponentData<PlayerMovementData>(_entity);
        movement.SpeedSlowdown = isSlowdown ? SlowdownMultiplier : DefaultSlowdownMultiplier;
        movement.DisableJump = isSlowdown;
        _entityManager.SetComponentData(_entity, movement);
    }

    private Vector3 GetRespawnPosition() => _originPosition;

    private Quaternion GetRespawnRotation() => _originRotation;

    private void SyncPlayerViewData()
    {
        var view = _entityManager.GetComponentObject<PlayerViewData>(_entity);
        view.Owner = gameObject;
        view.CharacterController = characterController;
        view.Animator = animator;
        view.Render = render;
        view.TempPointMove = tempPointMove;
        view.CameraTarget = cameraTarget;
    }

    private void RestartLifeTime()
    {
        if (_entityManager.HasComponent<LifeTimeComponent>(_entity))
        {
            var lifeTime = _entityManager.GetComponentData<LifeTimeComponent>(_entity);
            lifeTime.RemainingTime = lifeTime.Duration;
            lifeTime.IsFastTime = false;
            _entityManager.SetComponentData(_entity, lifeTime);
            _entityManager.SetComponentEnabled<LifeTimeComponent>(_entity, true);

            if (_entityManager.HasComponent<LifeTimePausedTag>(_entity))
                _entityManager.RemoveComponent<LifeTimePausedTag>(_entity);

            if (!_entityManager.HasComponent<RestartFireRequestTag>(_entity))
                _entityManager.AddComponent<RestartFireRequestTag>(_entity);
        }

        if (_entityManager.HasComponent<BlendShapeData>(_entity))
        {
            var blendShape = _entityManager.GetComponentData<BlendShapeData>(_entity);
            blendShape.IsFireZero = false;
            _entityManager.SetComponentData(_entity, blendShape);
        }
    }

    public Material GetDisintegrateMaterial()
    {
        return _respawnTextureCycler?.TargetMaterial ?? disintegrate;
    }

    private InputComponent ReadInput()
    {
        using var query = _entityManager.CreateEntityQuery(ComponentType.ReadOnly<InputComponent>());
        return query.TryGetSingletonEntity<InputComponent>(out var entity)
            ? _entityManager.GetComponentData<InputComponent>(entity)
            : new InputComponent
            {
                MouseSensitivityMultiplier = 1f,
                StickSensitivityMultiplier = 1f
            };
    }

    private void EnsureTriggerBridge()
    {
        var provider = GetComponent<ColliderAndTriggerDOTSProvider>();

        if (provider == null)
            provider = gameObject.AddComponent<ColliderAndTriggerDOTSProvider>();

        provider.entityA = _entity;
    }

    #endregion
}
