using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(AudioSource))]
public class PlayerFootstepAudio : MonoBehaviour
{
    [Header("참조")]
    [SerializeField] private PlayerController player;
    [Header("발소리")]
    [SerializeField] private AudioClip footstepLoop;
    [SerializeField, Range(0f, 1f)] private float volume = 0.7f;

    private AudioSource audioSource;

    void Awake()
    {
        audioSource = GetComponent<AudioSource>();

        audioSource.playOnAwake = false;
        audioSource.loop = true;
        audioSource.spatialBlend = 0f;
        audioSource.dopplerLevel = 0f;
        audioSource.volume = volume;
        audioSource.clip = footstepLoop;

        audioSource.Stop();

        if (player == null || footstepLoop == null)
        {
            Debug.LogError("[발소리] Player와 Footstep Loop를 연결하세요.", this);

            enabled = false;
        }
    }
    void LateUpdate()
    {
        //playercontroller.Update 의 이동 처리가 끝난후 판단
        bool shouldPlay = player != null && player.IsMoving && Application.isFocused && Time.timeScale > 0f;

        if (shouldPlay)
        {
            if (!audioSource.isPlaying)
            {
                audioSource.Play();
                //Debug.Log("재생중", this);
            }
        }
        else
        {
            StopPlayback();
        }
    }

    private void StopPlayback()
    {
        if (audioSource != null && audioSource.isPlaying)
            audioSource.Stop();
    }
    private void OnApplicationFocus(bool hasFocus)
    {
        if (!hasFocus)
            StopPlayback();
    }

    private void OnApplicationPause(bool paused)
    {
        if (paused)
            StopPlayback();
    }

    private void OnDisable()
    {
        StopPlayback();
    }
}
