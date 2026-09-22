using System;
using REC.Core;
using UnityEngine;

public sealed class RoundThreePresentation : MonoBehaviour
{
    [Header("데이터 이벤트 — 시간은 테이블을 따릅니다")]
    [SerializeField] private int roundId = 3;
    [SerializeField] private int questId = 15;
    [Header("아트 및 기준 사물")]
    [SerializeField] private string blanketName = "SYNC_04__UNI_02_FoldedBlanket";
    [SerializeField] private string bloodName = "I_15";
    [SerializeField] private PresentationActorSettings standingWoman = new();
    [SerializeField] private PresentationActorSettings crawlerWoman = new() { height = 0.8f };
    [Header("배치 — Anchor 미지정 시 기준 사물 주변에 임시 배치")]
    [SerializeField] private Transform standingAnchor;
    [SerializeField] private Transform corridorAnchor;
    [SerializeField] private Transform rushAnchor;
    [SerializeField] private Vector3 standingOffset = new(0.8f, 0, 0);
    [SerializeField] private Vector3 corridorOffset = new(1.5f, 0, 0);
    [SerializeField] private Vector3 triggerSize = new(2.5f, 3f, 3f);
    [Header("조명 깜빡임")]
    [SerializeField] private Light[] corridorLights = Array.Empty<Light>();
    [SerializeField, Min(1)] private int blinkCount = 3;
    [SerializeField, Min(0.01f)] private float darkSeconds = 0.2f;
    [SerializeField, Min(0.01f)] private float lightSeconds = 0.25f;
    [SerializeField, Min(0)] private float blackoutSeconds = 0.5f;
    [Header("돌진 및 화면")]
    [SerializeField, Min(0.1f)] private float rushDistance = 6f;
    [SerializeField, Min(0.1f)] private float rushSpeed = 7f;
    [SerializeField, Min(0.1f)] private float passDistance = 3f;
    [SerializeField, Min(0)] private float shakeDegrees = 1.8f;
    [SerializeField, Min(0.01f)] private float flashPeriod = 0.16f;
    [SerializeField, Range(0, 1)] private float flashOpacity = 0.9f;
    [Header("임시 음원 — 모두 즉시 시작/정지")]
    [SerializeField] private AudioClip blackoutClip;
    [SerializeField] private AudioClip rushClip;

    private enum Stage { Dormant, Open, Armed, Blinking, Rushing, Complete }
    private Stage stage;
    private GameSessionHost host;
    private PlayerController player;
    private Camera view;
    private GameObject blood;
    private Transform blanket;
    private PresentationActor standing, crawler;
    private PresentationTriggerVolume corridor;
    private PresentationScreenView screen;
    private AudioSource audioSource;
    private float elapsed;
    private float[] lightBaseline;
    private Vector3 rushStart, rushEnd;
    private bool configured, paused, capturedLights;

    public void Configure(GameSessionHost owner, PlayerController actor, Camera camera, Transform map)
    {
        host = owner; player = actor; view = camera;
        blanket = PresentationSceneLookup.Named(map, blanketName);
        blood = PresentationSceneLookup.Named(map, bloodName).gameObject;
        blood.SetActive(false);
        standing = new PresentationActor(transform, "Round3_StandingWoman", standingWoman);
        crawler = new PresentationActor(transform, "Round3_CrawlerWoman", crawlerWoman);
        Vector3 center = InGameSceneBootstrapper.GeometryBounds(blood.transform).center;
        Vector3 floor = PresentationSceneLookup.Floor(center + corridorOffset);
        corridor = PresentationTriggerVolume.Create(transform, "Round3_CorridorTrigger", corridorAnchor, floor + Vector3.up * 1.2f, triggerSize, player);
        corridor.Entered += BeginBlackout;
        if (corridorLights.Length == 0)
        {
            var nearby = new System.Collections.Generic.List<Light>();
            foreach (var light in map.GetComponentsInChildren<Light>(true))
                if (Vector3.Distance(light.transform.position, corridor.transform.position) < 12f) nearby.Add(light);
            corridorLights = nearby.ToArray();
        }
        lightBaseline = new float[corridorLights.Length];
        screen = gameObject.AddComponent<PresentationScreenView>(); screen.Configure(view);
        audioSource = PresentationSceneLookup.Audio(transform, "Round3_Audio", null, true);
        host.Changed += Handle;
        configured = true;
        ResetPresentation();
    }
    private void Handle(SessionEvent message)
    {
        if (message.Kind is "RoundStarted" or "GameOver" or "GameClear" or "ConfigurationError") { ResetPresentation(); return; }
        if (!isActiveAndEnabled || message.RoundId != roundId || message.QuestId != questId) return;
        if (message.Kind == "MissionOpened") Open();
        if (message.Kind == "MissionResolved" && stage == Stage.Open) Resolve();
    }
    private void Open()
    {
        if (stage != Stage.Dormant) return;
        Bounds bounds = InGameSceneBootstrapper.GeometryBounds(blanket);
        Vector3 feet = standingAnchor != null ? standingAnchor.position : PresentationSceneLookup.Floor(bounds.center + standingOffset);
        standing.Show(feet, standingAnchor != null ? standingAnchor.forward : view.transform.position - feet);
        stage = Stage.Open;
    }
    private void Resolve()
    {
        blood.SetActive(true);
        stage = Stage.Armed;
        corridor.Arm(true);
    }
    private void BeginBlackout()
    {
        if (stage != Stage.Armed) return;
        for (int i = 0; i < corridorLights.Length; i++)
            if (corridorLights[i] != null) lightBaseline[i] = corridorLights[i].intensity;
        capturedLights = true;
        elapsed = 0; stage = Stage.Blinking;
        Play(blackoutClip);
    }
    private void Update()
    {
        if (!configured || stage == Stage.Dormant) return;
        bool pause = Time.timeScale <= 0 || !Application.isFocused;
        standing.SetPaused(pause); crawler.SetPaused(pause);
        if (paused != pause) { paused = pause; if (pause) audioSource.Pause(); else audioSource.UnPause(); }
        if (pause) return;
        if (stage is not (Stage.Blinking or Stage.Rushing)) return;
        elapsed += Time.unscaledDeltaTime;
        if (stage == Stage.Blinking)
        {
            float period = Mathf.Max(0.01f, darkSeconds) + Mathf.Max(0.01f, lightSeconds);
            float blinkEnd = Mathf.Max(1, blinkCount) * period;
            bool dark = elapsed < blinkEnd ? elapsed % period < darkSeconds : elapsed < blinkEnd + blackoutSeconds;
            SetLights(dark ? 0 : 1);
            screen.SetBlackout(dark ? (elapsed < blinkEnd ? 0.85f : 1f) : 0);
            if (elapsed >= blinkEnd + Mathf.Max(0, blackoutSeconds)) BeginRush();
        }
        else
        {
            float length = Vector3.Distance(rushStart, rushEnd);
            float progress = Mathf.Clamp01(elapsed * Mathf.Max(0.1f, rushSpeed) / Mathf.Max(0.01f, length));
            crawler.Root.transform.position = Vector3.Lerp(rushStart, rushEnd, progress);
            screen.SetBlackout(elapsed % Mathf.Max(0.01f, flashPeriod) < flashPeriod * 0.35f ? flashOpacity : 0);
            screen.SetShake(elapsed, shakeDegrees);
            if (progress >= 1)
            {
                stage = Stage.Complete;
                screen.ResetView(); audioSource.Stop(); crawler.SetMotion(0);
                // 소멸이 명시되지 않은 귀신과 피 글씨는 다음 회차 초기화까지 유지합니다.
            }
        }
    }
    private void BeginRush()
    {
        SetLights(1); capturedLights = false; screen.ResetView();
        Vector3 forward = Vector3.ProjectOnPlane(view.transform.forward, Vector3.up).normalized;
        if (forward.sqrMagnitude < 0.1f) forward = player.transform.forward;
        Vector3 target = PresentationSceneLookup.Floor(player.transform.position);
        rushStart = rushAnchor != null ? rushAnchor.position : target + forward * rushDistance;
        Vector3 direction = Vector3.ProjectOnPlane(target - rushStart, Vector3.up).normalized;
        if (direction.sqrMagnitude < 0.1f) direction = -forward;
        rushEnd = target + direction * passDistance;
        rushEnd.y = rushStart.y;
        crawler.Show(rushStart, direction); crawler.SetMotion(2);
        Play(rushClip);
        elapsed = 0; stage = Stage.Rushing;
    }
    private void Play(AudioClip clip) { audioSource.Stop(); audioSource.clip = clip; if (clip != null) audioSource.Play(); }
    private void SetLights(float multiplier)
    {
        if (!capturedLights) return;
        for (int i = 0; i < corridorLights.Length; i++)
            if (corridorLights[i] != null) corridorLights[i].intensity = lightBaseline[i] * multiplier;
    }
    private void ResetPresentation()
    {
        SetLights(1); capturedLights = false;
        standing?.Hide(); crawler?.Hide();
        if (blood != null) blood.SetActive(false);
        if (corridor != null) corridor.Arm(false);
        if (screen != null) screen.ResetView();
        if (audioSource != null) audioSource.Stop();
        elapsed = 0; paused = false; stage = Stage.Dormant;
    }
    private void OnDisable() => ResetPresentation();
    private void OnDestroy()
    {
        ResetPresentation();
        if (host != null) host.Changed -= Handle;
        if (corridor != null) corridor.Entered -= BeginBlackout;
        standing?.Dispose(); crawler?.Dispose();
    }
#if UNITY_EDITOR
    [ContextMenu("테스트/3회차 사건 연출 시작 (미션 상태 유지)")]
    private void PreviewOpen() { if (Application.isPlaying && configured && host.Session.RoundId == roundId) { ResetPresentation(); Open(); } }
    [ContextMenu("테스트/3회차 해결 연출 (미션 상태 유지)")]
    private void PreviewResolve() { if (Application.isPlaying && configured && stage == Stage.Open) Resolve(); }
#endif
}
