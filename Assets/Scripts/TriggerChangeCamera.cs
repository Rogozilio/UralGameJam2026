using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.Events;

namespace Scripts
{
    public class TriggerChangeCamera : MonoBehaviour
    {
        public UnityEvent onChangeCamera;
        
        private void OnTriggerEnter(Collider other)
        {
            if (other.CompareTag("Player"))
            {
                onChangeCamera?.Invoke();
                other.GetComponent<Player>().isStaticCamera = true;
            }
        }
    }
}