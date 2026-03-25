using System;
using UnityEngine;
using UnityEngine.Events;

namespace Scripts
{
    public class Finish : MonoBehaviour
    {
        public UnityEvent OnFinish;

        private void OnTriggerEnter(Collider other)
        {
            if (other.CompareTag("Player"))
            {
                OnFinish?.Invoke();
            }
        }
    }
}