using Scripts;
using UnityEngine;
using UralGameJam.Ecs.AshSpawner;

public class AshSpawner : MonoEntity
{
    [Header("Settings")]
    public GameObject prefab;
    public Transform spawnPoint;
    public Transform parentAfterSpawn;

    [Header("Auto Parent")]
    public bool detectParentBelowSpawn = true;
    [Range(0f, 1f)] public float minSurfaceUpDot = 0.5f;
    public LayerMask parentSearchLayers = ~0;
    public Transform ignoreHierarchyRoot;

    [Header("Reveal")]
    public bool revealOnSpawn = true;
    public float revealDuration = 0.35f;

    protected override void Awake()
    {
        base.Awake();

        _entityManager.AddComponentObject(_entity, new AshSpawnerViewComponent
        {
            Owner = gameObject,
            Prefab = prefab,
            SpawnPoint = spawnPoint,
            ParentAfterSpawn = parentAfterSpawn,
            IgnoreHierarchyRoot = ignoreHierarchyRoot
        });

        _entityManager.AddComponentData(_entity, new AshSpawnerComponent
        {
            DetectParentBelowSpawn = detectParentBelowSpawn,
            MinSurfaceUpDot = minSurfaceUpDot,
            ParentSearchLayers = parentSearchLayers,
            RevealOnSpawn = revealOnSpawn,
            RevealDuration = revealDuration
        });
    }

    private void OnDestroy()
    {
        if (_entityManager.Exists(_entity))
        {
            if (_entityManager.HasComponent<AshSpawnerViewComponent>(_entity))
                _entityManager.RemoveComponent<AshSpawnerViewComponent>(_entity);

            if (_entityManager.HasComponent<AshSpawnerComponent>(_entity))
                _entityManager.RemoveComponent<AshSpawnerComponent>(_entity);
        }
    }
}
