using System;
using Scripts;
using UnityEngine;
using Zenject;
using Input = Scripts.Input;

public class Player : MonoBehaviour, IRestart
{
    [Inject] private Input _input;
    
    [Header("Base")]
    public LifeTime lifeTime;
    
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
    
    private float _pitch;
    private float _yaw;

    void Awake()
    {
        _yaw   = cameraTarget.eulerAngles.y;
        _pitch = cameraTarget.eulerAngles.x;
        
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible   = false;

        ResetOriginPositionAndRotation();
        lifeTime.OnLifeTimeEnded += Restart;
        lifeTime.StartLifeTimer();
    }

    private void Update()
    {
        PlayerController();
    }

    void FixedUpdate()
    {
        MoveCamera();
    }

    private void OnDestroy()
    {
        lifeTime.OnLifeTimeEnded -= Restart;
    }

    private void MoveCamera()
    {
        Vector2 look = _input.playerLook;

        float sensitivity;
        
        if (!_input.isGamepad)
            sensitivity = mouseSensitivity * _input.mouseSensitivityMultiplay;
        else
            sensitivity = gamepadSensitivity * _input.stickSensitivityMultiplay * Time.deltaTime;

        _yaw   += look.x * sensitivity;
        _pitch -= look.y * sensitivity;
        _pitch  = Mathf.Clamp(_pitch, pitchMin, pitchMax);

        cameraTarget.localRotation = Quaternion.Euler(_pitch, _yaw, 0f);
    }
    
    void PlayerController()
    {
        bool isGrounded = characterController.isGrounded;

        // Сбрасываем вертикальную скорость когда стоим на земле
        if (isGrounded && _velocityY < 0f)
            _velocityY = -2f;

        // Читаем WASD / стик
        Vector2 move = _input.playerMove;

        // Направление камеры по горизонтали (только yaw, без pitch)
        Quaternion camYaw = Quaternion.Euler(0f, _yaw, 0f);

        // Переводим input в мировое пространство относительно камеры
        Vector3 dir = camYaw * new Vector3(move.x, 0f, move.y);
        characterController.Move(dir * moveSpeed * Time.deltaTime);

        // Прыжок
        if (_input.isJump && isGrounded)
            _velocityY = Mathf.Sqrt(jumpHeight * -2f * gravity);

        // Гравитация
        _velocityY += gravity * Time.deltaTime;
        characterController.Move(new Vector3(0f, _velocityY, 0f) * Time.deltaTime);
    }

    private Vector3 _originPosition;
    private Quaternion _originRotation;

    private void ResetOriginPositionAndRotation()
    {
        _originPosition = transform.position;
        _originRotation = transform.rotation;
    }
    
    public void Restart()
    {
        characterController.enabled = false;
    
        transform.position = _originPosition;
        transform.rotation = _originRotation;
        _velocityY = 0f;
    
        characterController.enabled = true;
        
        lifeTime.RestartLifeTimer();
    }
}
