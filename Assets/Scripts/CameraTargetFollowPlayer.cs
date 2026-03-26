using System;
using UnityEngine;

namespace Scripts
{
    public class CameraTargetFollowPlayer : MonoBehaviour, IRestart
    {
        public Transform target;
        public bool isMoveTargetCamera;

        private Vector3 _originPosition;
        private void Awake()
        {
            _originPosition = target.position;
        }

        private void OnTriggerStay(Collider other)
        {
            if(!isMoveTargetCamera) return;
                
            if (other.CompareTag("Player"))
            {
                other.GetComponent<Player>().IsStaticCamera = true;
                var newPosition = target.position;
                newPosition.y = other.transform.position.y;
                newPosition.z = other.transform.position.z;
                target.position = newPosition;
            }
        }

        private void OnTriggerExit(Collider other)
        {
            if (other.CompareTag("Player"))
            {
                target.position = _originPosition;
                other.GetComponent<Player>().IsStaticCamera = false;
            }
        }

        public void Restart()
        {
            target.position = _originPosition;
        }
    }
}