using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Scripts
{
    public class Input : MonoBehaviour
    {
        private InputSystem_Actions _input;

        public float mouseSensitivityMultiplay = 1f;
        public float stickSensitivityMultiplay = 1f;
        public Vector2 playerMove => _input.Player.Move.ReadValue<Vector2>();
        public Vector2 playerLook => _input.Player.Look.ReadValue<Vector2>();
        public bool isJump => _input.Player.Jump.WasPressedThisFrame();
        public bool isJumpHeld => _input.Player.Jump.IsPressed();
        public bool isGamepad => _input.Player.Look.activeControl?.device is Gamepad;
        public bool isEscape => _input.Player.Esc.WasPressedThisFrame();
        
        private Action<InputAction.CallbackContext> _onEscPerformed;

        public Action OnAction;
        public Action OnActionEcs;

        private void Awake()
        {
            _input = new InputSystem_Actions();
            _onEscPerformed = (ctx) => { OnActionEcs?.Invoke(); };
        }

        private void OnEnable()
        {
            _input.Enable();

            //_input.FindAction("Action").performed += (ctx) => { OnAction?.Invoke(); };
            _input.Player.Esc.performed += _onEscPerformed;
        }

        private void OnDisable()
        {
            //_input.FindAction("Action").performed -= (ctx) => { OnAction?.Invoke(); };
            _input.Player.Esc.performed -= _onEscPerformed;
            
            _input.Disable();
        }

        public void ChangeMouseSensitivity(float value)
        {
            mouseSensitivityMultiplay = value;
        }
    }
}
