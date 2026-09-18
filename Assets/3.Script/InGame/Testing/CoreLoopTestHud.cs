using REC.Core;
using UnityEngine;

// 외부 UI 팩에 의존하지 않는 테스트용 화면입니다. 실제 UI는 Host.Changed를 구독하면 됩니다.
public class CoreLoopTestHud : MonoBehaviour
{
    private GameSessionHost host;
    private CoreLoopTestPlayer player;
    private Font font;
    private GUIStyle label, heading, button;
    public void Configure(GameSessionHost owner, CoreLoopTestPlayer input) { host = owner; player = input; }
    private void OnGUI()
    {
        if (host == null || host.Session == null) return;
        if (label == null)
        {
            font = Font.CreateDynamicFontFromOSFont("Malgun Gothic", 17);
            label = new GUIStyle(GUI.skin.label) { font = font, fontSize = 17, wordWrap = true };
            heading = new GUIStyle(label) { fontSize = 23, fontStyle = FontStyle.Bold };
            button = new GUIStyle(GUI.skin.button) { font = font, fontSize = 17 };
        }
        GameSession session = host.Session;
        GUI.Box(new Rect(12, 12, 470, 280), GUIContent.none);
        GUILayout.BeginArea(new Rect(24, 20, 445, 260));
        int minute = session.DisplayMinute;
        GUILayout.Label($"{minute / 60:00}:{minute % 60:00}    {session.RoundId}회차", heading);
        GUILayout.Label(Describe(session.Phase), label);
        if (session.IsReturnOverdue) GUILayout.Label("23시 복귀 일정이 지났습니다. CCTV실로 복귀하세요.", label);
        foreach (var mission in session.Missions)
            GUILayout.Label($"사건 {mission.QuestId} · 사물 {mission.TargetId} · {Status(mission.Status)}", label);
        GUILayout.Label("WASD 이동 · 마우스 시점 · 좌클릭 1초 해결\nTab 가까운 CCTV실 복귀 · N 다음 일정 · Esc 메뉴", label);
        GUILayout.EndArea();
        if (player.Hovered != null)
        {
            GUI.Label(new Rect(Screen.width / 2f - 100, Screen.height / 2f + 30, 260, 35), $"해결하기 · 사물 {player.Hovered.TargetId}", label);
            GUI.Box(new Rect(Screen.width / 2f - 80, Screen.height / 2f + 65, 160 * Mathf.Clamp01(player.HoldProgress), 8), GUIContent.none);
        }
        GUI.Label(new Rect(Screen.width / 2f - 4, Screen.height / 2f - 12, 24, 24), "+", heading);
        if (session.CanPatrol && !player.MenuOpen) return;
        GUILayout.BeginArea(new Rect(Screen.width / 2f - 210, Screen.height / 2f + 110, 420, 350));
        if (session.Phase == SessionPhase.ControlRoom)
        {
            if (GUILayout.Button("23:30 / 다음 일정까지 시간 건너뛰기", button)) host.RequestSkip();
            if (GUILayout.Button("CCTV 확인", button)) { if (session.RequestObserveCctv()) player.ObserveCctv(); }
            if (GUILayout.Button("현장 순찰 시작", button)) host.RequestPatrol();
        }
        else if (session.Phase == SessionPhase.RoundComplete)
        {
            GUILayout.Label("미션 완료 · 다음 회차로 진행합니다.", label);
            if (GUILayout.Button("다음 회차 시작", button)) host.RequestNextRound();
        }
        else if (session.Phase == SessionPhase.GameClear) GUILayout.Label("모든 회차 완료!", heading);
        else if (session.Phase == SessionPhase.GameOver) GUILayout.Label("06:00 · 미완료 미션으로 게임오버", heading);
        else if (session.Phase == SessionPhase.ConfigurationError) GUILayout.Label(session.Error, label);
        else
        {
            GUI.enabled = player.NearRoom && session.CanEnterRoom;
            if (GUILayout.Button("CCTV실 복귀", button)) host.RequestReturn();
            GUI.enabled = true;
            if (GUILayout.Button("다음 일정까지 건너뛰기", button)) host.RequestSkip();
            if (session.Phase == SessionPhase.AnomalyPatrol && GUILayout.Button("시험: 06:00으로 이동 (미완료 시 실패)", button)) session.Tick(session.Rules.DeadlineMs - session.ElapsedMs);
        }
        GUILayout.EndArea();
    }
    private static string Status(MissionStatus status) => status switch { MissionStatus.Locked => "시간 대기", MissionStatus.Active => "미해결", _ => "해결 완료" };
    private static string Describe(SessionPhase phase) => phase switch
    {
        SessionPhase.LearningPatrol => "초기 순찰 · 23시까지 CCTV실 복귀",
        SessionPhase.ControlRoom => "23:30에 CCTV 확인 후 순찰 시작",
        SessionPhase.AnomalyPatrol => "이상현상 해결 중 · CCTV실 복귀 잠김",
        SessionPhase.AwaitFinalReturn => "모든 미션 완료 · CCTV실 최종 복귀",
        SessionPhase.RoundComplete => "회차 완료 · 추가 순찰 금지",
        SessionPhase.GameClear => "게임 클리어",
        SessionPhase.GameOver => "게임오버",
        _ => "설정 오류 · Console 확인"
    };
    private void OnDestroy() { if (font != null) Destroy(font); }
}
