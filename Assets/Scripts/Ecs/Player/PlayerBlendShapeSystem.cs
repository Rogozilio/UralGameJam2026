using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace UralGameJam.Ecs.Player
{
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(PlayerDeathSystem))]
    public sealed partial class PlayerBlendShapeSystem : SystemBase
    {
        private static readonly int GradientFillId = Shader.PropertyToID("_GradientFill");
        private static readonly int MaskProgressId = Shader.PropertyToID("_MaskProgress");

        protected override void OnUpdate()
        {
            new BlendShapeJob().Run();
            new RestartBlendShapeJob().Run();

            var ecbSingleton = SystemAPI.GetSingleton<BeginPresentationEntityCommandBufferSystem.Singleton>();
            var ecb = ecbSingleton.CreateCommandBuffer(World.Unmanaged);
            new RestartFireJob
            {
                ecb = ecb
            }.Run();

            new BlendShapeViewJob
            {
                gradientFillId = GradientFillId,
                maskProgressId = MaskProgressId
            }.Run();
        }

        [WithAll(typeof(PlayerRespawnComponent))]
        public partial struct RestartBlendShapeJob : IJobEntity
        {
            public void Execute(ref BlendShapeComponent blendShape)
            {
                blendShape.isFireZero = false;
            }
        }

        public partial struct BlendShapeJob : IJobEntity
        {
            public void Execute(ref BlendShapeComponent blendShape, in LifeTimeComponent lifeTime)
            {
                if (lifeTime.duration <= 0f)
                    return;

                blendShape.blendValue = math.saturate(1f - lifeTime.remainingTime / lifeTime.duration);
            }
        }

        [WithAll(typeof(RestartFireRequestTag))]
        public partial struct RestartFireJob : IJobEntity
        {
            public EntityCommandBuffer ecb;

            public void Execute(Entity entity, BlendShapeViewComponent view)
            {
                view.fire.Stop();
                view.fire.Clear();
                view.fire.Play();

                ecb.RemoveComponent<RestartFireRequestTag>(entity);
            }
        }

        public partial struct BlendShapeViewJob : IJobEntity
        {
            [ReadOnly] public int gradientFillId;
            [ReadOnly] public int maskProgressId;

            public void Execute(BlendShapeViewComponent view, in BlendShapeComponent blendShape)
            {
                ApplyBlendShapes(view, blendShape);
                ApplyGradientFill(view, blendShape);
                ApplyFire(view, blendShape);
            }

            private static void ApplyBlendShapes(BlendShapeViewComponent view, BlendShapeComponent blendShape)
            {
                var minus = 0;

                foreach (var renderer in view.skinnedMeshRenderers)
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
                            blendShape.blendValue) * 100f;

                        if (minus > 0 && i == 2)
                            weight = Mathf.Clamp(weight, 0f, 70f);

                        renderer.SetBlendShapeWeight(i, Mathf.Clamp(weight, 0f, 100f - minus));
                    }

                    minus += 10;
                }
            }

            private void ApplyGradientFill(BlendShapeViewComponent view, BlendShapeComponent blendShape)
            {
                if (view.gradientRenderers == null)
                    return;

                var propertyBlock = view.propertyBlock;

                foreach (var entry in view.gradientRenderers)
                {
                    if (entry.renderer == null ||
                        entry.materialIndex < 0 ||
                        entry.materialIndex >= entry.renderer.sharedMaterials.Length)
                        continue;

                    var start = Mathf.Clamp01(entry.gradientStart + entry.offsetStart);
                    var end = Mathf.Clamp01(entry.gradientEnd + entry.offsetEnd);
                    var gradientValue = Mathf.Lerp(start, end, blendShape.blendValue);

                    propertyBlock.Clear();
                    entry.renderer.GetPropertyBlock(propertyBlock, entry.materialIndex);
                    propertyBlock.SetFloat(gradientFillId, blendShape.isFireZero ? 0f : gradientValue);
                    propertyBlock.SetFloat(maskProgressId, gradientValue);
                    entry.renderer.SetPropertyBlock(propertyBlock, entry.materialIndex);
                }
            }

            private static void ApplyFire(BlendShapeViewComponent view, BlendShapeComponent blendShape)
            {
                var main = view.fire.main;

                if (view.curve != null)
                    main.startSizeXMultiplier = view.curve.Evaluate(blendShape.blendValue);

                if (view.fire.transform.parent == null)
                    return;

                var position = view.fire.transform.parent.localPosition;
                position.y = Mathf.Lerp(
                    0.00109f,
                    0.00015f,
                    Mathf.Clamp01(blendShape.blendValue / 0.3f));
                view.fire.transform.parent.localPosition = position;
            }
        }
    }
}
