using System;
using UnityEngine;
using UnityEngine.Events;

namespace Scripts
{
    public class Candle : MonoBehaviour
    {
        public ParticleSystem[] particles;
        public Light light;
        public AudioSource audioSource;
        public AudioClip startBurnClip;

        private bool _isActive;

        public UnityEvent onFire;

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
            
            audioSource.PlayOneShot(startBurnClip, 0.3f);
            
            onFire?.Invoke();
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