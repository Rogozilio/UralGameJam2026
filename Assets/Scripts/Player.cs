using System;
using System.Collections;
using Scripts;
using Unity.Cinemachine;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Events;
using Zenject;
using Input = Scripts.Input;

public class Player : MonoBehaviour, IRestart
{
    [Inject] private Input _input;
    [Inject] private UIMenu _uiMenu;
    
    [Header("References")]
    public LifeTime lifeTime;
    public Animator animator;
    public Transform render;
    public AshSpawner ashSpawner;
    public Transform respawn1;
    public Transform respawn2;
    public bool isTutorial;

    public bool IsTutorial
    {
        set
        {
            isTutorial = value;
            animator.SetBool("isTutorial", value);
            isIdleFire = !value;
            if(!isTutorial)
                lifeTime.StartLifeTimer();
        }
    }

    [Header("CharacterController")]
    public CharacterController characterController;
    public float moveSpeed = 5f;
    public float jumpHeight = 1.5f;
    public float gravity = -20f;
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
    private bool _wasGrounded;
    
    [HideInInspector] public bool isOnPlatform; 
    private float _pitch;
    private float _yaw;
    private float _speedSlowdown = 1f;
    private bool _disableJump;

    [SerializeField]
    private bool isIdleFire;

    public Transform tempPointMove;

    [Header("Death")] 
    public UnityEvent onStartDeath;
    public UnityEvent onEndDeath;
    public bool isDeath;
    public Material disintegrate;
    
    public bool isMove => _input.playerMove.magnitude > 0;

    public bool SetIsPushAnim
    {
        set => animator.SetBool("isPush", value);
        get => animator.GetBool("isPush");
    }

    void Awake()
    {
        _yaw   = cameraTarget.eulerAngles.y;
        float rawPitch = cameraTarget.eulerAngles.x;
        _pitch = rawPitch > 180f ? rawPitch - 360f : rawPitch;
        
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible   = false;

        animator.applyRootMotion = false;
        
        ResetOriginPositionAndRotation();
        lifeTime.OnLifeTimeEnded += RestartNow;
        if(!isTutorial)
            lifeTime.StartLifeTimer();
        
        _uiMenu.OnResumed += HandleResumed;
        
        onStartDeath.AddListener(OnStartDie);

        onEndDeath.AddListener(OnEndDie);
        
        transform.position = isTutorial ? respawn1.position : respawn2.position;
        transform.rotation = isTutorial ? respawn1.rotation : respawn2.rotation;
    }
    
    private void OnDestroy()
    {
        lifeTime.OnLifeTimeEnded -= RestartNow;
        _uiMenu.OnResumed -= HandleResumed; 
        
        onStartDeath.RemoveListener(OnStartDie);

        onEndDeath.RemoveListener(OnEndDie);
    }

    private void OnStartDie()
    {
        lifeTime.isFastTime = true;
        animator.CrossFade("Die", 0.3f);

        StartCoroutine(LaunchDisintegrate());
    }
    
    private IEnumerator LaunchDisintegrate()
    {
        lifeTime.shapeController.fire.Stop();
        lifeTime.shapeController.fire.Clear();
        while(lifeTime.shapeController.blendValue < 0.98f)
        {
            yield return new WaitForFixedUpdate();
        }
        
        disintegrate.SetFloat("_DissolveProgress", 0);
        lifeTime.shapeController.isFireZero = true;
        
        var step = 0.01f;
        var dissolveProgress = disintegrate.GetFloat("_DissolveProgress");

        while (dissolveProgress < 1)
        {
            dissolveProgress += step;
            disintegrate.SetFloat("_DissolveProgress", dissolveProgress);
            yield return new WaitForFixedUpdate();
        }
        
        disintegrate.SetFloat("_DissolveProgress", 0);
    }

    private void OnEndDie()
    {
        isDeath = false;
        lifeTime.isFastTime = false;
        lifeTime.shapeController.isFireZero = false;
        ashSpawner.Spawn();
        RestartSystem.Restart();
    }
    
    private void HandleResumed() => _justResumed = true;

    private void Update()
    {
        if(Time.timeScale == 0f) return;
        
        MoveCamera();
        if (isDeath && characterController.isGrounded)
        {
            if(!lifeTime.isFastTime)
                Death();
            return;
        }
        PlayerController();
        
        animator.SetBool("isIdleFire", isIdleFire);
    }

    private void MoveCamera()
    {
        Vector2 look = _input.playerLook;

        float sensitivity;
        
        if (!_input.isGamepad)
            sensitivity = mouseSensitivity * _input.mouseSensitivityMultiplay * Time.deltaTime;
        else
            sensitivity = gamepadSensitivity * _input.stickSensitivityMultiplay * Time.deltaTime;

        _yaw   += look.x * sensitivity;
        _pitch -= look.y * sensitivity;
        _pitch = Mathf.Clamp(_pitch, pitchMin, pitchMax);

        cameraTarget.localRotation = Quaternion.Euler(_pitch, _yaw, 0f);
    }
    
    void PlayerController()
    {
        if (!characterController.enabled) return;
        if (_isAnimation) return;
    
        bool isGrounded = characterController.isGrounded || (isOnPlatform && _velocityY <= 0f);

        if (_justResumed && !isGrounded)
        {
            isGrounded = true; // форсируем "на земле" на первый кадр
            _justResumed = false;
        }
        // Coyote time: считаем вниз, пока игрок был на земле, но уже сошёл
        if (isGrounded)
            _coyoteTimeCounter = coyoteTime;
        else
            _coyoteTimeCounter -= Time.deltaTime;

        if (isGrounded && _velocityY < 0f)
            _velocityY = -2f;

        Vector2 move = _input.playerMove;
        Quaternion camYaw = isStaticCamera
            ? Quaternion.Euler(0f, staticCameraTransform.eulerAngles.y, 0f)
            : Quaternion.Euler(0f, _yaw, 0f);
        Vector3 dir = camYaw * new Vector3(move.x, 0f, move.y);
        var speed = moveSpeed * _speedSlowdown;
        characterController.Move(dir * speed * Time.deltaTime);
    
        if (dir.sqrMagnitude > 0.01f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(dir) * Quaternion.Euler(270f, 90f, 0f);
            render.rotation = Quaternion.Slerp(render.rotation, targetRotation, 15f * Time.deltaTime);
            tempPointMove.rotation = Quaternion.Slerp(tempPointMove.rotation, targetRotation, 15f * Time.deltaTime);
        }

        // Прыжок с coyote time: прыгаем, пока счётчик > 0 (даже если уже в воздухе)
        if (_input.isJump && _coyoteTimeCounter > 0f && !_disableJump)
        {
            _velocityY = Mathf.Sqrt(jumpHeight * -2f * gravity);
            _coyoteTimeCounter = 0f; // сбрасываем, чтобы прыгнуть только раз
        }

        _velocityY += gravity * Time.deltaTime;
        characterController.Move(new Vector3(0f, _velocityY, 0f) * Time.deltaTime);

        tempPointMove.position = transform.position;
      
    
        animator.SetBool("isJump", !isGrounded);
        animator.SetInteger("move", move.magnitude > 0 ? 1 : 0);
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

    public void Death()
    {
        if(!isDeath) return;
        
        StartCoroutine(WaitEndDeath(2));
    }
    
    private IEnumerator WaitEndDeath(float time)
    {
        onStartDeath?.Invoke();
        
        yield return new WaitForSeconds(time);

        onEndDeath?.Invoke();
    }

    public void RestartNow()
    {
        isDeath = true;
    }
    
    public void Restart()
    {
        characterController.enabled = false;
    
        transform.position = isTutorial ? respawn1.position : respawn2.position;
        transform.rotation = isTutorial ? respawn1.rotation : respawn2.rotation;
        render.localRotation = _originRenderRotation;
        _velocityY = 0f;

        if (!isTutorial)
        {
            lifeTime.RestartLifeTimer();
            characterController.enabled = true;
        }

        _speedSlowdown = 1f;
        _disableJump = false;
        _coyoteTimeCounter = 0f;
        IsStaticCamera = false;

        if (isTutorial)
            FinishRespawn();
    }

    #endregion

    #region Animations

    private bool _isAnimation;
    
    private void OnAnimatorMove()
    { 
        if(!_isAnimation) return;
        
        animator.ApplyBuiltinRootMotion();
    }

    public void FinishRespawn()
    {
        characterController.enabled = true;
        transform.rotation *= Quaternion.Euler(0f, 180f, 0f);
    }

    private void Climb(ClimbData target)
    {
        var isLooksAt = Vector3.Dot(-render.right, target.transform.forward) > 0.5f;
        var isPlayerHigher = render.position.y > target.transform.position.y;
        
        if(!isLooksAt || isPlayerHigher) return;
        
        _isAnimation = true;
        
        lifeTime.PauseLifeTimer();
        characterController.enabled = false;
        animator.CrossFade("Climb", 0.1f, 0);
        transform.position = target.GetPointStartClimb(transform);
        render.rotation = target.startClimb.rotation *  Quaternion.Euler(270, 90f, 0f);
    }

    public void FinishClimb()
    {
        _isAnimation = false;
        _disableJump = false;
        transform.position = _climbData.GetPointFinishClimb(transform);
        characterController.enabled = true;
        _velocityY = 0f;
        lifeTime.ResumeLifeTimer();
        animator.SetTrigger("isClimb");
    }

    #endregion

    #region Collisions&Triggers

    private ClimbData _climbData;
    private bool _justResumed;

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Climb"))
        {
            Climb(other.GetComponent<ClimbData>());
            _climbData =  other.GetComponent<ClimbData>();
        }
        else if (other.CompareTag("Slowdown"))
        {
            _speedSlowdown = 0.4f;
            _disableJump = true;
        }
    }

    private void OnTriggerStay(Collider other)
    {
        if (other.CompareTag("Slowdown"))
        {
            _speedSlowdown = 0.4f;
            _disableJump = true;
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Slowdown"))
        {
            _speedSlowdown = 1f;
            _disableJump = false;
        }
    }

    #endregion
}
