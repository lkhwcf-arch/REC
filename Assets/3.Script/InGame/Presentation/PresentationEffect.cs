using REC.Core;
using UnityEngine;

public abstract class PresentationEffect : MonoBehaviour
{
    [Header("발동 조건 — ID가 0이면 제한 없음")]
    [SerializeField] private string triggerEvent;
    [SerializeField, Min(0)] private int roundId;
    [SerializeField, Min(0)] private int questId;
    [SerializeField, Min(0)] private int targetId;
    [Header("재생 시간")]
    [SerializeField, Min(0.01f)] private float duration = 2f;

    private float elapsed;

    public string TriggerEvent => triggerEvent;
    public bool IsRunning { get; private set; }
    protected float Duration => Mathf.Max(0.01f, duration);

    public bool Matches(SessionEvent message)
    {
        return triggerEvent == message.Kind &&
            (roundId == 0 || roundId == message.RoundId) &&
            (questId == 0 || questId == message.QuestId) &&
            (targetId == 0 || targetId == message.TargetId);
    }

    public void Play()
    {
        StopImmediately();
        elapsed = 0f;
        IsRunning = true;
        OnStarted();
    }

    public void Tick(float deltaTime)
    {
        if (!IsRunning)
            return;

        elapsed = Mathf.Min(elapsed + Mathf.Max(0f, deltaTime), Duration);
        OnTick(elapsed);

        if (elapsed >= Duration)
            StopImmediately();
    }

    public void SetPaused(bool paused)
    {
        if (IsRunning)
            OnPaused(paused);
    }

    public void ResetForRound()
    {
        StopImmediately();
        elapsed = 0f;
        OnReset();
    }

    public void StopImmediately()
    {
        if (!IsRunning)
            return;

        IsRunning = false;
        OnStopped();
    }

    protected abstract void OnStarted();
    protected abstract void OnTick(float elapsed);
    protected abstract void OnStopped();
    protected virtual void OnPaused(bool paused) { }
    protected virtual void OnReset() { }
    protected virtual void OnDisable() => StopImmediately();
}