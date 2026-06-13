using UralGameJam.Ecs.Game;
using Unity.Entities;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Scripts
{
    [DefaultExecutionOrder(-100)]
    public class Input : MonoBehaviour
    {
        private InputSystem_Actions _input;
        private EntityManager _entityManager;
        private Entity _sourceEntity;

        public Vector2 playerMove => _input.Player.Move.ReadValue<Vector2>();
        public Vector2 playerLook => _input.Player.Look.ReadValue<Vector2>();
        public bool isJump => _input.Player.Jump.WasPressedThisFrame();
        public bool isJumpHeld => _input.Player.Jump.IsPressed();
        public bool isGamepad => _input.Player.Look.activeControl?.device is Gamepad;
        public bool isEscape => _input.Player.Esc.WasPressedThisFrame();

        private void Awake()
        {
            _input = new InputSystem_Actions();
            _entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;
            _sourceEntity = _entityManager.CreateEntity();
            _entityManager.AddComponentObject(_sourceEntity, new InputSource
            {
                View = this
            });
            _entityManager.AddComponentData(_sourceEntity, new InputComponent
            {
                MouseSensitivityMultiplier = 1f,
                StickSensitivityMultiplier = 1f
            });
        }

        private void OnEnable()
        {
            _input.Enable();
        }

        private void OnDisable()
        {
            _input.Disable();
        }

        private void OnDestroy()
        {
            if (_entityManager.Exists(_sourceEntity))
                _entityManager.DestroyEntity(_sourceEntity);
        }
    }
}