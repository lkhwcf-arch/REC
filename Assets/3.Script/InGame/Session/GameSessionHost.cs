using System;
using System.Collections.Generic;
using REC.Core;
using UnityEngine;
using UnityEngine.Events;

[Serializable] public class SessionCueEvent : UnityEvent<string, int, int> { }

// Unity 생명주기, 데이터 저장소, 맵 어댑터를 조립하는 구성 루트입니다.
public class GameSessionHost : MonoBehaviour
{

    [SerializeField] private GameDataBootstrapper dataBootstrapper;
    [SerializeField] private AnomalyTargetAdapter[] targets = Array.Empty<AnomalyTargetAdapter>();
    [SerializeField] private RoundResetScope resetScope;
    [SerializeField, Min(1)] private int firstRoundId = 1;
    [SerializeField] private int randomSeed = 1709;
    [SerializeField, Min(1)] private int millisecondsPerMinute = 2500;
    [SerializeField, Range(0, 1439)] private int startMinute = 1320;
    [SerializeField, Min(1)] private int intermediateReturnMinute = 60;
    [SerializeField, Min(1)] private int firstObservationMinute = 90;
    [SerializeField, Min(1)] private int roundDurationMinutes = 480;

    [Header("연출 연결: 사건 종류 / DirectionGroupID / TargetID")]
    [SerializeField] private SessionCueEvent onPresentationCue = new();
    [SerializeField] private UnityEvent onRoundReset = new();
    [SerializeField] private UnityEvent onGameClear = new();
    [SerializeField] private UnityEvent onGameOver = new();
    private double remainderMs;
    private bool initialized;
    public GameSession Session { get; private set; }
    public event Action<SessionEvent> Changed;
    public void Configure(GameDataBootstrapper data, AnomalyTargetAdapter[] bindings, RoundResetScope scope = null, int firstRound = 1, int seed = 1709)
    {
        dataBootstrapper = data; targets = bindings; resetScope = scope; firstRoundId = firstRound; randomSeed = seed;
    }
    private void Start() => Initialize();
    public void Initialize()
    {
        if (initialized) return;

        initialized = true;

        if (dataBootstrapper == null || !dataBootstrapper.IsLoaded)
        {
            Debug.LogError("[회차 초기화] 데이터 로드를 먼저 완료하세요.", this); enabled = false;
            return;
        }

        Dictionary<int, IAnomalyBody> bodies = new();
        foreach (var target in targets)
        {
            if (target == null) { Debug.LogError("[회차 초기화] 대상 연결이 비었습니다.", this); enabled = false; return; }
            foreach (int id in target.TargetIds)
                if (!bodies.TryAdd(id, target)) { Debug.LogError($"[회차 초기화] TargetID={id} 연결이 중복되었습니다.", this); enabled = false; return; }
        }
        try
        {
            if (resetScope != null) resetScope.Capture();
            var rules = new SessionRules(millisecondsPerMinute, startMinute, intermediateReturnMinute, firstObservationMinute, roundDurationMinutes);
            var planner = new MissionPlanner(dataBootstrapper.Data, new SeededRandom(randomSeed));
            Session = new GameSession(dataBootstrapper.Data, planner, new AnomalyRuntime(bodies, new AnomalyActionRegistry()), rules);
            Session.Changed += OnChanged;
            foreach (var target in targets) target.Bind(Session);
            Session.Start(firstRoundId);
        }
        catch (Exception exception) { Debug.LogError($"[회차 초기화] {exception.Message}", this); enabled = false; }
    }
    private void Update()
    {
        if (Session == null || Time.timeScale <= 0 || !Application.isFocused) return;
        remainderMs += Time.unscaledDeltaTime * 1000.0;
        long step = (long)remainderMs; remainderMs -= step; Session.Tick(step);
    }
    private void OnChanged(SessionEvent message)
    {
        if (message.Kind == "RoundStarted") { remainderMs = 0; if (resetScope != null) resetScope.ResetMap(); }
        // 연출 실패가 이미 완료된 도메인 상태 변경을 되돌리지 않도록 경계를 분리합니다.
        try
        {
            if (message.Kind == "RoundStarted") onRoundReset.Invoke();
            if (message.Kind == "GameOver") onGameOver.Invoke();
            if (message.Kind == "GameClear") onGameClear.Invoke();
            onPresentationCue.Invoke(message.Kind, message.DirectionGroupId, message.TargetId);
        }
        catch (Exception exception) { Debug.LogException(exception, this); }
        if (Changed != null)
            foreach (Action<SessionEvent> listener in Changed.GetInvocationList())
                try { listener(message); } catch (Exception exception) { Debug.LogException(exception, this); }
        if (message.Kind == "ConfigurationError") Debug.LogError($"[회차 설정 오류] {Session.Error}", this);
        else Debug.Log($"[회차] {message.Kind} | Round={message.RoundId} Quest={message.QuestId} Target={message.TargetId}", this);
    }
    public void RequestReturn() => Session?.RequestEnterRoom();
    public void RequestObserve() => Session?.RequestObserveCctv();
    public void RequestPatrol() => Session?.RequestLeaveRoom();
    public void RequestSkip() => Session?.RequestSkip();
    public void RequestNextRound() => Session?.RequestNextRound();
    private void OnDestroy()
    {
        // 씬 파괴 시 대상의 OnDestroy 순서는 보장되지 않습니다. 재시작 초기화는 BeginRound가 소유합니다.
        if (Session != null) { Session.Changed -= OnChanged; Session.Stop(false); }
        foreach (var target in targets) if (target != null) target.Unbind();
    }

#if UNITY_EDITOR
    [Header("에디터 테스트")]
    [SerializeField, Min(1)] private int testRoundId = 2;
#endif
#if UNITY_EDITOR
    [ContextMenu("테스트/지정 회차 시작")]
    private void StartTestRound()
    {
        if (!Application.isPlaying)
        {
            Debug.LogWarning("[회차 테스트] 플레이 중에 실행하세요.", this);
            return;
        }

        if (!isActiveAndEnabled || Session == null)
        {
            Debug.LogWarning("[회차 테스트] 실행 가능한 세션이 없습니다.", this);
            return;
        }

        if (!Session.RequestTestRound(testRoundId, out string error))
        {
            Debug.LogWarning($"[회차 테스트] {error}", this);
            return;
        }

        Debug.Log($"[회차 테스트] {testRoundId}회차를 22:00부터 시작합니다.", this);
    }
#endif
}
