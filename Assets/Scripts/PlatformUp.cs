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

            if (_playerController != null && delta != Vector3.zero)
                _playerController.Move(delta);
        }

        private void OnTriggerStay(Collider other)
        {
            if (other.CompareTag("Player"))
            {
                _isUp = true;

                if (_playerController == null)
                {
                    _playerController = other.GetComponent<CharacterController>();
                    _player = other.GetComponent<Player>();
                }

                if (_player != null)
                    _player.isOnPlatform = true;
            }
        }

        private void OnTriggerExit(Collider other)
        {
            if (other.CompareTag("Player"))
            {
                _isUp = false;

                if (_player != null)
                    _player.isOnPlatform = false;

                _playerController = null;
                _player = null;
            }
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
            
            _isUp = false;
        }
    }
}