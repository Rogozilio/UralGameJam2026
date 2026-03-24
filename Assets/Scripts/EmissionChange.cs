using UnityEngine;

namespace Scripts
{
    public class EmissionChange : MonoBehaviour
    {
        public PlatformUp platform;
        public Renderer rend;

        private Material _mat;
        private static readonly int EmissionColor = Shader.PropertyToID("_EmissionColor");

        private void Awake()
        {
            _mat = rend.material;
            _mat.EnableKeyword("_EMISSION");
        }

        private void Update()
        {
            float t = platform.NormalizedHeight;

            Color emission = Color.white * t;
            _mat.SetColor(EmissionColor, emission);
        }
    }
}