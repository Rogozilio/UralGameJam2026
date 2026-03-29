using UnityEngine;

namespace Scripts
{
    public class PlayerRespawnTextureCycler : MonoBehaviour, IRestart
    {
        private static readonly int BaseMapId = Shader.PropertyToID("_BaseMap");
        private static readonly int DissolveProgressId = Shader.PropertyToID("_DissolveProgress");

        [Header("Target")]
        public Renderer targetRenderer;
        [Min(0)] public int materialIndex;

        [Header("Textures")]
        public Texture2D[] textures;

        private int _currentTextureIndex = -1;

        private void Awake()
        {
            if (targetRenderer == null)
                targetRenderer = GetComponentInChildren<Renderer>();
        }

        public void Restart()
        {
            if (targetRenderer == null || textures == null || textures.Length == 0)
                return;

            _currentTextureIndex = (_currentTextureIndex + 1) % textures.Length;
            ApplyTexture(textures[_currentTextureIndex]);
        }

        private void ApplyTexture(Texture texture)
        {
            Material[] materials = targetRenderer.materials;
            if (materialIndex < 0 || materialIndex >= materials.Length)
                return;

            Material material = materials[materialIndex];
            if (!material.HasProperty(BaseMapId))
                return;

            material.SetTexture(BaseMapId, texture);

            if (material.HasProperty(DissolveProgressId))
                material.SetFloat(DissolveProgressId, 0f);
        }
    }
}
