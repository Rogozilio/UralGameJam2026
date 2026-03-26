using System;
using UnityEngine;
using UnityEngine.Events;

namespace Scripts
{
    public class PushObject : MonoBehaviour, IRestart
    {
        public Transform target;

        public Transform pushPoint;
        public float rangeClimb = 0.5f;
        public float pushSpeed = 1f;

        [Header("Movement Limit")] public bool useMoveLimit = true;
        public float moveLimit = 2f;

        [Header("Audio")] public AudioSource audioSource;
        public AudioClip pushLoopClip;
        public AudioClip pushEndClip;
        public float delayEndClip = 0.35f;

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
                    StopPushLoopSound();
                    PlayPushEndSound();
                    actionPushToEnd?.Invoke();
                    _player.SetIsPushAnim = false;
                    return;
                }
            }

            target.position += pushPoint.forward * pushSpeed * Time.deltaTime;

            PlayPushLoopSound();

            if (useMoveLimit)
            {
                float movedDistance = Vector3.Distance(_startTargetPosition, target.position);
                if (movedDistance > moveLimit)
                {
                    target.position = _startTargetPosition + pushPoint.forward.normalized * moveLimit;
                    _player.SetIsPushAnim = false;
                    StopPushLoopSound();
                    PlayPushEndSound();
                    actionPushToEnd?.Invoke();
                }
            }
        }

        private void LateUpdate()
        {
            if (!_isPushing)
            {
                StopPushLoopSound();
                return;
            }

            _player.SetIsPushAnim = false;

            var dot = Vector3.Dot(-_player.tempPointMove.transform.right, pushPoint.forward);

            if (dot < 0.9f)
            {
                StopPushLoopSound();
                _isBegin = true;
                return;
            }

            if (!_player.isMove)
            {
                StopPushLoopSound();
                _isBegin = true;
                return;
            }

            Vector3 targetPos = GetPointForPush(_player.transform, _isBegin);

            Vector3 delta = targetPos - _player.transform.position;
            _player.characterController.Move(delta);
            _player.render.rotation = pushPoint.rotation * Quaternion.Euler(270, 90f, 0f);
            _player.SetIsPushAnim = true;
            _player.animator.SetBool("isJump", false);
            _isBegin = false;
        }

        private void PlayPushLoopSound()
        {
            if (audioSource == null || pushLoopClip == null) return;
            if (audioSource.isPlaying && audioSource.clip == pushLoopClip) return;

            audioSource.clip = pushLoopClip;
            audioSource.loop = true;
            audioSource.Play();
        }

        private void StopPushLoopSound()
        {
            if (audioSource == null) return;
            if (audioSource.clip == pushLoopClip && audioSource.isPlaying)
                audioSource.Stop();
        }

        private void PlayPushEndSound()
        {
            if (audioSource == null || pushEndClip == null) return;

            audioSource.loop = false;
            audioSource.clip = pushEndClip;
            audioSource.PlayDelayed(delayEndClip);
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
            {
                _isPushing = false;
                StopPushLoopSound();
            }
        }

        private void OnDrawGizmos()
        {
            if (pushPoint == null) return;

            Gizmos.color = Color.green;

            Vector3 leftStart = pushPoint.TransformPoint(new Vector3(-rangeClimb, 0f, 0f));
            Vector3 rightStart = pushPoint.TransformPoint(new Vector3(rangeClimb, 0f, 0f));

            Gizmos.DrawSphere(leftStart, 0.04f);
            Gizmos.DrawSphere(rightStart, 0.04f);
            Gizmos.DrawLine(leftStart, rightStart);

            if (!useMoveLimit) return;

            Vector3 limitEnd = pushPoint.position + pushPoint.forward.normalized * moveLimit;

            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(pushPoint.position, limitEnd);
            Gizmos.DrawSphere(limitEnd, 0.06f);

#if UNITY_EDITOR
            UnityEditor.Handles.color = Color.yellow;
            UnityEditor.Handles.Label(limitEnd + Vector3.up * 0.15f, $"Limit: {moveLimit:F1}m");
#endif
        }

        public void Restart()
        {
            _isPushing = false;
            StopPushLoopSound();
            _player.SetIsPushAnim = false;
            _isBegin = true;
        }
    }
}