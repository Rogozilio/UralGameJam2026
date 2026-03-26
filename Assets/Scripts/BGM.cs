using UnityEngine;

public class BGM : MonoBehaviour
{
    [Header("Audio Sources (6 tracks)")]
    public AudioSource[] sources;

    [Header("Layers")]
    [Range(0, 5)]
    public int currentTrackIndex = 0;

    [Header("Master Toggle")]
    public bool isEnabled = true;

    [Header("Smooth")]
    public float fadeSpeed = 3f;

    private float[] targetVolumes;

    void Start()
    {
        targetVolumes = new float[sources.Length];

        // Запускаем ВСЕ источники сразу
        foreach (var s in sources)
        {
            s.volume = 0f;
            s.Play();
        }
    }

    void Update()
    {
        UpdateTargetVolumes();
        SmoothVolumes();
    }

    void UpdateTargetVolumes()
    {
        if (!isEnabled)
        {
            // выключить всё
            for (int i = 0; i < sources.Length; i++)
                targetVolumes[i] = 0f;

            return;
        }

        for (int i = 0; i < sources.Length; i++)
        {
            // включаем все до currentTrackIndex
            targetVolumes[i] = (i <= currentTrackIndex) ? 1f : 0f;
        }
    }

    void SmoothVolumes()
    {
        for (int i = 0; i < sources.Length; i++)
        {
            sources[i].volume = Mathf.Lerp(
                sources[i].volume,
                targetVolumes[i],
                Time.deltaTime * fadeSpeed
            );
        }
    }

    // удобно вызывать из кода
    public void SetLayer(int index)
    {
        currentTrackIndex = Mathf.Clamp(index, 0, sources.Length - 1);
    }

    public void EnableMusic(bool value)
    {
        isEnabled = value;
    }
}
