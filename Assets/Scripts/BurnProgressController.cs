using UnityEngine;
using UnityEngine.Events;

public class BurnProgressController : MonoBehaviour
{
    [Header("Material Settings")]
    public Material burnMaterial;

    [Header("Burn Settings")]
    [Range(0f, 1f)]
    public float burnProgress = 0f;

    [Header("Animation")]
    public float burnDuration = 2f;
    public bool playOnStart = false;

    [Header("Events")]
    public UnityEvent onBurnComplete;
    public UnityEvent onReverseComplete;

    public AudioClip clip;
    public AudioSource audioSource;

    private static readonly string BurnProgressProperty = "_BurnProgress";
    private float previousBurnProgress = -1f;
    private Coroutine burnCoroutine;

    void Start()
    {
        if (burnMaterial == null)
        {
            Renderer rend = GetComponent<Renderer>();
            if (rend != null)
                burnMaterial = rend.material;
        }

        SetBurnProgress(burnProgress);

        if (playOnStart)
            PlayBurn();
    }

    void Update()
    {
        if (!Mathf.Approximately(burnProgress, previousBurnProgress))
        {
            SetBurnProgress(burnProgress);
            previousBurnProgress = burnProgress;
        }
    }

    public void SetBurnProgress(float value)
    {
        if (burnMaterial != null)
            burnMaterial.SetFloat(BurnProgressProperty, value);
    }

    public void PlayBurn()
    {
        if (burnCoroutine != null)
            StopCoroutine(burnCoroutine);
        audioSource.clip = clip;
        audioSource.Play();
        burnCoroutine = StartCoroutine(BurnRoutine(0f, 1f));
    }

    public void ReverseBurn()
    {
        if (burnCoroutine != null)
            StopCoroutine(burnCoroutine);
        burnCoroutine = StartCoroutine(BurnRoutine(1f, 0f));
    }

    public void StopBurn()
    {
        if (burnCoroutine != null)
        {
            StopCoroutine(burnCoroutine);
            burnCoroutine = null;
        }
    }

    private System.Collections.IEnumerator BurnRoutine(float from, float to)
    {
        float elapsed = 0f;

        while (elapsed < burnDuration)
        {
            elapsed += Time.deltaTime;
            burnProgress = Mathf.Lerp(from, to, elapsed / burnDuration);
            SetBurnProgress(burnProgress);
            previousBurnProgress = burnProgress;
            yield return null;

            if (burnProgress >= 0.8f)
            {
                burnProgress = to;
                SetBurnProgress(to);
                previousBurnProgress = to;
                burnCoroutine = null;
                onBurnComplete?.Invoke();
            }
        }

        burnProgress = to;
        SetBurnProgress(to);
        previousBurnProgress = to;
        burnCoroutine = null;

        if (to >= 0.5f)
            onBurnComplete?.Invoke();
        else
            onReverseComplete?.Invoke();
    }
}
