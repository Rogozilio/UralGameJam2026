using System.Collections;
using UnityEngine;
using UnityEngine.Events;

namespace Scripts.Cutscene
{
    public class MoveFromAToBSignal : MonoBehaviour
    {
        [Header("References")]
        public Transform actor;
        public Transform startPoint;
        public Transform endPoint;
        public Transform rotationTarget;
        public Animator animator;
        public Player playerController;

        [Header("Movement")]
        public float duration = 1f;
        public AnimationCurve moveCurve = AnimationCurve.Linear(0f, 0f, 1f, 1f);
        public bool snapToStartOnPlay = true;
        public bool rotateAlongPath = true;
        public bool rotateOnlyY = true;
        public bool useUnscaledTime = false;
        public Vector3 rotationOffsetEuler;

        [Header("Animation")]
        public string moveParameter = "move";
        public int walkValue = 1;
        public int idleValue = 0;

        [Header("Cutscene Safety")]
        public bool lockPlayerInput = true;
        public bool disableCharacterController = true;

        [Header("Events")]
        public UnityEvent onStarted;
        public UnityEvent onCompleted;

        private Coroutine _moveCoroutine;
        private bool _isPlaying;
        private bool _initialPlayerActive;
        private CharacterController _characterController;
        private bool _initialCharacterControllerEnabled;

        public void Play()
        {
            ResolveReferences();

            if (actor == null || startPoint == null || endPoint == null)
            {
                Debug.LogWarning($"[{nameof(MoveFromAToBSignal)}] Missing actor/start/end on {name}.", this);
                return;
            }

            StopInternal(restoreState: false);
            _moveCoroutine = StartCoroutine(MoveRoutine());
        }

        public void Stop()
        {
            StopInternal(restoreState: true);
        }

        public void SnapToStart()
        {
            ResolveReferences();

            if (actor == null || startPoint == null)
                return;

            ApplyPosition(startPoint.position);
            RotateToDirection(startPoint.position, endPoint != null ? endPoint.position : startPoint.position);
            SetWalkAnimation(false);
        }

        public void SnapToEnd()
        {
            ResolveReferences();

            if (actor == null || endPoint == null)
                return;

            ApplyPosition(endPoint.position);
            RotateToDirection(startPoint != null ? startPoint.position : endPoint.position, endPoint.position);
            SetWalkAnimation(false);
        }

        private IEnumerator MoveRoutine()
        {
            CacheState();
            _isPlaying = true;

            if (lockPlayerInput && playerController != null)
                playerController.IsActive = false;

            if (disableCharacterController && _characterController != null)
                _characterController.enabled = false;

            if (snapToStartOnPlay)
                ApplyPosition(startPoint.position);

            onStarted?.Invoke();

            float safeDuration = Mathf.Max(0.0001f, duration);
            float elapsed = 0f;
            Vector3 start = startPoint.position;
            Vector3 end = endPoint.position;

            while (elapsed < safeDuration)
            {
                elapsed += useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
                float normalizedTime = Mathf.Clamp01(elapsed / safeDuration);
                float curveTime = moveCurve != null ? moveCurve.Evaluate(normalizedTime) : normalizedTime;

                ApplyPosition(Vector3.LerpUnclamped(start, end, curveTime));
                RotateToDirection(start, end);

                bool isWalkingNow = (end - start).sqrMagnitude > 0.0001f && normalizedTime < 1f;
                SetWalkAnimation(isWalkingNow);

                yield return null;
            }

            ApplyPosition(end);
            RotateToDirection(start, end);
            FinishMovement(invokeCompleted: true);
        }

        private void StopInternal(bool restoreState)
        {
            if (_moveCoroutine != null)
            {
                StopCoroutine(_moveCoroutine);
                _moveCoroutine = null;
            }

            if (restoreState)
                FinishMovement(invokeCompleted: false);
            else
                _isPlaying = false;
        }

        private void FinishMovement(bool invokeCompleted)
        {
            if (!_isPlaying)
                return;

            _isPlaying = false;
            _moveCoroutine = null;
            SetWalkAnimation(false);

            if (lockPlayerInput && playerController != null)
                playerController.IsActive = _initialPlayerActive;

            if (disableCharacterController && _characterController != null)
                _characterController.enabled = _initialCharacterControllerEnabled;

            if (invokeCompleted)
                onCompleted?.Invoke();
        }

        private void ResolveReferences()
        {
            if (actor == null)
                actor = transform;

            if (playerController == null && actor != null)
                playerController = actor.GetComponent<Player>();

            if (animator == null)
            {
                if (playerController != null && playerController.animator != null)
                    animator = playerController.animator;
                else if (actor != null)
                    animator = actor.GetComponentInChildren<Animator>();
            }
        }

        private void CacheState()
        {
            _initialPlayerActive = playerController != null && playerController.IsActive;

            _characterController = actor != null ? actor.GetComponent<CharacterController>() : null;
            _initialCharacterControllerEnabled = _characterController != null && _characterController.enabled;
        }

        private void ApplyPosition(Vector3 position)
        {
            if (actor == null)
                return;

            actor.position = position;
            SyncPlayerHelpers();
        }

        private void RotateToDirection(Vector3 start, Vector3 end)
        {
            if (!rotateAlongPath)
                return;

            Transform target = GetRotationTarget();
            if (target == null)
                return;

            Vector3 direction = end - start;
            if (rotateOnlyY)
                direction.y = 0f;

            if (direction.sqrMagnitude <= 0.0001f)
                return;

            Vector3 normalizedDirection = direction.normalized;
            Quaternion lookRotation = Quaternion.LookRotation(normalizedDirection, Vector3.up);
            Quaternion targetRotation = GetTargetRotation(target, normalizedDirection, lookRotation);
            target.rotation = targetRotation;

            if (playerController != null && playerController.tempPointMove != null)
                playerController.tempPointMove.rotation = targetRotation;
        }

        private Transform GetRotationTarget()
        {
            if (rotationTarget != null)
                return rotationTarget;

            if (playerController != null && playerController.render != null)
                return playerController.render;

            return actor;
        }

        private Quaternion GetTargetRotation(Transform target, Vector3 direction, Quaternion lookRotation)
        {
            if (playerController != null && target == playerController.render)
            {
                Vector3 renderForward = Vector3.Cross(direction, Vector3.up);
                if (renderForward.sqrMagnitude > 0.0001f)
                    return Quaternion.LookRotation(renderForward.normalized, Vector3.up);
            }

            return lookRotation * Quaternion.Euler(rotationOffsetEuler);
        }

        private void SyncPlayerHelpers()
        {
            if (playerController == null || playerController.tempPointMove == null || actor == null)
                return;

            playerController.tempPointMove.position = actor.position;
        }

        private void SetWalkAnimation(bool isWalking)
        {
            if (animator == null || string.IsNullOrWhiteSpace(moveParameter))
                return;

            animator.SetInteger(moveParameter, isWalking ? walkValue : idleValue);
        }
    }
}
