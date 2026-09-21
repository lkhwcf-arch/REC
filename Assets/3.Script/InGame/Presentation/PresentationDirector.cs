using System;
using REC.Core;
using UnityEngine;

public class PresentationDirector : MonoBehaviour
{
    [SerializeField] private PresentationEffect[] effects = Array.Empty<PresentationEffect>();

    private bool[] played;
    private bool suspended;

    private void Awake()
    {
        played = new bool[effects.Length];
    }

    public void Handle(SessionEvent message)
    {
        if (message.Kind == "RoundStarted")
        {
            StopAll();
            Array.Clear(played, 0, played.Length);
            return;
        }

        if (message.Kind is "GameOver" or "GameClear" or "ConfigurationError")
        {
            StopAll();
            return;
        }

        for (int i = 0; i < effects.Length; i++)
        {
            PresentationEffect effect = effects[i];

            if (effect == null || !effect.isActiveAndEnabled || played[i] ||
                effect.TriggerEvent != message.Kind)
                continue;

            played[i] = true;
            effect.Play();
            effect.SetPaused(suspended);
        }
    }

    public void SetSuspended(bool value)
    {
        if (suspended == value)
            return;

        suspended = value;

        foreach (PresentationEffect effect in effects)
            if (effect != null)
                effect.SetPaused(value);
    }

    private void Update()
    {
        SetSuspended(Time.timeScale <= 0f || !Application.isFocused);

        if (suspended)
            return;

        float deltaTime = Time.unscaledDeltaTime;

        foreach (PresentationEffect effect in effects)
            if (effect != null && effect.isActiveAndEnabled && effect.IsRunning)
                effect.Tick(deltaTime);
    }

    private void OnApplicationFocus(bool hasFocus)
    {
        SetSuspended(!hasFocus || Time.timeScale <= 0f);
    }

    private void StopAll()
    {
        foreach (PresentationEffect effect in effects)
            if (effect != null)
                effect.StopImmediately();
    }

    private void OnDisable() => StopAll();
}
