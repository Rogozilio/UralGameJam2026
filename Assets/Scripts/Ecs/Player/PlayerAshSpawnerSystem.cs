using System;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;
using Object = UnityEngine.Object;

namespace UralGameJam.Ecs.Player
{
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(PlayerDeathSystem))]
    public sealed partial class PlayerAshSpawnerSystem : SystemBase
    {
        protected override void OnCreate()
        {
            RequireForUpdate<AshSpawnerComponent>();
        }

        protected override void OnUpdate()
        {
            var ecbSingleton = SystemAPI.GetSingleton<BeginPresentationEntityCommandBufferSystem.Singleton>();
            var ecb = ecbSingleton.CreateCommandBuffer(World.Unmanaged);

            new SpawnAshJob
            {
                ecb = ecb
            }.Run();

            new AshRevealJob
            {
                ecb = ecb,
                deltaTime = SystemAPI.Time.DeltaTime
            }.Run();
        }

        [WithAll(typeof(PlayerDeathStartedTag))]
        public partial struct SpawnAshJob : IJobEntity
        {
            public EntityCommandBuffer ecb;

            public void Execute(Entity entity, AshSpawnerViewComponent view, in AshSpawnerComponent spawner)
            {
                var source = view.spawnPoint != null ? view.spawnPoint : view.owner.transform;
                var position = source.position;
                var rotation = source.rotation;
                var hasSurfaceHit = TryGetSpawnSurfaceHit(view, spawner, position, out var hit);

                if (hasSurfaceHit)
                {
                    position = hit.point;
                }

                //TODO: заменить на пул
                var ash = Object.Instantiate(view.prefab, position, rotation);

                var parent = hit.rigidbody != null ? hit.rigidbody.transform : hit.transform;
                ash.transform.SetParent(parent, true);

                if (hasSurfaceHit)
                    AlignAshToSurface(ash.transform, hit.point, hit.normal);

                if (spawner.revealOnSpawn)
                    StartReveal(entity, ash.transform, spawner.revealDuration);
            }

            private static bool TryGetSpawnSurfaceHit(AshSpawnerViewComponent view, AshSpawnerComponent spawner,
                Vector3 spawnPosition, out RaycastHit validHit)
            {
                validHit = default;

                if (!spawner.detectParentBelowSpawn)
                    return false;

                var hits = Physics.RaycastAll(spawnPosition, Vector3.down, 1f, 
                    spawner.parentSearchLayers, QueryTriggerInteraction.Ignore);

                if (hits.Length == 0)
                    return false;

                Array.Sort(hits, (left, right) => left.distance.CompareTo(right.distance));
                var ignoredRoot = view.ignoreHierarchyRoot != null
                    ? view.ignoreHierarchyRoot
                    : view.owner.transform.root;

                foreach (var hit in hits)
                {
                    if (hit.transform == null ||
                        ignoredRoot != null && hit.transform.IsChildOf(ignoredRoot) || 
                        Vector3.Dot(hit.normal, Vector3.up) < spawner.minSurfaceUpDot)
                        continue;

                    validHit = hit;
                    return true;
                }

                return false;
            }

            private void StartReveal(Entity entity, Transform target, float duration)
            {
                var targetScale = target.localScale;
                var initialScale = targetScale;
                initialScale.z = 0f;
                target.localScale = initialScale;

                ecb.AddComponent(entity, new AshRevealViewComponent
                {
                    transform = target
                });
                ecb.AddComponent(entity, new AshRevealComponent
                {
                    targetScale = targetScale,
                    duration = Mathf.Max(0.01f, duration)
                });
            }

            private static void AlignAshToSurface(Transform ash, Vector3 surfacePoint, Vector3 surfaceNormal)
            {
                if (!TryGetCombinedBounds(ash, out var bounds))
                    return;

                var normal = surfaceNormal.sqrMagnitude > 0f
                    ? surfaceNormal.normalized
                    : Vector3.up;
                var absNormal = new Vector3(Mathf.Abs(normal.x), Mathf.Abs(normal.y), Mathf.Abs(normal.z));
                ash.position += surfacePoint + normal * Vector3.Dot(bounds.extents, absNormal) - bounds.center;
            }

            private static bool TryGetCombinedBounds(Transform root, out Bounds combinedBounds)
            {
                var renderers = root.GetComponentsInChildren<Renderer>();

                if (renderers.Length == 0)
                {
                    combinedBounds = default;
                    return false;
                }

                combinedBounds = renderers[0].bounds;

                for (var i = 1; i < renderers.Length; i++)
                    combinedBounds.Encapsulate(renderers[i].bounds);

                return true;
            }
        }

        public partial struct AshRevealJob : IJobEntity
        {
            public EntityCommandBuffer ecb;
            [ReadOnly] public float deltaTime;

            public void Execute(Entity entity, AshRevealViewComponent view, ref AshRevealComponent reveal)
            {
                if (view.transform == null)
                {
                    RemoveRevealComponents(entity);
                    return;
                }

                reveal.elapsed += deltaTime;
                var progress = Mathf.Clamp01(reveal.elapsed / reveal.duration);
                var scale = reveal.targetScale;
                scale.z = Mathf.Lerp(0f, reveal.targetScale.z, Mathf.SmoothStep(0f, 1f, progress));
                view.transform.localScale = scale;

                if (progress < 1f)
                    return;

                view.transform.localScale = reveal.targetScale;
                RemoveRevealComponents(entity);
            }

            private void RemoveRevealComponents(Entity entity)
            {
                ecb.RemoveComponent<AshRevealViewComponent>(entity);
                ecb.RemoveComponent<AshRevealComponent>(entity);
            }
        }
    }
}
