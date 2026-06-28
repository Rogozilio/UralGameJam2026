using UralGameJam.Ecs.Physics3D;

namespace Scripts
{
    public sealed class KillBox : PhysicsMonoEntity
    {
        protected override void Awake()
        {
            base.Awake();
            _entityManager.AddComponent<KillBoxTag>(_entity);
        }
        
        private void OnDestroy()
        {
            RemoveComponent<KillBoxTag>();
        }
    }
}
