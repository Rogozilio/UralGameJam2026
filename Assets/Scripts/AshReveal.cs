using System.Collections;
using UnityEngine;

public class AshReveal : MonoBehaviour
{
    private Coroutine _revealCoroutine;

    public void Play(float duration)
    {
        if (_revealCoroutine != null)
            StopCoroutine(_revealCoroutine);

        _revealCoroutine = StartCoroutine(RevealCoroutine(duration));
    }

    private IEnumerator RevealCoroutine(float duration)
    {
        Vector3 targetScale = transform.localScale;
        float targetScaleZ = targetScale.z;

        Vector3 currentScale = targetScale;
        currentScale.z = 0f;
        transform.localScale = currentScale;

        float elapsed = 0f;
        float safeDuration = Mathf.Max(0.01f, duration);

        while (elapsed < safeDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / safeDuration);
            float revealT = Mathf.SmoothStep(0f, 1f, t);

            currentScale = targetScale;
            currentScale.z = Mathf.Lerp(0f, targetScaleZ, revealT);
            transform.localScale = currentScale;

            yield return null;
        }

        transform.localScale = targetScale;
        _revealCoroutine = null;
    }
}
