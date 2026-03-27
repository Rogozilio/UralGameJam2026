using System;
using System.Collections;
using TMPro;
using UnityEngine;

namespace Scripts
{
    public class LifeTime : MonoBehaviour
    {
        public float time;
        public TextMeshProUGUI text;
        public BlendShapeController shapeController;

        public event Action OnLifeTimeEnded;

        public bool isFastTime;

        private Coroutine _lifeCoroutine;
        private float _remainingTime;
        private bool _isPaused;

        public void StartLifeTimer()
        {
            shapeController.fire.Stop();
            shapeController.fire.Clear();
            shapeController.fire.Play();
            _remainingTime = time;
            _lifeCoroutine = StartCoroutine(LifeTimerCoroutine());
        }

        public void StopLifeTimer()
        {
            if (_lifeCoroutine != null)
            {
                StopCoroutine(_lifeCoroutine);
                _lifeCoroutine = null;
            }
            
            _isPaused = false;
        }

        public void RestartLifeTimer()
        {
            StopLifeTimer();
            StartLifeTimer();
        }

        public void PauseLifeTimer()
        {
            if (_lifeCoroutine != null)
                _isPaused = true;
        }

        public void ResumeLifeTimer()
        {
            if (_lifeCoroutine != null)
                _isPaused = false;
        }

        private IEnumerator LifeTimerCoroutine()
        {
            while (_remainingTime > 0f)
            {
                text.text = _remainingTime.ToString("00");

                shapeController.blendValue = 1f - _remainingTime / time;

                if (!_isPaused)
                {
                    var multiply = isFastTime ? 40f : 1f;
                    _remainingTime -= Time.deltaTime * multiply;
                }
                    
                
                yield return null;
            }

            text.text = "0";
            _lifeCoroutine = null;
            OnLifeTimeEnded?.Invoke();
        }
    }
}