using System;
using UnityEngine;
using UnityEngine.Events;

namespace Scripts
{
    public class EventToEnabled : MonoBehaviour
    {
        public UnityEvent action;

        private void OnEnable()
        {
            action?.Invoke();
        }
    }
}