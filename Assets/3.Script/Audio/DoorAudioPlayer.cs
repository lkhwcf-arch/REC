using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(AudioSource))]
public sealed class DoorAudioPlayer : MonoBehaviour
{
    private DoorInteraction door;
    private AudioSource audioSource;

    private AudioClip openClip;
    private AudioClip closeClip;

    private bool paused;

    private void Awake()
    {
        audioSource = GetComponent<AudioSource>();

        audioSource.playOnAwake = false;
        audioSource.loop = false;
        audioSource.spatialBlend = 1f;
        audioSource.dopplerLevel = 0f;
        audioSource.rolloffMode = AudioRolloffMode.Linear;

        audioSource.Stop();
    }

    public void Configure(DoorInteraction target, AudioClip opening, AudioClip closing,
                            float volume, float minDistance, float maxDistance)
    {
        Unsubscribe();

        door = target;
        openClip = opening;
        closeClip = closing;

        audioSource.volume = Mathf.Clamp01(volume);
        audioSource.minDistance = Mathf.Max(0.01f, minDistance);
        audioSource.maxDistance = Mathf.Max(
            audioSource.minDistance + 0.01f,
            maxDistance);

        if (door == null)
            return;

        door.MotionStarted += OnMotionStarted;
        door.MotionCancelled += StopPlayback;
    }

    private void OnMotionStarted(bool opening)
    {
        if (!isActiveAndEnabled)
            return;

        // 이전 클립과 겹치지 않도록 즉시 끊습니다.
        StopPlayback();

        audioSource.clip = opening ? openClip : closeClip;

        if (audioSource.clip == null)
            return;

        audioSource.Play();
        SynchronizePause();
    }

    private void LateUpdate()
    {
        SynchronizePause();
    }

    private void SynchronizePause()
    {
        bool shouldPause = !Application.isFocused || Time.timeScale <= 0f;

        if (paused == shouldPause)
            return;

        paused = shouldPause;

        if (paused)
            audioSource.Pause();
        else
            audioSource.UnPause();
    }

    private void StopPlayback()
    {
        if (audioSource != null)
            audioSource.Stop();

        paused = false;
    }

    private void OnApplicationFocus(bool hasFocus)
    {
        if (isActiveAndEnabled)
            SynchronizePause();
    }

    private void OnDisable()
    {
        StopPlayback();
    }

    private void Unsubscribe()
    {
        if (door == null)
            return;

        door.MotionStarted -= OnMotionStarted;
        door.MotionCancelled -= StopPlayback;
    }

    private void OnDestroy()
    {
        Unsubscribe();
    }
}