using UnityEngine;
using UnityEngine.Audio;


[CreateAssetMenu(fileName = "RecoveryAudioSettings", menuName = "REC/Audio/Recovery Audio Settings")]
public class RecoveryAudioSettings : ScriptableObject
{
    [Header("복구 음원 — 현재는 모두 임시 음원 연결")]
    [SerializeField] private AudioClip restoreMovement;
    [SerializeField] private AudioClip restoreMissing;
    [SerializeField] private AudioClip removeAdded;

    [Header("출력")]
    [SerializeField] private AudioMixerGroup outputGroup;
    [SerializeField, Range(0f, 1f)] private float volume = 1f;

    [Header("3D 거리")]
    [SerializeField, Min(0.01f)] private float minDistance = 1f;
    [SerializeField, Min(0.01f)] private float maxDistance = 12f;

    public AudioMixerGroup OutputGroup => outputGroup;
    public float Volume => Mathf.Clamp01(volume);
    public float MinDistance => Mathf.Max(0.01f, minDistance);
    public float MaxDistance => Mathf.Max(MinDistance, maxDistance);

    public AudioClip GetClip(RecoverySoundType type) => type switch
    {
        RecoverySoundType.RestoreMovement => restoreMovement,
        RecoverySoundType.RestoreMissing => restoreMissing,
        RecoverySoundType.RemoveAdded => removeAdded,
        _ => null
    };

    private void OnValidate()
    {
        volume = Mathf.Clamp01(volume);
        minDistance = Mathf.Max(0.01f, minDistance);
        maxDistance = Mathf.Max(minDistance, maxDistance);
    }
}
