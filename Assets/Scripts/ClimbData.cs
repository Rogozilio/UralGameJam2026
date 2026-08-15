using UralGameJam.Ecs.Player;
using UnityEngine;

namespace Scripts
{
    public class ClimbData : MonoEntity
    {
        public Transform startClimb;
        public Transform finishClimb;
        public float rangeClimb = 0.5f;
        
        protected override void Awake()
        {
            if (startClimb == null)
                throw new MissingReferenceException(
                    $"{nameof(ClimbData)}.{nameof(startClimb)} is not assigned on {name}");

            if (finishClimb == null)
                throw new MissingReferenceException(
                    $"{nameof(ClimbData)}.{nameof(finishClimb)} is not assigned on {name}");

            base.Awake();

            _entityManager.AddComponent<ClimbTag>(_entity);
            _entityManager.AddComponentData(_entity, new ClimbComponent());
            UpdateClimbComponent();
        }

        private void OnDestroy()
        {
            if (!_entityManager.Exists(_entity))
                return;

            _entityManager.RemoveComponent<ClimbComponent>(_entity);
            _entityManager.RemoveComponent<ClimbTag>(_entity);
        }

        private void LateUpdate()
        {
            //TODO: работает в динамике (наверное), но через update, что зря
            UpdateClimbComponent();
        }

        private void UpdateClimbComponent()
        {
            _entityManager.SetComponentData(_entity, new ClimbComponent
            {
                startPosition = startClimb.position,
                startRight = startClimb.right,
                finishPosition = finishClimb.position,
                finishRight = finishClimb.right,
                startRotation = startClimb.rotation,
                forward = transform.forward,
                position = transform.position,
                range = rangeClimb
            });
        }
        
        private void OnDrawGizmos()
        {
            if (startClimb == null || finishClimb == null) return;

            Gizmos.color = Color.green;
    
            Vector3 leftStart  = startClimb.TransformPoint(new Vector3(-rangeClimb, 0f, 0f));
            Vector3 rightStart = startClimb.TransformPoint(new Vector3( rangeClimb, 0f, 0f));
    
            Gizmos.DrawSphere(leftStart,  0.1f);
            Gizmos.DrawSphere(rightStart, 0.1f);
            Gizmos.DrawLine(leftStart, rightStart);
    
            Gizmos.color = Color.blue;
    
            Vector3 leftFinish  = finishClimb.TransformPoint(new Vector3(-rangeClimb, 0f, 0f));
            Vector3 rightFinish = finishClimb.TransformPoint(new Vector3( rangeClimb, 0f, 0f));
    
            Gizmos.DrawSphere(leftFinish,  0.1f);
            Gizmos.DrawSphere(rightFinish, 0.1f);
            Gizmos.DrawLine(leftFinish, rightFinish);
        }
    }
}
