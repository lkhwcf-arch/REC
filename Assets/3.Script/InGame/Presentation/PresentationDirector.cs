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
            foreach (PresentationEffect effect in effects)
            {
                if (effect != null)
                    effect.ResetForRound();
            }

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

            if (effect == null || !effect.isActiveAndEnabled || played[i] || !effect.Matches(message))
                continue;

            played[i] = true;
            Debug.Log($"[연출 발동] {effect.name} / 이벤트={message.Kind} / 회차={message.RoundId} / 퀘스트={message.QuestId} / 대상={message.TargetId}", effect);
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
