using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(AudioSource))]
public class RecoveryAudioPlayer : MonoBehaviour
{
    [SerializeField] private RecoveryAudioSettings settings;

    private AudioSource source;
    private Transform followTarget;
    private bool playing;

    private void Awake()
    {
        source = GetComponent<AudioSource>();
        source.playOnAwake = false;
        source.loop = true;
        source.spatialBlend = 1f;
        source.dopplerLevel = 0f;
        source.rolloffMode = AudioRolloffMode.Linear;
        source.ignoreListenerPause = false;
        source.Stop();
    }
    public void Configure(RecoveryAudioSettings value)
    {
        StopImmediately();
        settings = value;
    }
    public bool Begin(RecoverySoundType type, Transform soundAnchor)
    {
        StopImmediately();

        if (!isActiveAndEnabled || source is null || settings is null ||
        soundAnchor is null || !soundAnchor.gameObject.activeInHierarchy ||
        Time.timeScale <= 0f || !Application.isFocused)
            return false;

        AudioClip clip = settings.GetClip(type);
        if (clip == null)
            return false;

        followTarget = soundAnchor;
        transform.position = soundAnchor.position;

        source.outputAudioMixerGroup = settings.OutputGroup;
        source.volume = settings.Volume;
        source.minDistance = settings.MinDistance;
        source.maxDistance = settings.MaxDistance;
        source.clip = clip;
        source.Play();
        playing = true;
        return true;
    }

    public void StopImmediately()
    {
        playing = false;
        followTarget = null;

        if (source == null)
            return;

        source.Stop();
        source.clip = null;
    }

    private void LateUpdate()
    {
        if (!playing)
            return;

        if (followTarget == null || !followTarget.gameObject.activeInHierarchy ||
            Time.timeScale <= 0f || !Application.isFocused)
        {
            StopImmediately();
            return;
        }

        transform.position = followTarget.position;
    }

    private void OplicationFocus(bool hasfocus)
    {
        if (!hasfocus)
            StopImmediately();
    }
    private void OnApplicationPause(bool paused)
    {
        if (paused)
            StopImmediately();
    }

    private void OnDisable() => StopImmediately();
    private void OnDestroy() => StopImmediately();
}
