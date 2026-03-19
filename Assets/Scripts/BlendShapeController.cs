using UnityEngine;

namespace Scripts
{
    [ExecuteAlways]
    public class BlendShapeController : MonoBehaviour
    {
        public SkinnedMeshRenderer skinnedMeshRenderer;
    
        [Range(0f, 1f)]
        public float blendValue = 0f;

        private void Update()
        {
            ApplyBlendShapes();
        }

        public void ApplyBlendShapes()
        {
            if (skinnedMeshRenderer == null) return;

            int count = skinnedMeshRenderer.sharedMesh.blendShapeCount; // 11
            float step = 1f / count; // ~0.0909

            for (int i = 0; i < count; i++)
            {
                float segStart = step * i;
                float segEnd   = step * (i + 1);

                float weight = Mathf.InverseLerp(segStart, segEnd, blendValue) * 100f;
                weight = Mathf.Clamp(weight, 0f, 100f);

                skinnedMeshRenderer.SetBlendShapeWeight(i, weight);
            }
        }
    }
}