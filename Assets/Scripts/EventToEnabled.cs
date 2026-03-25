using System;
using UnityEngine;
using UnityEngine.Events;

namespace Scripts
{
    public class EventToEnabled : MonoBehaviour
    {
        public bool isEnterTrigger;
        public UnityEvent action;

        private void OnEnable()
        {
            action?.Invoke();
        }

        private void OnTriggerEnter(Collider other)
        {
            if(!isEnterTrigger) return;
            
            if (other.CompareTag("Player"))
            {
                OnEnable();
            }
        }
    }
}