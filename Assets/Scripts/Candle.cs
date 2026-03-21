using System;
using UnityEngine;

namespace Scripts
{
    public class Candle : MonoBehaviour
    {
        public ParticleSystem[] particles;
        public Light light;
        public AudioSource audioSource;

        private bool _isActive;

        public void FireCandle()
        {
            if(_isActive) return;
            
            foreach (var particle in particles)
            {
                particle.Play();
            }
            
            light.enabled = true;
            audioSource.Play();
            
            _isActive = true;
        }

        private void OnTriggerEnter(Collider other)
        {
            if (other.CompareTag("Player"))
            {
                FireCandle();
            }
        }
    }
}