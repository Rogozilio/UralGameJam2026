using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace UralGameJam.Ecs.LifeTime
{
    [UpdateInGroup(typeof(PresentationSystemGroup))]
    [UpdateAfter(typeof(UralGameJam.Ecs.Player.PlayerMovementSystem))]
    public sealed partial class LifeTimeSystem : SystemBase
    { 
        protected override void OnUpdate()
        {
            new LifeTimeJob()
            {
                fixedDeltaTime = SystemAPI.Time.fixedDeltaTime,
                fastTimeMultiplier = 40f
            }.Run();
            new LifeTimeViewJob().Run();
        }

        [WithNone(typeof(LifeTimePausedTag))]
        public partial struct LifeTimeJob : IJobEntity
        {
            [ReadOnly] public float fixedDeltaTime;
            [ReadOnly]  public float fastTimeMultiplier;
            public void Execute(ref LifeTimeComponent lifeTime, EnabledRefRW<LifeTimeComponent> enabled)
            {
                if (lifeTime.RemainingTime <= 0f)
                    enabled.ValueRW = false;
                
                var deltaRemainingTime = fixedDeltaTime * (lifeTime.IsFastTime ? fastTimeMultiplier : 1f);
                lifeTime.RemainingTime = math.clamp(lifeTime.RemainingTime - deltaRemainingTime, 0f, lifeTime.Duration);
            }
        }

        public partial struct LifeTimeViewJob : IJobEntity
        {
            public void Execute(LifeTimeViewComponent view, LifeTimeComponent lifeTime)
            {
                view.Text.text = lifeTime.RemainingTime <= 0f ? "0" : lifeTime.RemainingTime.ToString("00");
            }
        }
    }
}
