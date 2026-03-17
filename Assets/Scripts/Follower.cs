using UnityEngine;

namespace Scripts
{
    public class Follower : MonoBehaviour
    {
        public Transform target;

        public bool lockPosition;
        public bool lockPositionX;
        public bool lockPositionY;
        public bool lockPositionZ;
        [Space] 
        public bool lockRotation;
        public bool lockRotationX;
        public bool lockRotationY;
        public bool lockRotationZ;

        void Update()
        {
            if (target == null) return;

            transform.position = target.position;

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
    }
}