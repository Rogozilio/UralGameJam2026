using System;
using UnityEngine;

namespace Scripts
{
    public class PushObject : MonoBehaviour
    {
        public Transform target;

        public Transform pushPoint;
        public float rangeClimb = 0.5f;
        public float pushSpeed = 1f;

        private bool _isPushing;

        private Player _player;
        private bool _isBegin = true;
        private float _posX;

        private void Start()
        {
            _player = FindObjectOfType<Player>();
        }

        private void Update()
        {
            if (!_isPushing) return;

            var dot = Vector3.Dot(-_player.tempPointMove.transform.right, pushPoint.forward);

            if (dot < 0.9f) return;

            if (!_player.isMove) return;

            target.position += pushPoint.forward * pushSpeed * Time.deltaTime;
        }

        private void LateUpdate()
        {
            if(!_isPushing) return;
            
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
            _player.render.rotation = pushPoint.rotation * Quaternion.Euler(270, 90f, 0f);;
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

            Gizmos.color = Color.green;

            Vector3 leftStart = pushPoint.TransformPoint(new Vector3(-rangeClimb, 0f, 0f));
            Vector3 rightStart = pushPoint.TransformPoint(new Vector3(rangeClimb, 0f, 0f));

            Gizmos.DrawSphere(leftStart, 0.01f);
            Gizmos.DrawSphere(rightStart, 0.01f);
            Gizmos.DrawLine(leftStart, rightStart);
        }
    }
}