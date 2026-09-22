using UnityEngine;
using UnityEngine.Audio;

[RequireComponent(typeof(AudioSource))]
public class DoorOpenPresentation : PresentationEffect
{
    [Header("문 회전 기준")]
    [SerializeField] private Transform hinge;
    [SerializeField] private Vector3 localAxis = Vector3.up;
    [SerializeField] private float openAngle = 90f;
    [SerializeField, Min(0f)] private float delay = 0.5f;

    [Header("문 열림 소리")]
    [SerializeField] private AudioClip openClip;
    [SerializeField] private AudioMixerGroup outputGroup;
    [SerializeField, Range(0f, 1f)] private float volume = 0.7f;
    [SerializeField, Min(0.01f)] private float minDistance = 1f;
    [SerializeField, Min(0.01f)] private float maxDistance = 12f;

    private AudioSource source;
    private Quaternion closedRotation;
    private Quaternion openedRotation;
    private bool soundStarted;

    private void Awake()
    {
        source = GetComponent<AudioSource>();
        source.playOnAwake = false;
        source.loop = false;
        source.spatialBlend = 1f;
        source.dopplerLevel = 0f;
        source.rolloffMode = AudioRolloffMode.Linear;
        source.Stop();

        if (hinge == null)
        {
            Debug.LogError("[문 연출] Hinge를 연결하세요.", this);
            enabled = false;
            return;
        }

        closedRotation = hinge.localRotation;
    }

    protected override void OnStarted()
    {
        Debug.Log($"[문 연출] 시작 / Hinge={hinge?.name}", this);
        if (hinge == null)
        {
            StopImmediately();
            return;
        }

        soundStarted = false;
        Vector3 axis = localAxis.sqrMagnitude > 0.0001f ? localAxis.normalized : Vector3.up;
        openedRotation = closedRotation * Quaternion.AngleAxis(openAngle, axis);
        hinge.localRotation = closedRotation;
    }

    protected override void OnTick(float elapsed)
    {
        if (hinge == null)
        {
            StopImmediately();
            return;
        }

        float startDelay = Mathf.Clamp(delay, 0f, Duration);
        if (elapsed < startDelay)
            return;

        if (!soundStarted)
        {
            Debug.Log($"[문 연출] 회전 시작 / 축={localAxis}, 각도={openAngle}", this);
            soundStarted = true;
            source.outputAudioMixerGroup = outputGroup;
            source.volume = volume;
            source.minDistance = Mathf.Max(0.01f, minDistance);
            source.maxDistance = Mathf.Max(source.minDistance, maxDistance);

            if (openClip != null)
            {
                source.clip = openClip;
                source.Play();
            }
        }

        float openingDuration = Duration - startDelay;
        float progress = openingDuration <= 0f ? 1f : Mathf.Clamp01((elapsed - startDelay) / openingDuration);
        hinge.localRotation = Quaternion.Slerp(closedRotation, openedRotation, progress);
    }

    protected override void OnPaused(bool paused)
    {
        if (paused)
            source.Pause();
        else
            source.UnPause();
    }

    protected override void OnStopped()
    {
        if (source == null)
            return;

        source.Stop();
        source.clip = null;
    }

    protected override void OnReset()
    {
        soundStarted = false;

        if (hinge != null)
            hinge.localRotation = closedRotation;
    }
}