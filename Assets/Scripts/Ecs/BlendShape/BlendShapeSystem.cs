using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using UralGameJam.Ecs.LifeTime;

namespace UralGameJam.Ecs.BlendShape
{
    [UpdateInGroup(typeof(PresentationSystemGroup))]
    [UpdateAfter(typeof(LifeTimeSystem))]
    public sealed partial class BlendShapeSystem : SystemBase
    {
        private static readonly int GradientFillId = Shader.PropertyToID("_GradientFill");
        private static readonly int MaskProgressId = Shader.PropertyToID("_MaskProgress");

        protected override void OnUpdate()
        {
            var ecbSingleton = SystemAPI.GetSingleton<BeginPresentationEntityCommandBufferSystem.Singleton>();
            var ecb = ecbSingleton.CreateCommandBuffer(World.Unmanaged);
            
            new BlendShapeJob().Run();
            
            new RestartFireJob
            {
                ecb = ecb
            }.Run();

            new BlendShapeViewJob
            {
                GradientFillId = GradientFillId,
                MaskProgressId = MaskProgressId
            }.Run();
        }

        public partial struct BlendShapeJob : IJobEntity
        {
            public void Execute(ref BlendShapeComponent blendShape, in LifeTimeComponent lifeTime)
            {
                if (lifeTime.Duration <= 0f)
                    return;

                blendShape.BlendValue = math.saturate(1f - lifeTime.RemainingTime / lifeTime.Duration);
            }
        }

        [WithAll(typeof(RestartFireRequestTag))]
        public partial struct RestartFireJob : IJobEntity
        {
            public EntityCommandBuffer ecb;

            public void Execute(Entity entity, BlendShapeViewComponent view)
            {
                if (view.Fire != null)
                {
                    view.Fire.Stop();
                    view.Fire.Clear();
                    view.Fire.Play();
                }

                ecb.RemoveComponent<RestartFireRequestTag>(entity);
            }
        }

        public partial struct BlendShapeViewJob : IJobEntity
        {
            [ReadOnly] public int GradientFillId;
            [ReadOnly] public int MaskProgressId;

            public void Execute(BlendShapeViewComponent view, in BlendShapeComponent blendShape)
            {
                ApplyBlendShapes(view, blendShape);
                ApplyGradientFill(view, blendShape);
                ApplyFire(view, blendShape);
            }

            private static void ApplyBlendShapes(BlendShapeViewComponent view, BlendShapeComponent blendShape)
            {
                if (view.SkinnedMeshRenderers == null)
                    return;

                var minus = 0;

                foreach (var renderer in view.SkinnedMeshRenderers)
                {
                    if (renderer == null || renderer.sharedMesh == null)
                        continue;

                    var count = renderer.sharedMesh.blendShapeCount;

                    if (count == 0)
                        continue;

                    var step = 1f / count;

                    for (var i = 0; i < count; i++)
                    {
                        var weight = Mathf.InverseLerp(
                            step * i,
                            step * (i + 1),
                            blendShape.BlendValue) * 100f;

                        if (minus > 0 && i == 2)
                            weight = Mathf.Clamp(weight, 0f, 70f);

                        renderer.SetBlendShapeWeight(i, Mathf.Clamp(weight, 0f, 100f - minus));
                    }

                    minus += 10;
                }
            }

            private void ApplyGradientFill(BlendShapeViewComponent view, BlendShapeComponent blendShape)
            {
                if (view.GradientRenderers == null)
                    return;

                var propertyBlock = new MaterialPropertyBlock();

                foreach (var entry in view.GradientRenderers)
                {
                    if (entry.renderer == null ||
                        entry.materialIndex < 0 ||
                        entry.materialIndex >= entry.renderer.sharedMaterials.Length)
                        continue;

                    var start = Mathf.Clamp01(entry.gradientStart + entry.offsetStart);
                    var end = Mathf.Clamp01(entry.gradientEnd + entry.offsetEnd);
                    var gradientValue = Mathf.Lerp(start, end, blendShape.BlendValue);

                    propertyBlock.Clear();
                    entry.renderer.GetPropertyBlock(propertyBlock, entry.materialIndex);
                    propertyBlock.SetFloat(GradientFillId, blendShape.IsFireZero ? 0f : gradientValue);
                    propertyBlock.SetFloat(MaskProgressId, gradientValue);
                    entry.renderer.SetPropertyBlock(propertyBlock, entry.materialIndex);
                }
            }

            private static void ApplyFire(BlendShapeViewComponent view, BlendShapeComponent blendShape)
            {
                if (view.Fire == null)
                    return;

                var main = view.Fire.main;

                if (view.Curve != null)
                    main.startSizeXMultiplier = view.Curve.Evaluate(blendShape.BlendValue);

                if (view.Fire.transform.parent == null)
                    return;

                var position = view.Fire.transform.parent.localPosition;
                position.y = Mathf.Lerp(
                    0.00109f,
                    0.00015f,
                    Mathf.Clamp01(blendShape.BlendValue / 0.3f));
                view.Fire.transform.parent.localPosition = position;
            }
        }
    }
}
