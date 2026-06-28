using UralGameJam.Ecs.Physics3D;

namespace Scripts
{
    public abstract class PhysicsMonoEntity : MonoEntity
    {
        protected override void Awake()
        {
            base.Awake();

            var provider = GetComponent<ColliderAndTriggerDOTSProvider>();

            if (provider == null)
                provider = gameObject.AddComponent<ColliderAndTriggerDOTSProvider>();

            provider.entityA = _entity;
        }
    }
}
