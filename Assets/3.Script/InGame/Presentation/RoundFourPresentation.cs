using System;
using REC.Core;
using UnityEngine;
using UnityEngine.Video;

public sealed class RoundFourPresentation : MonoBehaviour
{
    [Header("데이터 이벤트 — 비활성 미션을 강제로 활성화하지 않습니다")]
    [SerializeField] private int roundId = 4;
    [SerializeField] private int questId = 28;
    [SerializeField] private string portraitName = "I_14";
    [Header("남자 귀신 및 4빈소 배치")]
    [SerializeField] private PresentationActorSettings man = new();
    [SerializeField] private Transform peekAnchor;
    [SerializeField] private Transform retreatAnchor;
    [SerializeField] private Transform approachAnchor;
    [SerializeField] private Transform closeAnchor;
    [SerializeField] private Vector3 peekOffset = new(0.25f, 0, 0);
    [SerializeField] private Vector3 approachSize = new(10, 3, 10);
    [SerializeField] private Vector3 closeSize = new(3, 3, 3);
    [SerializeField, Min(0.1f)] private float retreatSeconds = 1f;
    [Header("환경음 / 심장박동 — Cut 전환")]
    [SerializeField] private AudioSource[] ambienceSources = Array.Empty<AudioSource>();
    [SerializeField] private AudioClip temporaryAmbience;
    [SerializeField] private AudioClip heartbeatClip;
    [Header("엔딩")]
    [SerializeField] private VideoClip endingClip;
    [SerializeField, Min(0.1f)] private float temporaryEndingSeconds = 2f;
    [SerializeField, Min(1)] private float videoPrepareTimeout = 10f;

    private enum Stage { Dormant, Peeking, Heartbeat, Retreating, Hidden, Ending, Complete }
    private Stage stage;
    private GameSessionHost host;
    private InGameSessionController flow;
    private PlayerController player;
    private PlayerInteractor interactor;
    private PresentationActor ghost;
    private PresentationTriggerVolume approach, close;
    private AudioSource heartbeat, fallbackAmbience;
    private bool[] ambienceMuted;
    private bool ambienceCaptured, configured, paused, previewEnding;
    private Vector3 peekPosition, retreatPosition, facing;
    private float elapsed;
    private EndingVideoView ending;

    public void Configure(GameSessionHost owner, InGameSessionController controller, PlayerController actor, PlayerInteractor input, Transform map)
    {
        host = owner; flow = controller; player = actor; interactor = input;
        Transform portrait = PresentationSceneLookup.Named(map, portraitName);
        Bounds portraitBounds = InGameSceneBootstrapper.GeometryBounds(portrait);
        Transform wreath = null; float best = float.MaxValue;
        // 한 번만 기준 화환을 찾습니다. 실제 4빈소 위치는 Anchor를 지정해 덮어쓸 수 있습니다.
        foreach (var node in map.GetComponentsInChildren<Transform>(true))
        {
            if (!node.name.StartsWith("Wreath", StringComparison.Ordinal) || node.GetComponent<Renderer>() == null) continue;
            Vector3 center = node.GetComponent<Renderer>().bounds.center;
            float distance = (center - portraitBounds.center).sqrMagnitude;
            if (distance < best) { best = distance; wreath = node; }
        }
        Vector3 wreathPosition = wreath != null ? InGameSceneBootstrapper.GeometryBounds(wreath).center : portraitBounds.center + Vector3.forward * 3f;
        peekPosition = peekAnchor != null ? peekAnchor.position : PresentationSceneLookup.Floor(wreathPosition + peekOffset);
        retreatPosition = retreatAnchor != null ? retreatAnchor.position : PresentationSceneLookup.Floor(portraitBounds.center);
        facing = Vector3.ProjectOnPlane(peekPosition - retreatPosition, Vector3.up);
        ghost = new PresentationActor(transform, "Round4_MaskedHumanoid", man);
        approach = PresentationTriggerVolume.Create(transform, "Round4_ApproachTrigger", approachAnchor, peekPosition + Vector3.up * 1.2f, approachSize, player);
        close = PresentationTriggerVolume.Create(transform, "Round4_CloseTrigger", closeAnchor, peekPosition + Vector3.up * 1.2f, closeSize, player);
        approach.Entered += BeginHeartbeat; close.Entered += BeginRetreat;
        heartbeat = PresentationSceneLookup.Audio(transform, "Round4_Heartbeat", heartbeatClip, true);
        fallbackAmbience = PresentationSceneLookup.Audio(transform, "Round4_TemporaryAmbience", temporaryAmbience, true);
        ambienceMuted = new bool[ambienceSources.Length];
        ending = gameObject.AddComponent<EndingVideoView>(); ending.Configure(); ending.Completed += CompleteEnding;
        host.ConfigureEndingQuest(questId);
        host.Changed += Handle;
        configured = true; ResetPresentation();
    }
    private void Handle(SessionEvent message)
    {
        if (message.Kind is "RoundStarted" or "GameOver" or "GameClear" or "ConfigurationError") { ResetPresentation(); return; }
        if (!isActiveAndEnabled || message.RoundId != roundId || message.QuestId != questId) return;
        if (message.Kind == "MissionOpened") Open();
        if (message.Kind == "EndingStarted") BeginEnding(false);
    }
    private void Open()
    {
        if (stage != Stage.Dormant) return;
        ghost.Show(peekPosition, peekAnchor != null ? peekAnchor.forward : facing);
        approach.Arm(true); close.Arm(true); stage = Stage.Peeking;
        if (ambienceSources.Length == 0 && temporaryAmbience != null) fallbackAmbience.Play();
    }
    private void CaptureAmbience()
    {
        if (ambienceCaptured) return;
        for (int i = 0; i < ambienceSources.Length; i++)
        {
            var source = ambienceSources[i]; if (source == null) continue;
            ambienceMuted[i] = source.mute; source.mute = true;
        }
        ambienceCaptured = true; fallbackAmbience.Stop();
    }
    private void RestoreAmbience(bool restartFallback)
    {
        heartbeat.Stop();
        if (ambienceCaptured)
            for (int i = 0; i < ambienceSources.Length; i++)
                if (ambienceSources[i] != null) ambienceSources[i].mute = ambienceMuted[i];
        ambienceCaptured = false;
        if (restartFallback && ambienceSources.Length == 0 && temporaryAmbience != null) fallbackAmbience.Play();
    }
    private void BeginHeartbeat()
    {
        if (stage != Stage.Peeking) return;
        CaptureAmbience(); if (heartbeatClip != null) heartbeat.Play(); stage = Stage.Heartbeat;
    }
    private void BeginRetreat()
    {
        if (stage is not (Stage.Peeking or Stage.Heartbeat)) return;
        BeginHeartbeat();
        approach.Arm(false); close.Arm(false);
        ghost.Show(peekPosition, retreatPosition - peekPosition); ghost.SetMotion(1);
        elapsed = 0; stage = Stage.Retreating;
    }
    private void Update()
    {
        if (!configured || stage == Stage.Dormant) return;
        bool pause = Time.timeScale <= 0 || !Application.isFocused;
        ghost.SetPaused(pause);
        if (paused != pause)
        {
            paused = pause;
            if (pause) { heartbeat.Pause(); fallbackAmbience.Pause(); }
            else { heartbeat.UnPause(); fallbackAmbience.UnPause(); }
        }
        if (pause || stage != Stage.Retreating) return;
        elapsed += Time.unscaledDeltaTime;
        float t = Mathf.Clamp01(elapsed / Mathf.Max(0.1f, retreatSeconds));
        ghost.Root.transform.position = Vector3.Lerp(peekPosition, retreatPosition, t);
        if (t >= 1) { ghost.Hide(); RestoreAmbience(true); stage = Stage.Hidden; }
    }
    private void BeginEnding(bool preview)
    {
        if (stage == Stage.Ending) return;
        previewEnding = preview;
        if (flow.IsPaused) flow.SetPaused(false);
        flow.SetEndingPreview(preview);
        interactor.CancelCurrentInteraction(); player.SetPresentationLock(this, true);
        approach.Arm(false); close.Arm(false); ghost.Hide();
        CaptureAmbience(); heartbeat.Stop(); fallbackAmbience.Stop();
        stage = Stage.Ending;
        ending.Play(endingClip, temporaryEndingSeconds, videoPrepareTimeout);
    }
    private void CompleteEnding()
    {
        player.SetPresentationLock(this, false); flow.SetEndingPreview(false);
        RestoreAmbience(false); stage = Stage.Complete;
        if (!previewEnding) host.Session.RequestCompleteEnding();
        previewEnding = false;
    }
    private void ResetPresentation()
    {
        ghost?.Hide();
        if (approach != null) approach.Arm(false);
        if (close != null) close.Arm(false);
        if (heartbeat != null) RestoreAmbience(false);
        if (fallbackAmbience != null) fallbackAmbience.Stop();
        if (ending != null) ending.Stop();
        if (player != null) player.SetPresentationLock(this, false);
        if (flow != null) flow.SetEndingPreview(false);
        elapsed = 0; paused = previewEnding = false; stage = Stage.Dormant;
    }
    private void OnDisable() => ResetPresentation();
    private void OnDestroy()
    {
        ResetPresentation();
        if (host != null) host.Changed -= Handle;
        if (approach != null) approach.Entered -= BeginHeartbeat;
        if (close != null) close.Entered -= BeginRetreat;
        if (ending != null) ending.Completed -= CompleteEnding;
        ghost?.Dispose();
    }
#if UNITY_EDITOR
    [ContextMenu("테스트/4회차 귀신 연출 (미션 상태 유지)")]
    private void PreviewGhost() { if (Application.isPlaying && configured && host.Session.RoundId == roundId) { ResetPresentation(); Open(); } }
    [ContextMenu("테스트/엔딩 미리보기 (클리어 전환 없음)")]
    private void PreviewEnding() { if (Application.isPlaying && configured && host.Session.RoundId == roundId) BeginEnding(true); }
#endif
}
