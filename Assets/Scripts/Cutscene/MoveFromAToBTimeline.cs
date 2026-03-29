using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

namespace Scripts.Cutscene
{
    [TrackColor(0.27f, 0.65f, 0.91f)]
    [TrackClipType(typeof(MoveFromAToBClip))]
    public class MoveFromAToBTrack : TrackAsset
    {
    }

    [System.Serializable]
    public class MoveFromAToBClip : PlayableAsset, ITimelineClipAsset
    {
        [Header("References")]
        public ExposedReference<Transform> actor;
        public ExposedReference<Transform> startPoint;
        public ExposedReference<Transform> endPoint;
        public ExposedReference<Transform> rotationTarget;
        public ExposedReference<Animator> animator;
        public ExposedReference<Player> playerController;

        [Header("Movement")]
        public bool rotateAlongPath = true;
        public bool rotateOnlyY = true;
        public Vector3 rotationOffsetEuler;

        [Header("Animation")]
        public string moveParameter = "move";
        public int walkValue = 1;
        public int idleValue = 0;

        [Header("Cutscene Safety")]
        public bool lockPlayerInput = true;
        public bool disableCharacterController = true;

        public ClipCaps clipCaps => ClipCaps.None;

        public override Playable CreatePlayable(PlayableGraph graph, GameObject owner)
        {
            var playable = ScriptPlayable<MoveFromAToBBehaviour>.Create(graph);
            var behaviour = playable.GetBehaviour();
            var resolver = graph.GetResolver();

            behaviour.actor = actor.Resolve(resolver);
            behaviour.startPoint = startPoint.Resolve(resolver);
            behaviour.endPoint = endPoint.Resolve(resolver);
            behaviour.rotationTarget = rotationTarget.Resolve(resolver);
            behaviour.animator = animator.Resolve(resolver);
            behaviour.playerController = playerController.Resolve(resolver);

            behaviour.rotateAlongPath = rotateAlongPath;
            behaviour.rotateOnlyY = rotateOnlyY;
            behaviour.rotationOffsetEuler = rotationOffsetEuler;

            behaviour.moveParameter = moveParameter;
            behaviour.walkValue = walkValue;
            behaviour.idleValue = idleValue;

            behaviour.lockPlayerInput = lockPlayerInput;
            behaviour.disableCharacterController = disableCharacterController;

            return playable;
        }
    }

    public class MoveFromAToBBehaviour : PlayableBehaviour
    {
        [HideInInspector] public Transform actor;
        [HideInInspector] public Transform startPoint;
        [HideInInspector] public Transform endPoint;
        [HideInInspector] public Transform rotationTarget;
        [HideInInspector] public Animator animator;
        [HideInInspector] public Player playerController;

        [HideInInspector] public bool rotateAlongPath;
        [HideInInspector] public bool rotateOnlyY;
        [HideInInspector] public Vector3 rotationOffsetEuler;

        [HideInInspector] public string moveParameter;
        [HideInInspector] public int walkValue;
        [HideInInspector] public int idleValue;

        [HideInInspector] public bool lockPlayerInput;
        [HideInInspector] public bool disableCharacterController;

        private bool _isPlaying;
        private bool _initialPlayerActive;
        private CharacterController _characterController;
        private bool _initialCharacterControllerEnabled;

        public override void OnBehaviourPlay(Playable playable, FrameData info)
        {
            CacheState();
            _isPlaying = true;

            if (lockPlayerInput && playerController != null)
                playerController.IsActive = false;

            if (disableCharacterController && _characterController != null)
                _characterController.enabled = false;

            SetWalkAnimation(true);
        }

        public override void ProcessFrame(Playable playable, FrameData info, object playerData)
        {
            if (actor == null || startPoint == null || endPoint == null)
                return;

            var duration = playable.GetDuration();
            var t = duration <= 0.0001d
                ? 1f
                : Mathf.Clamp01((float)(playable.GetTime() / duration));

            var start = startPoint.position;
            var end = endPoint.position;
            actor.position = Vector3.Lerp(start, end, t);

            if (rotateAlongPath)
                RotateToDirection(start, end);

            var hasDistance = (end - start).sqrMagnitude > 0.0001f;
            var isWalkingNow = hasDistance && t > 0f && t < 1f;
            SetWalkAnimation(isWalkingNow);
        }

        public override void OnBehaviourPause(Playable playable, FrameData info)
        {
            if (!_isPlaying)
                return;

            _isPlaying = false;
            SetWalkAnimation(false);

            if (lockPlayerInput && playerController != null)
                playerController.IsActive = _initialPlayerActive;

            if (disableCharacterController && _characterController != null)
                _characterController.enabled = _initialCharacterControllerEnabled;
        }

        private void CacheState()
        {
            if (playerController != null)
                _initialPlayerActive = playerController.IsActive;
            else
                _initialPlayerActive = false;

            _characterController = null;
            _initialCharacterControllerEnabled = false;
            if (actor != null)
                _characterController = actor.GetComponent<CharacterController>();

            if (_characterController != null)
                _initialCharacterControllerEnabled = _characterController.enabled;
        }

        private void RotateToDirection(Vector3 start, Vector3 end)
        {
            var target = rotationTarget != null ? rotationTarget : actor;
            if (target == null)
                return;

            var direction = end - start;
            if (rotateOnlyY)
                direction.y = 0f;

            if (direction.sqrMagnitude <= 0.0001f)
                return;

            var lookRotation = Quaternion.LookRotation(direction.normalized, Vector3.up);
            target.rotation = lookRotation * Quaternion.Euler(rotationOffsetEuler);
        }

        private void SetWalkAnimation(bool isWalking)
        {
            if (animator == null || string.IsNullOrWhiteSpace(moveParameter))
                return;

            animator.SetInteger(moveParameter, isWalking ? walkValue : idleValue);
        }
    }
}
