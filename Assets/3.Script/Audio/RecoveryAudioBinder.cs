using UnityEngine;

[DisallowMultipleComponent]
public class RecoveryAudioBinder : MonoBehaviour
{
    [SerializeField] private PlayerInteractor interactor;
    [SerializeField] private RecoveryAudioPlayer audioPlayer;

    private PlayerInteractor subscribedInteractor;

    public void Configure(PlayerInteractor input, RecoveryAudioPlayer playback)
    {
        Unsubscribe();

        interactor = input;
        audioPlayer = playback;

        if (isActiveAndEnabled)
            Subscribe();
    }

    private void OnEnable() => Subscribe();

    private void Subscribe()
    {
        if (subscribedInteractor != null || interactor == null || audioPlayer == null)
            return;

        subscribedInteractor = interactor;
        subscribedInteractor.InteractionStarted += OnInteractionStarted;
        subscribedInteractor.InteractionEnded += OnInteractionEnded;
    }

    private void Unsubscribe()
    {
        if (subscribedInteractor != null)
        {
            subscribedInteractor.InteractionStarted -= OnInteractionStarted;
            subscribedInteractor.InteractionEnded -= OnInteractionEnded;
        }

        subscribedInteractor = null;

        if (audioPlayer != null)
            audioPlayer.StopImmediately();
    }

    private void OnInteractionStarted(Interact target)
    {
        //Debug.Log($"[복구음] 홀드 시작: {target?.name}", this);
        if (audioPlayer == null)
        {
            return;
        }

        audioPlayer.StopImmediately();

        if (target == null || !target.CanInteract)
        {
            return;
        }

        if (!target.TryGetComponent<IRecoveryAudioTarget>(out var audioTarget))
        {
            return;
        }

        if (!audioTarget.TryGetRecoveryAudio(out var soundType, out var soundAnchor))
        {
            return;
        }

        bool started = audioPlayer.Begin(soundType, soundAnchor);
    }

    private void OnInteractionEnded(Interact target)
    {
        if (audioPlayer != null)
            audioPlayer.StopImmediately();
    }

    private void OnDisable() => Unsubscribe();
    private void OnDestroy() => Unsubscribe();
}