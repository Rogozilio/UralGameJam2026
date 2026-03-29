using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

namespace Scripts.Cutscene
{
    // Добавляем привязку к Transform, чтобы не таскать Actor в каждый клип
    [TrackColor(0.27f, 0.65f, 0.91f)]
    [TrackClipType(typeof(MoveFromAToBClip))]
    [TrackBindingType(typeof(Transform))] 
    public class MoveFromAToBTrack : TrackAsset
    {
    }

    [System.Serializable]
    public class MoveFromAToBClip : PlayableAsset, ITimelineClipAsset
    {
        // Actor теперь берется из трека, тут оставляем только точки
        public ExposedReference<Transform> startPoint;
        public ExposedReference<Transform> endPoint;
        public ExposedReference<Transform> rotationTarget;
        public ExposedReference<Animator> animator;
        // Будь осторожен с кастомными классами типа Player в билде
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

            // Передаем данные
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
        public Transform startPoint;
        public Transform endPoint;
        public Transform rotationTarget;
        public Animator animator;
        public Player playerController;

        public bool rotateAlongPath;
        public bool rotateOnlyY;
        public Vector3 rotationOffsetEuler;
        public string moveParameter;
        public int walkValue;
        public int idleValue;
        public bool lockPlayerInput;
        public bool disableCharacterController;

        private bool _isPlaying;
        private bool _initialPlayerActive;
        private CharacterController _characterController;
        private bool _initialCharacterControllerEnabled;

        public override void OnBehaviourPlay(Playable playable, FrameData info)
        {
            _isPlaying = true;
            // ВАЖНО: В этот момент ProcessFrame еще не вызывался, 
            // поэтому ссылки на объекты берем аккуратно.
        }

        public override void ProcessFrame(Playable playable, FrameData info, object playerData)
        {
            // Получаем Actor напрямую из Binding трека
            Transform actor = playerData as Transform;

            if (actor == null || startPoint == null || endPoint == null)
                return;

            // Кэшируем компоненты один раз при старте воспроизведения
            if (_characterController == null && disableCharacterController)
            {
                _characterController = actor.GetComponent<CharacterController>();
                if (_characterController != null)
                {
                    _initialCharacterControllerEnabled = _characterController.enabled;
                    _characterController.enabled = false;
                }
            }

            if (lockPlayerInput && playerController != null && playerController.IsActive)
            {
                _initialPlayerActive = true;
                playerController.IsActive = false;
            }

            float duration = (float)playable.GetDuration();
            float time = (float)playable.GetTime();
            float t = Mathf.Clamp01(time / duration);

            actor.position = Vector3.Lerp(startPoint.position, endPoint.position, t);

            if (rotateAlongPath)
            {
                RotateToDirection(actor, startPoint.position, endPoint.position);
            }

            SetWalkAnimation(t > 0 && t < 1);
        }

        public override void OnBehaviourPause(Playable playable, FrameData info)
        {
            if (!_isPlaying) return;
            
            _isPlaying = false;
            SetWalkAnimation(false);

            // Возвращаем состояния
            if (playerController != null)
                playerController.IsActive = _initialPlayerActive;

            if (_characterController != null)
                _characterController.enabled = _initialCharacterControllerEnabled;
        }

        private void RotateToDirection(Transform actor, Vector3 start, Vector3 end)
        {
            Transform target = rotationTarget != null ? rotationTarget : actor;
            Vector3 direction = end - start;
            if (rotateOnlyY) direction.y = 0;

            if (direction.sqrMagnitude > 0.001f)
            {
                Quaternion lookRotation = Quaternion.LookRotation(direction.normalized, Vector3.up);
                target.rotation = lookRotation * Quaternion.Euler(rotationOffsetEuler);
            }
        }

        private void SetWalkAnimation(bool isWalking)
        {
            if (animator == null || string.IsNullOrEmpty(moveParameter)) return;
            animator.SetInteger(moveParameter, isWalking ? walkValue : idleValue);
        }
    }
}