using Scripts;
using UnityEngine;
using UralGameJam.Ecs.Player;

namespace DefaultNamespace
{
    public class SpawnBox : MonoEntity
    {
        public Transform spawnPoint;
        protected override void Awake()
        {
            if (spawnPoint == null)
                throw new MissingReferenceException($"{nameof(SpawnBox)}.{nameof(spawnPoint)} is not assigned on {name}");

            base.Awake();
            
            _entityManager.AddComponentObject(_entity, new SpawnBoxComponent()
            {
                target = spawnPoint
            });
        }
    }
}
