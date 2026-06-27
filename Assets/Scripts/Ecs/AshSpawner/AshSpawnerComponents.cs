using Unity.Entities;
using UnityEngine;

namespace UralGameJam.Ecs.AshSpawner
{
    public sealed class AshSpawnerViewComponent : IComponentData
    {
        public GameObject Owner;
        public GameObject Prefab;
        public Transform SpawnPoint;
        public Transform ParentAfterSpawn;
        public Transform IgnoreHierarchyRoot;
    }

    public struct AshSpawnerComponent : IComponentData
    {
        public LayerMask ParentSearchLayers;
        public float MinSurfaceUpDot;
        public float RevealDuration;
        public bool DetectParentBelowSpawn;
        public bool RevealOnSpawn;
    }

    public sealed class AshRevealViewComponent : IComponentData
    {
        public Transform Transform;
    }

    public struct AshRevealComponent : IComponentData
    {
        public Vector3 TargetScale;
        public float Duration;
        public float Elapsed;
    }
}
