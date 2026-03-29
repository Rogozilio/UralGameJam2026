using System.Collections.Generic;
using Unity.Cinemachine;
using UnityEngine;

namespace Scripts
{
    public class Follower : MonoBehaviour
    {
        public Transform target;
        public bool smoothOnLargeDistance = true;
        public float smoothDistanceThreshold = 3f;
        public float smoothTime = 0.35f;

        public bool lockPosition;
        public bool lockPositionX;
        public bool lockPositionY;
        public bool lockPositionZ;
        [Space] 
        public bool lockRotation;
        public bool lockRotationX;
        public bool lockRotationY;
        public bool lockRotationZ;

        private Vector3 _smoothVelocity;
        private bool _isInitialized;
        private bool _isSmoothing;
        private readonly List<CinemachineThirdPersonFollow> _thirdPersonFollowers = new();
        private readonly List<bool> _avoidObstacleStates = new();

        private void Awake()
        {
            CacheThirdPersonFollowers();
        }

        private void OnDisable()
        {
            RestoreObstacleAvoidance();
        }

        private void LateUpdate()
        {
            if (target == null) return;

            var targetPosition = target.position;

            if (!_isInitialized)
            {
                transform.position = targetPosition;
                _isInitialized = true;
            }

            if (smoothOnLargeDistance)
            {
                var distanceToTarget = Vector3.Distance(transform.position, targetPosition);
                if (!_isSmoothing && distanceToTarget >= smoothDistanceThreshold)
                    BeginSmoothing();

                if (_isSmoothing)
                {
                    transform.position = Vector3.SmoothDamp(transform.position, targetPosition, ref _smoothVelocity, smoothTime);

                    if (Vector3.Distance(transform.position, targetPosition) <= 0.05f)
                    {
                        transform.position = targetPosition;
                        _smoothVelocity = Vector3.zero;
                        EndSmoothing();
                    }
                }
                else
                {
                    transform.position = targetPosition;
                }
            }
            else
            {
                transform.position = targetPosition;
            }

            if (!lockPosition)
            {
                var newPosition = transform.position;
                newPosition.x *= lockPositionX ? 0 : 1f;
                newPosition.y *= lockPositionY ? 0 : 1f;
                newPosition.z *= lockPositionZ ? 0 : 1f;
                transform.position = newPosition;
            }
            
            if (!lockRotation)
            {
                var newRotation = target.rotation;
                newRotation.x *= lockRotationX ? 0 : 1f;
                newRotation.y *= lockRotationY ? 0 : 1f;
                newRotation.z *= lockRotationZ ? 0 : 1f;
                transform.rotation = newRotation;
            }
        }

        private void BeginSmoothing()
        {
            _isSmoothing = true;
            SetObstacleAvoidanceEnabled(false);
        }

        private void EndSmoothing()
        {
            _isSmoothing = false;
            RestoreObstacleAvoidance();
        }

        private void CacheThirdPersonFollowers()
        {
            _thirdPersonFollowers.Clear();
            _avoidObstacleStates.Clear();

            var cameras = FindObjectsOfType<CinemachineCamera>(true);
            foreach (var camera in cameras)
            {
                if (camera == null || camera.Follow != transform)
                    continue;

                var thirdPersonFollow = camera.GetComponent<CinemachineThirdPersonFollow>();
                if (thirdPersonFollow == null)
                    continue;

                _thirdPersonFollowers.Add(thirdPersonFollow);
                _avoidObstacleStates.Add(thirdPersonFollow.AvoidObstacles.Enabled);
            }
        }

        private void SetObstacleAvoidanceEnabled(bool isEnabled)
        {
            if (_thirdPersonFollowers.Count == 0)
                CacheThirdPersonFollowers();

            for (var i = 0; i < _thirdPersonFollowers.Count; i++)
            {
                var thirdPersonFollow = _thirdPersonFollowers[i];
                if (thirdPersonFollow == null)
                    continue;

                var settings = thirdPersonFollow.AvoidObstacles;
                settings.Enabled = isEnabled;
                thirdPersonFollow.AvoidObstacles = settings;
            }
        }

        private void RestoreObstacleAvoidance()
        {
            for (var i = 0; i < _thirdPersonFollowers.Count; i++)
            {
                var thirdPersonFollow = _thirdPersonFollowers[i];
                if (thirdPersonFollow == null)
                    continue;

                var settings = thirdPersonFollow.AvoidObstacles;
                settings.Enabled = _avoidObstacleStates[i];
                thirdPersonFollow.AvoidObstacles = settings;
            }
        }
    }
}
