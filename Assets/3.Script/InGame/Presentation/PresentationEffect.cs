using UnityEngine;

public abstract class PresentationEffect : MonoBehaviour
{
    [SerializeField] private string triggerEvent;
    [SerializeField, Min(0.01f)] private float duration = 2f;

    private float elapsed;

    public string TriggerEvent => triggerEvent;
    public bool IsRunning { get; private set; }

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

        elapsed += Mathf.Max(0f, deltaTime);

        if (elapsed >= Mathf.Max(0.01f, duration))
        {
            StopImmediately();
            return;
        }

        OnTick(elapsed);
    }

    public void SetPaused(bool paused)
    {
        if (IsRunning)
            OnPaused(paused);
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
    protected virtual void OnDisable() => StopImmediately();
}