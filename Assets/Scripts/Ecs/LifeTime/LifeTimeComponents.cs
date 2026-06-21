using TMPro;
using Unity.Entities;

namespace UralGameJam.Ecs.LifeTime
{
    public sealed class LifeTimeViewComponent : IComponentData
    {
        public TextMeshProUGUI Text;
    }

    public struct LifeTimeComponent : IComponentData, IEnableableComponent
    {
        public float Duration;
        public float RemainingTime;
        public bool IsFastTime;
    }

    public struct LifeTimePausedTag : IComponentData
    {
    }

}
