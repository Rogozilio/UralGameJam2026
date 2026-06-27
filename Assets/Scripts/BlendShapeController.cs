using UnityEngine;
using UnityEngine.Serialization;
using UralGameJam.Ecs.BlendShape;

namespace Scripts
{
    public class BlendShapeController : MonoEntity
    {
        public ParticleSystem fire;
        public AnimationCurve curve;
        public SkinnedMeshRenderer[] skinnedMeshRenderer;
    
        [FormerlySerializedAs("_blendValue")]
        [Range(0f, 1f)]
        public float blendValue;

        [FormerlySerializedAs("_isFireZero")]
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

        protected override void Awake()
        {
            base.Awake();
            
            _entityManager.AddComponentObject(_entity, new BlendShapeViewComponent
            {
                Fire = fire,
                Curve = curve,
                SkinnedMeshRenderers = skinnedMeshRenderer,
                GradientRenderers = gradientRenderers
            });
            
            _entityManager.AddComponentData(_entity, new BlendShapeComponent
            {
                BlendValue = blendValue,
                IsFireZero = isFireZero
            });
        }

        private void OnDestroy()
        {
            if (_entityManager.Exists(_entity))
            {
                if (_entityManager.HasComponent<BlendShapeViewComponent>(_entity))
                    _entityManager.RemoveComponent<BlendShapeViewComponent>(_entity);

                if (_entityManager.HasComponent<BlendShapeComponent>(_entity))
                    _entityManager.RemoveComponent<BlendShapeComponent>(_entity);
            }
        }
    }
}
