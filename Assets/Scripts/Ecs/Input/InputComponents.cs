using Unity.Entities;
using UnityEngine;

namespace UralGameJam.Ecs.Game
{
    public sealed class InputSource : IComponentData
    {
        public Scripts.Input View;
    }

    public struct InputComponent : IComponentData
    {
        public Vector2 Move;
        public Vector2 Look;
        public bool JumpPressed;
        public bool JumpHeld;
        public bool IsGamepad;
        public bool EscapePressed;
        public float MouseSensitivityMultiplier;
        public float StickSensitivityMultiplier;
    }
}
