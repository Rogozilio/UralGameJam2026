using UnityEngine;

public enum FootstepSurfaceType
{
    Default,
    Metal,
    Book,
    Slime
}

[RequireComponent(typeof(AudioSource))]
public class FootstepAudio : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private AudioSource audioSource;

    [Header("Current Surface")]
    [SerializeField] private FootstepSurfaceType currentSurfaceType = FootstepSurfaceType.Default;

    [Header("Volume")]
    [Range(0f, 1f)]
    [SerializeField] private float stepVolume = 1f;

    [Header("Surface Volumes")]
    [Range(0f, 1f)]
    [SerializeField] private float defaultVolume = 1f;
    [Range(0f, 1f)]
    [SerializeField] private float metalVolume = 1f;
    [Range(0f, 1f)]
    [SerializeField] private float bookVolume = 1f;
    [Range(0f, 1f)]
    [SerializeField] private float slimeVolume = 1f;

    [Header("Footstep Clips")]
    [SerializeField] private AudioClip[] defaultClips;
    [SerializeField] private AudioClip[] metalClips;
    [SerializeField] private AudioClip[] bookClips;
    [SerializeField] private AudioClip[] slimeClips;

    private AudioClip _lastPlayedClip;

    private void Reset()
    {
        audioSource = GetComponent<AudioSource>();
    }

    private void Awake()
    {
        if (audioSource == null)
            audioSource = GetComponent<AudioSource>();
    }

    public void PlayStep()
    {
        if (audioSource == null)
            return;

        var clips = GetClipsBySurfaceType(currentSurfaceType);
        var clip = GetRandomClip(clips);

        if (clip == null)
            return;

        audioSource.PlayOneShot(clip, stepVolume * GetSurfaceVolume(currentSurfaceType));
        _lastPlayedClip = clip;
    }

    public void SetSurfaceType(FootstepSurfaceType surfaceType)
    {
        currentSurfaceType = surfaceType;
    }

    public void ResetSurfaceTypeToDefault()
    {
        currentSurfaceType = FootstepSurfaceType.Default;
    }

    public void SetSurfaceType(int surfaceTypeIndex)
    {
        if (!System.Enum.IsDefined(typeof(FootstepSurfaceType), surfaceTypeIndex))
            return;

        currentSurfaceType = (FootstepSurfaceType)surfaceTypeIndex;
    }

    public FootstepSurfaceType GetCurrentSurfaceType()
    {
        return currentSurfaceType;
    }

    public void SetStepVolume(float volume)
    {
        stepVolume = Mathf.Clamp01(volume);
    }

    private AudioClip[] GetClipsBySurfaceType(FootstepSurfaceType surfaceType)
    {
        switch (surfaceType)
        {
            case FootstepSurfaceType.Metal:
                return metalClips;
            case FootstepSurfaceType.Book:
                return bookClips;
            case FootstepSurfaceType.Slime:
                return slimeClips;
            default:
                return defaultClips;
        }
    }

    private float GetSurfaceVolume(FootstepSurfaceType surfaceType)
    {
        switch (surfaceType)
        {
            case FootstepSurfaceType.Metal:
                return metalVolume;
            case FootstepSurfaceType.Book:
                return bookVolume;
            case FootstepSurfaceType.Slime:
                return slimeVolume;
            default:
                return defaultVolume;
        }
    }

    private AudioClip GetRandomClip(AudioClip[] clips)
    {
        if (clips == null || clips.Length == 0)
            return null;

        if (clips.Length == 1)
            return clips[0];

        AudioClip clip;

        do
        {
            clip = clips[Random.Range(0, clips.Length)];
        } while (clip == _lastPlayedClip);

        return clip;
    }
}
