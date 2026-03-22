using System;
using UnityEngine;
using UnityEngine.Events;

namespace Scripts
{
    public class PushObject : MonoBehaviour
    {
        public Transform target;

        public Transform pushPoint;
        public float rangeClimb = 0.5f;
        public float pushSpeed = 1f;

        [Header("Movement Limit")]
        public bool useMoveLimit = true;
        public float moveLimit = 2f;

        [Space] public UnityEvent actionPushToEnd;

        private bool _isPushing;

        private Player _player;
        private bool _isBegin = true;
        private float _posX;

        private Vector3 _startTargetPosition;

        private void Start()
        {
            _player = FindObjectOfType<Player>();
            _startTargetPosition = target.position;
        }

        private void Update()
        {
            if (!_isPushing) return;

            var dot = Vector3.Dot(-_player.tempPointMove.transform.right, pushPoint.forward);

            if (dot < 0.9f) return;

            if (!_player.isMove) return;

            if (useMoveLimit)
            {
                float movedDistance = Vector3.Distance(_startTargetPosition, target.position);
                if (movedDistance >= moveLimit)
                {
                    actionPushToEnd?.Invoke();
                    _player.SetIsPushAnim = false;
                    return;
                }
            }

            target.position += pushPoint.forward * pushSpeed * Time.deltaTime;

            if (useMoveLimit)
            {
                float movedDistance = Vector3.Distance(_startTargetPosition, target.position);
                if (movedDistance > moveLimit)
                {
                    target.position = _startTargetPosition + pushPoint.forward.normalized * moveLimit;
                    _player.SetIsPushAnim = false;
                    actionPushToEnd?.Invoke();
                }
            }
        }

        private void LateUpdate()
        {
            if (!_isPushing) return;

            _player.SetIsPushAnim = false;

            if (!_isPushing)
            {
                _isBegin = true;
                return;
            }

            var dot = Vector3.Dot(-_player.tempPointMove.transform.right, pushPoint.forward);

            if (dot < 0.9f)
            {
                _isBegin = true;
                return;
            }

            if (!_player.isMove)
            {
                _isBegin = true;
                return;
            }

            _player.transform.position = GetPointForPush(_player.transform, _isBegin);
            _player.render.rotation = pushPoint.rotation * Quaternion.Euler(270, 90f, 0f);
            _player.SetIsPushAnim = true;
            _isBegin = false;
        }

        public Vector3 GetPointForPush(Transform player, bool isBegin)
        {
            var localPlayerPoint = pushPoint.InverseTransformPoint(player.position);

            if (isBegin)
                _posX = Math.Clamp(localPlayerPoint.x, pushPoint.localPosition.x - rangeClimb,
                    pushPoint.localPosition.x + rangeClimb);

            return pushPoint.TransformPoint(new Vector3(_posX, 0f, 0f));
        }

        private void OnTriggerStay(Collider other)
        {
            if (other.CompareTag("Player"))
                _isPushing = true;
        }

        private void OnTriggerExit(Collider other)
        {
            if (other.CompareTag("Player"))
                _isPushing = false;
        }

        private void OnDrawGizmos()
        {
            if (pushPoint == null) return;

            // --- Зона захвата игрока (зелёная) ---
            Gizmos.color = Color.green;

            Vector3 leftStart = pushPoint.TransformPoint(new Vector3(-rangeClimb, 0f, 0f));
            Vector3 rightStart = pushPoint.TransformPoint(new Vector3(rangeClimb, 0f, 0f));

            Gizmos.DrawSphere(leftStart, 0.04f);
            Gizmos.DrawSphere(rightStart, 0.04f);
            Gizmos.DrawLine(leftStart, rightStart);

            if (!useMoveLimit) return;

            // --- Предел движения (жёлтая линия и точка конца) ---
            Vector3 limitEnd = pushPoint.position + pushPoint.forward.normalized * moveLimit;

            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(pushPoint.position, limitEnd);
            Gizmos.DrawSphere(limitEnd, 0.06f);

#if UNITY_EDITOR
            // Подпись с дистанцией
            UnityEditor.Handles.color = Color.yellow;
            UnityEditor.Handles.Label(limitEnd + Vector3.up * 0.15f, $"Limit: {moveLimit:F1}m");
#endif
        }
    }
}