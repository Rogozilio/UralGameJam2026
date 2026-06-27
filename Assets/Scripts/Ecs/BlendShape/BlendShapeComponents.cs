using Unity.Entities;
using UnityEngine;

namespace UralGameJam.Ecs.BlendShape
{
    public sealed class BlendShapeViewComponent : IComponentData
    {
        public ParticleSystem Fire;
        public AnimationCurve Curve;
        public SkinnedMeshRenderer[] SkinnedMeshRenderers;
        public Scripts.BlendShapeController.GradientRendererEntry[] GradientRenderers;
    }

    public struct BlendShapeComponent : IComponentData
    {
        public float BlendValue;
        public bool IsFireZero;
    }

    public struct RestartFireRequestTag : IComponentData
    {
    }
}
