using Scripts;
using UnityEngine;
using UralGameJam.Ecs.Player;

namespace DefaultNamespace
{
    public class AreaRefire : MonoEntity
    {
        private readonly int _colorId = Shader.PropertyToID("_Color");
        private readonly int _innerRadiusId = Shader.PropertyToID("_InnerRadius");

        public SphereCollider sphereCollider;
        public float radius;

        [Header("Boundary")]
        [SerializeField] private MeshRenderer boundaryRenderer;
        [SerializeField, Min(0.01f)] private float ringWidth = 0.2f;
        [SerializeField, Min(0.1f)] private float projectionHeight = 3f;
        [SerializeField] private Color boundaryColor = new(1f, 0.02f, 0f, 0.9f);

        private MaterialPropertyBlock _propertyBlock;

        protected override void Awake()
        {
            base.Awake();
            
            if (sphereCollider == null)
                throw new MissingReferenceException(
                    $"{nameof(AreaRefire)}.{nameof(sphereCollider)} is not assigned on {name}");

            if (boundaryRenderer == null)
                throw new MissingReferenceException(
                    $"{nameof(AreaRefire)}.{nameof(boundaryRenderer)} is not assigned on {name}");
            
            _entityManager.AddComponent<AreaRefireTag>(_entity);
        }

        private void OnValidate()
        {
            sphereCollider ??= GetComponent<SphereCollider>();

            radius = Mathf.Max(0.01f, radius);
            ringWidth = Mathf.Clamp(ringWidth, 0.01f, radius);
            projectionHeight = Mathf.Max(0.1f, projectionHeight);

            if (sphereCollider != null)
                sphereCollider.radius = radius;

            UpdateBoundary();
        }

        private void OnEnable()
        {
            if (boundaryRenderer != null)
                boundaryRenderer.enabled = true;
        }

        private void OnDisable()
        {
            if (boundaryRenderer != null)
                boundaryRenderer.enabled = false;
        }

        private void OnDestroy()
        {
            _entityManager.RemoveComponent<AreaRefireTag>(_entity);
        }

        private void UpdateBoundary()
        {
            if (boundaryRenderer == null)
                return;

            var diameter = radius * 2f;
            boundaryRenderer.transform.localPosition = sphereCollider.center;
            boundaryRenderer.transform.localRotation = Quaternion.identity;
            boundaryRenderer.transform.localScale = new Vector3(diameter, projectionHeight, diameter);

            _propertyBlock ??= new MaterialPropertyBlock();
            _propertyBlock.SetColor(_colorId, boundaryColor);
            _propertyBlock.SetFloat(_innerRadiusId, 1f - ringWidth / radius);
            boundaryRenderer.SetPropertyBlock(_propertyBlock);
        }
    }
}
