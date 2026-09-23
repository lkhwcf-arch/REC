using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(AudioSource))]
public sealed class GameAmbienceAudio : MonoBehaviour
{
    [Header("참조")]
    [SerializeField] private CameraManager cameraManager;

    [Header("환경음")]
    [SerializeField] private AudioClip ambienceLoop;

    [SerializeField, Range(0f, 1f)]
    private float volume = 0.5f;

    private AudioSource audioSource;
    private bool playbackPaused;

    private void Awake()
    {
        audioSource = GetComponent<AudioSource>();

        audioSource.playOnAwake = false;
        audioSource.loop = true;
        audioSource.spatialBlend = 0f;
        audioSource.dopplerLevel = 0f;
        audioSource.volume = volume;
        audioSource.clip = ambienceLoop;

        audioSource.Stop();
    }

    private void OnEnable()
    {
        if (cameraManager == null || ambienceLoop == null)
        {
            Debug.LogError(
                "[환경음] CameraManager와 Ambience Loop를 연결하세요.",
                this);

            enabled = false;
            return;
        }

        cameraManager.ModeChanged += OnCameraModeChanged;

        playbackPaused = false;
        audioSource.Play();

        RefreshPlayback();
    }

    private void OnCameraModeChanged(CameraMode mode)
    {
        RefreshPlayback();
    }

    private void LateUpdate()
    {
        // Time.timeScale 변경과 포커스 상태를 확인
        // 실제 Pause/UnPause는 상태가 달라질 때만 호출
        RefreshPlayback();
    }

    private void RefreshPlayback()
    {
        if (audioSource == null || cameraManager == null)
            return;

        bool shouldPause = !cameraManager.IsPlayerView || Time.timeScale <= 0f || !Application.isFocused;

        if (playbackPaused == shouldPause)
            return;

        playbackPaused = shouldPause;

        if (playbackPaused)
            audioSource.Pause();
        else
            audioSource.UnPause();
    }

    private void OnApplicationFocus(bool hasFocus)
    {
        if (isActiveAndEnabled)
            RefreshPlayback();
    }

    private void OnDisable()
    {
        if (cameraManager != null)
            cameraManager.ModeChanged -= OnCameraModeChanged;

        if (audioSource != null)
            audioSource.Stop();

        playbackPaused = false;
    }
}