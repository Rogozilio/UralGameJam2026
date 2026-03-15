using System;
using UnityEngine;

public class Player : MonoBehaviour
{
    [Header("CharacterController")]
    public CharacterController characterController;
    public float moveSpeed = 5f;
    public float jumpHeight = 1.5f;
    public float gravity = -20f;
    private float _velocityY;
    
    [Header("Camera")]
    public Transform cameraTarget;
    public float sensitivity = 0.15f;
    public float pitchMin = -80f;
    public float pitchMax = 80f;
    
    private InputSystem_Actions _input;
    private float _pitch;
    private float _yaw;

    void Awake()
    {
        _input = new InputSystem_Actions();
        
        _yaw   = cameraTarget.eulerAngles.y;
        _pitch = cameraTarget.eulerAngles.x;
        
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible   = false;
    }

    void OnEnable()
    {
        _input.Enable();
    }

    void OnDisable()
    {
        _input.Disable();
    }

    private void Update()
    {
        PlayerController();
    }

    void FixedUpdate()
    {
        MoveCamera();
    }

    private void MoveCamera()
    {
        Vector2 look = _input.Player.Look.ReadValue<Vector2>();

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
        Vector2 move = _input.Player.Move.ReadValue<Vector2>();

        // Направление камеры по горизонтали (только yaw, без pitch)
        Quaternion camYaw = Quaternion.Euler(0f, _yaw, 0f);

        // Переводим input в мировое пространство относительно камеры
        Vector3 dir = camYaw * new Vector3(move.x, 0f, move.y);
        characterController.Move(dir * moveSpeed * Time.deltaTime);

        // Прыжок
        if (_input.Player.Jump.WasPressedThisFrame() && isGrounded)
            _velocityY = Mathf.Sqrt(jumpHeight * -2f * gravity);

        // Гравитация
        _velocityY += gravity * Time.deltaTime;
        characterController.Move(new Vector3(0f, _velocityY, 0f) * Time.deltaTime);
    }
}
