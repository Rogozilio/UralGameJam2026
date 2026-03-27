using System;
using UnityEngine;

public class AshSpawner : MonoBehaviour
{
    [Header("Settings")]
    public GameObject prefab;
    public Transform spawnPoint;
    public Transform parentAfterSpawn;
    public KeyCode spawnKey = KeyCode.Space;

    [Header("Auto Parent")]
    public bool detectParentBelowSpawn = true;
    public float parentProbeHeight = 0.5f;
    public float parentSearchDistance = 5f;
    [Range(0f, 1f)] public float minSurfaceUpDot = 0.5f;
    public LayerMask parentSearchLayers = ~0;
    public Transform ignoreHierarchyRoot;

    [Header("Reveal")]
    public bool revealOnSpawn = true;
    public float revealDuration = 0.35f;

    public void Spawn()
    {
        if (prefab == null)
        {
            Debug.LogWarning("PrefabSpawner: prefab не назначен!");
            return;
        }

        var pos = spawnPoint != null ? spawnPoint.position : transform.position;
        var rot = spawnPoint != null ? spawnPoint.rotation : transform.rotation;
        var ashParent = parentAfterSpawn;

        bool hasSurfaceHit = TryGetSpawnSurfaceHit(pos, out RaycastHit hit);
        if (hasSurfaceHit)
        {
            pos = hit.point;

            if (ashParent == null)
                ashParent = hit.rigidbody != null ? hit.rigidbody.transform : hit.transform;
        }

        var ash = Instantiate(prefab, pos, rot);

        if (ashParent != null)
            ash.transform.SetParent(ashParent, true);

        if (hasSurfaceHit)
            AlignAshToSurface(ash.transform, hit.point, hit.normal);

        if (revealOnSpawn)
        {
            var reveal = ash.GetComponent<AshReveal>();
            if (reveal == null)
                reveal = ash.AddComponent<AshReveal>();

            reveal.Play(revealDuration);
        }
    }

    private bool TryGetSpawnSurfaceHit(Vector3 spawnPosition, out RaycastHit validHit)
    {
        validHit = default;

        if (!detectParentBelowSpawn)
            return false;

        Vector3 origin = spawnPosition;
        float distance = 1f;
        RaycastHit[] hits = Physics.RaycastAll(origin, Vector3.down, distance, parentSearchLayers, QueryTriggerInteraction.Ignore);

        if (hits.Length == 0)
            return false;

        Array.Sort(hits, (left, right) => left.distance.CompareTo(right.distance));

        Transform ignoredRoot = ignoreHierarchyRoot != null ? ignoreHierarchyRoot : transform.root;

        foreach (var hit in hits)
        {
            if (hit.transform == null)
                continue;

            if (ignoredRoot != null && hit.transform.IsChildOf(ignoredRoot))
                continue;

            if (Vector3.Dot(hit.normal, Vector3.up) < minSurfaceUpDot)
                continue;

            validHit = hit;
            return true;
        }

        return false;
    }

    private static void AlignAshToSurface(Transform ash, Vector3 surfacePoint, Vector3 surfaceNormal)
    {
        if (!TryGetCombinedBounds(ash, out Bounds bounds))
            return;

        Vector3 normal = surfaceNormal.sqrMagnitude > 0f ? surfaceNormal.normalized : Vector3.up;
        Vector3 extents = bounds.extents;
        Vector3 absNormal = new Vector3(Mathf.Abs(normal.x), Mathf.Abs(normal.y), Mathf.Abs(normal.z));
        float supportDistance = Vector3.Dot(extents, absNormal);
        Vector3 desiredCenter = surfacePoint + normal * supportDistance;
        Vector3 delta = desiredCenter - bounds.center;

        ash.position += delta;
    }

    private static bool TryGetCombinedBounds(Transform root, out Bounds combinedBounds)
    {
        Renderer[] renderers = root.GetComponentsInChildren<Renderer>();
        if (renderers.Length == 0)
        {
            combinedBounds = default;
            return false;
        }

        combinedBounds = renderers[0].bounds;

        for (int i = 1; i < renderers.Length; i++)
            combinedBounds.Encapsulate(renderers[i].bounds);

        return true;
    }
}
