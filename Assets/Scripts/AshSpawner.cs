using Scripts;
using UnityEngine;
using UralGameJam.Ecs.Player;

public class AshSpawner : MonoEntity
{
    [Header("Settings")]
    public GameObject prefab;
    public Transform spawnPoint;

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
        if (prefab == null)
            throw new MissingReferenceException($"{nameof(AshSpawner)}.{nameof(prefab)} is not assigned on {name}");

        base.Awake();

        _entityManager.AddComponentObject(_entity, new AshSpawnerViewComponent
        {
            owner = gameObject,
            prefab = prefab,
            spawnPoint = spawnPoint,
            ignoreHierarchyRoot = ignoreHierarchyRoot
        });

        _entityManager.AddComponentData(_entity, new AshSpawnerComponent
        {
            detectParentBelowSpawn = detectParentBelowSpawn,
            minSurfaceUpDot = minSurfaceUpDot,
            parentSearchLayers = parentSearchLayers,
            revealOnSpawn = revealOnSpawn,
            revealDuration = revealDuration
        });
    }

    private void OnDestroy()
    {
        RemoveComponent<AshSpawnerComponent>();
        RemoveComponent<AshSpawnerViewComponent>();
    }
}
