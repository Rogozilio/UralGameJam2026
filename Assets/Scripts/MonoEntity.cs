using Unity.Entities;
using UnityEngine;

namespace Scripts
{
    public abstract class MonoEntity : MonoBehaviour
    {
        protected Entity _entity;
        protected EntityManager _entityManager;

        protected virtual void Awake()
        {
            _entity = TryGetComponent(out GeneralEntity entity) 
                ? entity.GetOrCreate() 
                : gameObject.AddComponent<GeneralEntity>().GetOrCreate();
            
            _entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;
        }
    }
}
