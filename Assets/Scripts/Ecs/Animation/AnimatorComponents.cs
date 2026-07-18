using Unity.Entities;
using UnityEngine;

namespace UralGameJam.Ecs.Animation
{
    public sealed class AnimatorViewComponent : IComponentData
    {
        public Animator animator;
    }

    public struct AnimatorStateComponent : IComponentData
    {
        public bool applyRootMotion;
    }

    public enum AnimatorCommandType : byte
    {
        SetBool,
        SetInteger,
        SetFloat,
        SetTrigger,
        CrossFade
    }

    public struct AnimatorCommand : IBufferElementData
    {
        public AnimatorCommandType type;
        public int nameHash;
        public int integerValue;
        public float floatValue;
        public float transitionDuration;
        public int layer;

        public static AnimatorCommand SetBool(int nameHash, bool value)
        {
            return new AnimatorCommand
            {
                type = AnimatorCommandType.SetBool,
                nameHash = nameHash,
                integerValue = value ? 1 : 0
            };
        }

        public static AnimatorCommand SetInteger(int nameHash, int value)
        {
            return new AnimatorCommand
            {
                type = AnimatorCommandType.SetInteger,
                nameHash = nameHash,
                integerValue = value
            };
        }

        public static AnimatorCommand SetFloat(int nameHash, float value)
        {
            return new AnimatorCommand
            {
                type = AnimatorCommandType.SetFloat,
                nameHash = nameHash,
                floatValue = value
            };
        }

        public static AnimatorCommand SetTrigger(int nameHash)
        {
            return new AnimatorCommand
            {
                type = AnimatorCommandType.SetTrigger,
                nameHash = nameHash
            };
        }

        public static AnimatorCommand CrossFade(int stateHash, float transitionDuration, int layer = 0)
        {
            return new AnimatorCommand
            {
                type = AnimatorCommandType.CrossFade,
                nameHash = stateHash,
                transitionDuration = transitionDuration,
                layer = layer
            };
        }
    }
}
