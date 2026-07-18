using Unity.Entities;
using UnityEngine;

namespace UralGameJam.Ecs.Game
{
    public sealed class InputSource : IComponentData
    {
        public Scripts.Input view;
    }

    public struct InputComponent : IComponentData
    {
        public Vector2 move;
        public Vector2 look;
        public bool jumpPressed;
        public bool jumpHeld;
        public bool isGamepad;
        public bool escapePressed;
        public float mouseSensitivityMultiplier;
        public float stickSensitivityMultiplier;
    }
}
