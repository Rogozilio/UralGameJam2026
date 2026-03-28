using UnityEngine;

public class BGM : MonoBehaviour
{
    [Header("Layered Music Sources")]
    public AudioSource[] sources;

    [Header("Standalone Music Sources")]
    public AudioSource menuMusicSource;
    public AudioSource endGameMusicSource;

    [Header("Layers")]
    [Range(0, 5)]
    public int currentTrackIndex = 0;

    [Header("Master Toggle")]
    public bool isEnabled = true;

    [Header("Standalone Toggles")]
    public bool isMenuMusicEnabled;
    public bool isEndGameMusicEnabled;

    [Header("Smooth")]
    public float fadeSpeed = 3f;

    private float[] targetVolumes;
    private float menuMusicDefaultVolume;
    private float endGameMusicDefaultVolume;

    void Start()
    {
        targetVolumes = new float[GetLayeredSourceCount()];

        CacheStandaloneVolumes();
        ResetLayeredSources();
        RestoreStandaloneVolumes();
        UpdatePlaybackStates();
    }

    void Update()
    {
        UpdateTargetVolumes();
        UpdatePlaybackStates();
        SmoothVolumes();
    }

    void UpdateTargetVolumes()
    {
        int layeredSourceCount = GetLayeredSourceCount();
        bool standaloneMusicIsActive = IsStandaloneMusicActive();

        for (int i = 0; i < layeredSourceCount; i++)
        {
            targetVolumes[i] = !isEnabled || standaloneMusicIsActive
                ? 0f
                : i <= currentTrackIndex ? 1f : 0f;
        }
    }

    void SmoothVolumes()
    {
        for (int i = 0; i < GetLayeredSourceCount(); i++)
        {
            SmoothLayeredSource(sources[i], targetVolumes[i]);
        }
    }

    public void SetLayer(int index)
    {
        if (GetLayeredSourceCount() == 0)
        {
            currentTrackIndex = 0;
            return;
        }

        currentTrackIndex = Mathf.Clamp(index, 0, sources.Length - 1);
    }

    public void EnableMusic(bool value)
    {
        isEnabled = value;
    }

    public void EnableMenuMusic(bool value)
    {
        isMenuMusicEnabled = value;
    }

    public void EnableEndGameMusic(bool value)
    {
        isEndGameMusicEnabled = value;
    }

    private void SmoothLayeredSource(AudioSource source, float targetVolume)
    {
        if (source == null)
        {
            return;
        }

        source.volume = Mathf.Lerp(
            source.volume,
            targetVolume,
            Time.deltaTime * fadeSpeed
        );
    }

    private void UpdatePlaybackStates()
    {
        if (!isEnabled)
        {
            StopAndResetLayeredSources();
            StopAndResetStandaloneSource(menuMusicSource);
            StopAndResetStandaloneSource(endGameMusicSource);
            RestoreStandaloneVolumes();
            return;
        }

        if (IsStandaloneMusicActive())
        {
            StopAndResetLayeredSources();
            UpdateStandaloneSourcePlayback(menuMusicSource, isMenuMusicEnabled, menuMusicDefaultVolume);
            UpdateStandaloneSourcePlayback(endGameMusicSource, isEndGameMusicEnabled, endGameMusicDefaultVolume);
            return;
        }

        EnsureLayeredSourcesPlaying();
        StopAndResetStandaloneSource(menuMusicSource);
        StopAndResetStandaloneSource(endGameMusicSource);
        RestoreStandaloneVolumes();
    }

    private bool IsStandaloneMusicActive()
    {
        return isMenuMusicEnabled || isEndGameMusicEnabled;
    }

    private int GetLayeredSourceCount()
    {
        return sources == null ? 0 : sources.Length;
    }

    private void EnsureLayeredSourcesPlaying()
    {
        if (sources == null)
        {
            return;
        }

        foreach (var source in sources)
        {
            if (source == null || source.isPlaying)
            {
                continue;
            }

            source.volume = 0f;
            source.time = 0f;
            source.Play();
        }
    }

    private void StopAndResetLayeredSources()
    {
        if (sources == null)
        {
            return;
        }

        foreach (var source in sources)
        {
            if (source == null)
            {
                continue;
            }

            if (source.isPlaying)
            {
                source.Stop();
            }

            source.time = 0f;
            source.volume = 0f;
        }
    }

    private void StopAndResetStandaloneSource(AudioSource source)
    {
        if (source == null)
        {
            return;
        }

        if (source.isPlaying)
        {
            source.Stop();
        }

        source.time = 0f;
    }

    private void UpdateStandaloneSourcePlayback(AudioSource source, bool shouldPlay, float defaultVolume)
    {
        if (source == null)
        {
            return;
        }

        source.volume = defaultVolume;

        if (!shouldPlay)
        {
            StopAndResetStandaloneSource(source);
            return;
        }

        if (!source.isPlaying)
        {
            source.time = 0f;
            source.Play();
        }
    }

    private void CacheStandaloneVolumes()
    {
        menuMusicDefaultVolume = menuMusicSource != null ? menuMusicSource.volume : 1f;
        endGameMusicDefaultVolume = endGameMusicSource != null ? endGameMusicSource.volume : 1f;
    }

    private void RestoreStandaloneVolumes()
    {
        if (menuMusicSource != null)
        {
            menuMusicSource.volume = menuMusicDefaultVolume;
        }

        if (endGameMusicSource != null)
        {
            endGameMusicSource.volume = endGameMusicDefaultVolume;
        }
    }

    private void ResetLayeredSources()
    {
        if (sources == null)
        {
            return;
        }

        foreach (var source in sources)
        {
            if (source != null)
            {
                source.volume = 0f;
            }
        }
    }
}
