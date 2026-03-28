using System.Collections;
using UnityEngine;

namespace Scripts
{
    public class PlatformUp : MonoBehaviour, IRestart
    {
         public Transform target;
        public float speed = 0.5f;

        [Header("Y Limits")]
        public float minY = 0f;
        public float maxY = 5f;

        private bool _isUp;
        private CharacterController _playerController;
        private Vector3 _lastPosition;
        private Player _player;

        public bool isCancelRestart;

        public AudioClip clip;
        public AudioSource audioSource;
        public float fadeDuration = 0.5f;
        private Coroutine _fadeCoroutine;

        public bool IsUp
        {
            set =>  _isUp = value;
        }
        
        public float NormalizedHeight
        {
            get
            {
                return Mathf.InverseLerp(minY, maxY, target.position.y);
            }
        }

        private void Awake()
        {
            _lastPosition = target.position;
        }

        private void Update()
        {
            if (_playerController != null && (_player == null || !_playerController.enabled || _player.isDeath))
                DetachPlayer();

            if (_isUp)
            {
                if (target.position.y < maxY)
                    target.position += Vector3.up * speed * Time.deltaTime;
            }
            else
            {
                if (target.position.y > minY)
                    target.position -= Vector3.up * speed * 2 * Time.deltaTime;
            }

            // Clamp позиции в пределах minY/maxY
            target.position = new Vector3(
                target.position.x,
                Mathf.Clamp(target.position.y, minY, maxY),
                target.position.z
            );

            Vector3 delta = target.position - _lastPosition;
            _lastPosition = target.position;

            if (_playerController != null && _playerController.enabled && delta != Vector3.zero)
                _playerController.Move(delta);
        }

        private void OnTriggerStay(Collider other)
        {
            if (other.CompareTag("Player"))
            {
                var player = other.GetComponent<Player>();
                var controller = other.GetComponent<CharacterController>();

                if (player == null || controller == null || !controller.enabled || player.isDeath)
                {
                    DetachPlayer();
                    return;
                }

                _isUp = true;

                if (_playerController == null)
                {
                    _playerController = controller;
                    _player = player;
                    _player.onStartDeath.AddListener(DetachPlayer);
                }

                if (_player != null)
                    _player.isOnPlatform = true;
                
                audioSource.clip = clip;
                audioSource.loop = true;
                if (!audioSource.isPlaying)
                    audioSource.Play();
            }
        }

        private void OnTriggerExit(Collider other)
        {
            if (other.CompareTag("Player"))
                DetachPlayer();
        }

        private void OnDestroy()
        {
            DetachPlayer();
        }
        
        private void StopSound()
        {
            if (audioSource != null && audioSource.isPlaying)
            {
                if (_fadeCoroutine != null)
                    StopCoroutine(_fadeCoroutine);

                _fadeCoroutine = StartCoroutine(FadeOut());
            }
        }

        private IEnumerator FadeOut()
        {
            float startVolume = audioSource.volume;
            float elapsed = 0f;

            while (elapsed < fadeDuration)
            {
                elapsed += Time.deltaTime;
                audioSource.volume = Mathf.Lerp(startVolume, 0f, elapsed / fadeDuration);
                yield return null;
            }

            audioSource.Stop();
            audioSource.volume = startVolume; // восстанавливаем для следующего раза
            _fadeCoroutine = null;
        }

        private void OnDrawGizmosSelected()
        {
            Vector3 center = target != null ? target.position : transform.position;

            // Линия minY
            Gizmos.color = Color.green;
            Vector3 minPos = new Vector3(center.x, minY, center.z);
            Gizmos.DrawLine(minPos + Vector3.left, minPos + Vector3.right);
            Gizmos.DrawLine(minPos + Vector3.back, minPos + Vector3.forward);
            Gizmos.DrawSphere(minPos, 0.1f);

            // Линия maxY
            Gizmos.color = Color.red;
            Vector3 maxPos = new Vector3(center.x, maxY, center.z);
            Gizmos.DrawLine(maxPos + Vector3.left, maxPos + Vector3.right);
            Gizmos.DrawLine(maxPos + Vector3.back, maxPos + Vector3.forward);
            Gizmos.DrawSphere(maxPos, 0.1f);

            // Вертикальная линия между min и max
            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(minPos, maxPos);
        }

        public void Restart()
        {
            if(isCancelRestart)  return;
            
            DetachPlayer();
        }

        private void DetachPlayer()
        {
            _isUp = false;

            if (_player != null)
            {
                _player.isOnPlatform = false;
                _player.onStartDeath.RemoveListener(DetachPlayer);
            }

            _playerController = null;
            _player = null;

            StopSound();
        }
    }
}
