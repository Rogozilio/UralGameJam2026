using System;
using Unity.Entities;
using UnityEngine;
using UralGameJam.Ecs.Player;
using Object = UnityEngine.Object;

namespace UralGameJam.Ecs.AshSpawner
{
    [UpdateInGroup(typeof(PresentationSystemGroup))]
    [UpdateAfter(typeof(PlayerDeathEffectsSystem))]
    public sealed partial class AshSpawnerSystem : SystemBase
    {
        protected override void OnUpdate()
        {
            var ecbSingleton = SystemAPI.GetSingleton<BeginPresentationEntityCommandBufferSystem.Singleton>();
            var ecb = ecbSingleton.CreateCommandBuffer(World.Unmanaged);

            new SpawnAshJob
            {
                ecb = ecb
            }.Run();

            new ClearDeathStartedJob
            {
                ecb = ecb
            }.Run();

            new AshRevealJob
            {
                ecb = ecb,
                DeltaTime = SystemAPI.Time.DeltaTime
            }.Run();
        }

        [WithAll(typeof(PlayerDeathStartedTag))]
        public partial struct SpawnAshJob : IJobEntity
        {
            public EntityCommandBuffer ecb;

            public void Execute(Entity entity, AshSpawnerViewComponent view, in AshSpawnerComponent spawner)
            {
                var source = view.SpawnPoint != null ? view.SpawnPoint : view.Owner.transform;
                var position = source.position;
                var rotation = source.rotation;
                var parent = view.ParentAfterSpawn;
                var hasSurfaceHit = TryGetSpawnSurfaceHit(view, spawner, position, out var hit);

                if (hasSurfaceHit)
                {
                    position = hit.point;

                    if (parent == null)
                        parent = hit.rigidbody != null ? hit.rigidbody.transform : hit.transform;
                }

                var ash = Object.Instantiate(view.Prefab, position, rotation);

                if (parent != null)
                    ash.transform.SetParent(parent, true);

                if (hasSurfaceHit)
                    AlignAshToSurface(ash.transform, hit.point, hit.normal);

                if (spawner.RevealOnSpawn)
                    StartReveal(entity, ash.transform, spawner.RevealDuration);
            }

            private static bool TryGetSpawnSurfaceHit(AshSpawnerViewComponent view, AshSpawnerComponent spawner,
                Vector3 spawnPosition, out RaycastHit validHit)
            {
                validHit = default;

                if (!spawner.DetectParentBelowSpawn)
                    return false;

                var hits = Physics.RaycastAll(spawnPosition, Vector3.down, 1f, 
                    spawner.ParentSearchLayers, QueryTriggerInteraction.Ignore);

                if (hits.Length == 0)
                    return false;

                Array.Sort(hits, (left, right) => left.distance.CompareTo(right.distance));
                var ignoredRoot = view.IgnoreHierarchyRoot != null
                    ? view.IgnoreHierarchyRoot
                    : view.Owner.transform.root;

                foreach (var hit in hits)
                {
                    if (hit.transform == null ||
                        ignoredRoot != null && hit.transform.IsChildOf(ignoredRoot) || 
                        Vector3.Dot(hit.normal, Vector3.up) < spawner.MinSurfaceUpDot)
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
                    Transform = target
                });
                ecb.AddComponent(entity, new AshRevealComponent
                {
                    TargetScale = targetScale,
                    Duration = Mathf.Max(0.01f, duration)
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

        [WithAll(typeof(PlayerDeathStartedTag))]
        public partial struct ClearDeathStartedJob : IJobEntity
        {
            public EntityCommandBuffer ecb;

            public void Execute(Entity entity)
            {
                ecb.RemoveComponent<PlayerDeathStartedTag>(entity);
            }
        }

        public partial struct AshRevealJob : IJobEntity
        {
            public EntityCommandBuffer ecb;
            public float DeltaTime;

            public void Execute(Entity entity, AshRevealViewComponent view, ref AshRevealComponent reveal)
            {
                if (view.Transform == null)
                {
                    RemoveRevealComponents(entity);
                    return;
                }

                reveal.Elapsed += DeltaTime;
                var progress = Mathf.Clamp01(reveal.Elapsed / reveal.Duration);
                var scale = reveal.TargetScale;
                scale.z = Mathf.Lerp(0f, reveal.TargetScale.z, Mathf.SmoothStep(0f, 1f, progress));
                view.Transform.localScale = scale;

                if (progress < 1f)
                    return;

                view.Transform.localScale = reveal.TargetScale;
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
