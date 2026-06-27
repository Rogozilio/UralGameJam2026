using Unity.Entities;
using TMPro;
using UnityEngine;
using UralGameJam.Ecs.BlendShape;
using UralGameJam.Ecs.LifeTime;

namespace Scripts
{
    public class LifeTime : MonoEntity
    {
        public float time;
        public TextMeshProUGUI text;

        protected override void Awake()
        {
            base.Awake();
            
            _entityManager.AddComponentObject(_entity, new LifeTimeViewComponent
            {
                Text = text
            });
            
            _entityManager.AddComponentData(_entity, new LifeTimeComponent
            {
                Duration = time,
                RemainingTime = time,
                IsFastTime = false
            });
            _entityManager.SetComponentEnabled<LifeTimeComponent>(_entity, true);
            _entityManager.AddComponent<LifeTimePausedTag>(_entity);
            _entityManager.SetComponentEnabled<LifeTimePausedTag>(_entity, false);

            if (!_entityManager.HasComponent<RestartFireRequestTag>(_entity))
                _entityManager.AddComponent<RestartFireRequestTag>(_entity);
        }

        private void OnDestroy()
        {
            if (_entityManager.Exists(_entity))
            {
                if (_entityManager.HasComponent<LifeTimeViewComponent>(_entity))
                    _entityManager.RemoveComponent<LifeTimeViewComponent>(_entity);

                if (_entityManager.HasComponent<LifeTimeComponent>(_entity))
                    _entityManager.RemoveComponent<LifeTimeComponent>(_entity);

                if (_entityManager.HasComponent<LifeTimePausedTag>(_entity))
                    _entityManager.RemoveComponent<LifeTimePausedTag>(_entity);
            }
        }
    }
}
