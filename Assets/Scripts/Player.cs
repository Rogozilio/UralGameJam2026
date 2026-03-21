using System;
using System.Collections;
using Scripts;
using Unity.Mathematics;
using UnityEngine;
using Zenject;
using Input = Scripts.Input;

public class Player : MonoBehaviour, IRestart
{
    [Inject] private Input _input;
    
    [Header("References")]
    public LifeTime lifeTime;
    public Animator animator;
    public Transform render;
    
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
    
    public bool isMove => _input.playerMove.magnitude > 0;

    public bool SetIsPushAnim
    {
        set => animator.SetBool("isPush", value);
        get => animator.GetBool("isPush");
    }

    void Awake()
    {
        _yaw   = cameraTarget.eulerAngles.y;
        _pitch = cameraTarget.eulerAngles.x;
        
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible   = false;

        animator.applyRootMotion = false;
        
        ResetOriginPositionAndRotation();
        lifeTime.OnLifeTimeEnded += RestartNow;
        lifeTime.StartLifeTimer();
    }

    private void Update()
    {
        MoveCamera();
        PlayerController();
        
        animator.SetBool("isIdleFire", isIdleFire);
    }

    private void OnDestroy()
    {
        lifeTime.OnLifeTimeEnded -= RestartNow;
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
        _pitch  = Mathf.Clamp(_pitch, pitchMin, pitchMax);

        cameraTarget.localRotation = Quaternion.Euler(_pitch, _yaw, 0f);
    }
    
    void PlayerController()
    {
        if (!characterController.enabled) return;
        if (_isAnimation) return;
    
        bool isGrounded = characterController.isGrounded || (isOnPlatform && _velocityY <= 0f);

        // Coyote time: считаем вниз, пока игрок был на земле, но уже сошёл
        if (isGrounded)
            _coyoteTimeCounter = coyoteTime;
        else
            _coyoteTimeCounter -= Time.deltaTime;

        if (isGrounded && _velocityY < 0f)
            _velocityY = -2f;

        Vector2 move = _input.playerMove;
        Quaternion camYaw = Quaternion.Euler(0f, _yaw, 0f);
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

    private void ResetOriginPositionAndRotation()
    {
        _originPosition = transform.position;
        _originRotation = transform.rotation;
    }

    private void RestartNow()
    {
        RestartSystem.Restart();
    }
    
    public void Restart()
    {
        characterController.enabled = false;
    
        transform.position = _originPosition;
        transform.rotation = _originRotation;
        _velocityY = 0f;
    
        characterController.enabled = true;
        
        lifeTime.RestartLifeTimer();

        _speedSlowdown = 1f;
        _disableJump = false;
        _coyoteTimeCounter = 0f;
    }

    #endregion

    #region Animations

    private bool _isAnimation;
    
    private void OnAnimatorMove()
    { 
        if(!_isAnimation) return;
        
        animator.ApplyBuiltinRootMotion();
    }
    
    private void Climb(ClimbData target)
    {
        // if(!target.isActive) return;
        // target.isActive = false;
        
        var isLooksAt = Vector3.Dot(-render.right, target.transform.forward) > 0.5f;
        var isPlayerHigher = render.position.y > target.transform.position.y;
        
        if(!isLooksAt || isPlayerHigher) return;
        
        _isAnimation = true;
        
        characterController.enabled = false;
        animator.CrossFade("Climb", 0.1f, 0);
        transform.position = target.GetPointStartClimb(transform);
        render.rotation = target.startClimb.rotation *  Quaternion.Euler(270, 90f, 0f);
        
        StartCoroutine(WaitAnimationEnd("Climb", () =>
        {
            _isAnimation = false;
            _disableJump = false;
            transform.position = target.GetPointFinishClimb(transform);
            _velocityY = 0f;
            characterController.enabled = true;
            animator.SetTrigger("isClimb");
        }));
    }
    
    private IEnumerator WaitAnimationEnd(string animationName, Action onComplete = null)
    {
        yield return null;
        while (!animator.GetCurrentAnimatorStateInfo(0).IsName(animationName))
            yield return null;
    
        while (animator.GetCurrentAnimatorStateInfo(0).IsName(animationName) &&
               animator.GetCurrentAnimatorStateInfo(0).normalizedTime < 1f)
        {
            yield return null;
        }

        onComplete?.Invoke();
    }

    #endregion

    #region Collisions&Triggers

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Climb"))
        {
            Climb(other.GetComponent<ClimbData>());
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
