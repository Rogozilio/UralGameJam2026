using Unity.Mathematics;
using UnityEngine;

namespace Scripts
{
    [ExecuteAlways]
    public class BlendShapeController : MonoBehaviour
    {
        public ParticleSystem fire;
        public AnimationCurve curve;
        public AnimationCurve curveY;
        public SkinnedMeshRenderer[] skinnedMeshRenderer;
    
        [Range(0f, 1f)]
        public float blendValue = 0f;

        public bool isFireZero;

        [System.Serializable]
        public struct GradientRendererEntry
        {
            public Renderer renderer;
            public int materialIndex;
            [Range(0f, 1f)] public float gradientStart;
            [Range(0f, 1f)] public float gradientEnd;
            [Range(-1f, 1f)] public float offsetStart; // сдвиг начала
            [Range(-1f, 1f)] public float offsetEnd;   // сдвиг конца
        }

        public GradientRendererEntry[] gradientRenderers;

        private static readonly int GradientFillID = Shader.PropertyToID("_GradientFill");
        private static readonly int maskProgress = Shader.PropertyToID("_MaskProgress");

        private void Update()
        {
            ApplyBlendShapes();
            ApplyGradientFill();

            var main = fire.main;
            main.startSizeXMultiplier = curve.Evaluate(blendValue);
            var vector3 = fire.transform.parent.transform.localPosition;
            vector3.y = Mathf.Lerp(0.00109f, 0.00015f, Mathf.Clamp01(blendValue / 0.3f));
            fire.transform.parent.transform.localPosition = vector3;
        }

        public void ApplyBlendShapes()
        {
            if (skinnedMeshRenderer == null || skinnedMeshRenderer.Length == 0) return;

            var minus = 0;
            foreach (var smr in skinnedMeshRenderer)
            {
                if (smr == null || smr.sharedMesh == null) continue;

                int count = smr.sharedMesh.blendShapeCount;
                if (count == 0) continue;

                float step = 1f / count;

                for (int i = 0; i < count; i++)
                {
                    float segStart = step * i;
                    float segEnd   = step * (i + 1);

                    float weight = Mathf.InverseLerp(segStart, segEnd, blendValue) * 100f;

                    if (minus > 0 && i == 2)
                        weight = Mathf.Clamp(weight, 0f, 70f);
                            
                    weight = Mathf.Clamp(weight, 0f, 100f - minus);

                    smr.SetBlendShapeWeight(i, weight);
                }

                minus += 10;
            }
        }

        public void ApplyGradientFill()
        {
            if (gradientRenderers == null || gradientRenderers.Length == 0) return;

            foreach (var entry in gradientRenderers)
            {
                if (entry.renderer == null) continue;

                int matCount = entry.renderer.sharedMaterials.Length;
                if (entry.materialIndex < 0 || entry.materialIndex >= matCount) continue;

                float start = Mathf.Clamp01(entry.gradientStart + entry.offsetStart);
                float end   = Mathf.Clamp01(entry.gradientEnd   + entry.offsetEnd);

                float gradientValue = Mathf.Lerp(start, end, blendValue);

                MaterialPropertyBlock block = new MaterialPropertyBlock();
                entry.renderer.GetPropertyBlock(block, entry.materialIndex);
                block.SetFloat(GradientFillID, isFireZero ? 0 : gradientValue);
                block.SetFloat(maskProgress, gradientValue);
                entry.renderer.SetPropertyBlock(block, entry.materialIndex);
            }
        }
    }
}