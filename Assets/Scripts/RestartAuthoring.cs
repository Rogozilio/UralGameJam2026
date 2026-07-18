using UralGameJam.Ecs.Restart;
using UnityEngine;

namespace Scripts
{
    [DisallowMultipleComponent]
    public sealed class RestartAuthoring : MonoBehaviour, IRestart
    {
        public bool restartEnabled;
        public bool restartPosition;
        public bool restartRotation;
        public bool restartScale;
        public bool restartPhysicsVelocity;

        [SerializeField] private Transform target;

        private Transform _target;
        private Rigidbody _rigidbody;
        private bool _initialEnabled;
        private Vector3 _initialPosition;
        private Quaternion _initialRotation;
        private Vector3 _initialScale;
        private Vector3 _initialVelocity;
        private Vector3 _initialAngularVelocity;

        private void Awake()
        {
            RefreshRestartData();
        }

        public void RefreshRestartData()
        {
            _target = target != null ? target : transform;
            _rigidbody = _target.GetComponent<Rigidbody>();
            _initialEnabled = gameObject.activeSelf;
            _initialPosition = _target.position;
            _initialRotation = _target.rotation;
            _initialScale = _target.localScale;

            if (_rigidbody != null)
            {
                _initialVelocity = _rigidbody.linearVelocity;
                _initialAngularVelocity = _rigidbody.angularVelocity;
            }
        }

        public void Restart()
        {
            if (_target == null)
                return;

            if (restartPosition || restartRotation)
            {
                if (_rigidbody != null)
                {
                    if (restartPosition)
                        _rigidbody.position = _initialPosition;

                    if (restartRotation)
                        _rigidbody.rotation = _initialRotation;
                }
                else
                {
                    if (restartPosition)
                        _target.position = _initialPosition;

                    if (restartRotation)
                        _target.rotation = _initialRotation;
                }
            }

            if (restartScale)
                _target.localScale = _initialScale;

            if (restartPhysicsVelocity && _rigidbody != null)
            {
                _rigidbody.linearVelocity = _initialVelocity;
                _rigidbody.angularVelocity = _initialAngularVelocity;
                _rigidbody.WakeUp();
            }

            if (restartEnabled)
                gameObject.SetActive(_initialEnabled);
        }
    }
}
