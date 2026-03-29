using System.Collections;
using UnityEngine;
using UnityEngine.Events;

public class EnableTimerEventInvoker : MonoBehaviour
{
    [Min(0f)]
    [SerializeField] private float delay = 1f;

    [SerializeField] private UnityEvent onEnableEvent;
    [SerializeField] private UnityEvent onTimerFinishedEvent;

    private Coroutine _timerCoroutine;

    private void OnEnable()
    {
        onEnableEvent?.Invoke();

        if (_timerCoroutine != null)
        {
            StopCoroutine(_timerCoroutine);
        }

        _timerCoroutine = StartCoroutine(TimerCoroutine());
    }

    private void OnDisable()
    {
        if (_timerCoroutine == null)
        {
            return;
        }

        StopCoroutine(_timerCoroutine);
        _timerCoroutine = null;
    }

    private IEnumerator TimerCoroutine()
    {
        if (delay > 0f)
        {
            yield return new WaitForSeconds(delay);
        }

        onTimerFinishedEvent?.Invoke();
        _timerCoroutine = null;
    }
}
