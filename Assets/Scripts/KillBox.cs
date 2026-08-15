using Unity.Entities;

namespace Scripts
{
    public struct KillBoxTag : IComponentData
    {
    }

    public sealed class KillBox : MonoEntity
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
