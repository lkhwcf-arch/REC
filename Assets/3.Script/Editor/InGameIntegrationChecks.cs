#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using System.Text;
using REC.Core;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// 별도 PreviewScene에서 실제 씬/프리팹을 검사합니다. 열려 있는 작업 씬은 저장하거나 변경하지 않습니다.
public static class InGameIntegrationChecks
{
    [MenuItem("Tools/REC/인게임 연결 자동 검증")]
    public static void Run()
    {
        StringBuilder report = new(); int checks = 0;
        void Check(bool condition, string message) { if (!condition) throw new Exception(message); checks++; report.AppendLine("PASS " + message); }
        var scene = EditorSceneManager.OpenPreviewScene("Assets/1.Scene/InGame.unity");
        var oldCursor = Cursor.lockState; bool oldVisible = Cursor.visible;
        try
        {
            var roots = scene.GetRootGameObjects();
            T Find<T>() where T : Component => roots.SelectMany(g => g.GetComponentsInChildren<T>(true)).Single();
            var data = Find<GameDataBootstrapper>(); data.SendMessage("Awake"); Check(data.IsLoaded, "CSV 8개 로드/참조 검증");
            var bootstrap = Find<InGameSceneBootstrapper>();
            var cameraManager = Find<CameraManager>(); cameraManager.SendMessage("Awake");
            bootstrap.SendMessage("Awake"); bootstrap.Initialize();
            Check(bootstrap.Error == null, "실제 InGame 초기화: " + bootstrap.Error);
            var session = bootstrap.Host.Session;
            Check(session.Phase == SessionPhase.LearningPatrol && cameraManager.IsPlayerView, "플레이어 시점 시작");
            var adapters = roots.SelectMany(g => g.GetComponentsInChildren<AnomalyTargetAdapter>(true)).ToArray();
            var doors = roots.SelectMany(g => g.GetComponentsInChildren<DoorInteraction>(true)).ToArray();
            Check(adapters.SelectMany(a => a.TargetIds).Distinct().Count() == 15, "활성 TargetID 15개 연결");
            Check(doors.Length == 6, "CSV 일반 문 6개 연결");
            foreach (var node in roots.SelectMany(g => g.GetComponentsInChildren<Transform>(true)).Where(t => t.name is "I_7_1" or "I_7_3"))
                Check(node.GetComponentInParent<Interact>() == null, node.name + " 상호작용 제외");
            foreach (var door in doors)
            {
                var initial = door.transform.rotation; door.GetComponent<Interact>().Click(); door.Tick(1);
                Check(Quaternion.Angle(initial, door.transform.rotation) > 80, $"문 {door.DoorId} 클릭/Y축 열림");
                door.ResetForRound(); Check(Quaternion.Angle(initial, door.transform.rotation) < 0.01f, $"문 {door.DoorId} 초기화");
            }
            var player = Find<PlayerController>();
            report.AppendLine($"SPAWN {player.transform.position:F3}");
            Physics.SyncTransforms();
            Check(Physics.Raycast(player.transform.position + Vector3.up * 0.1f, Vector3.down, out var ground, 1, ~LayerMask.GetMask("Player", "InteractionArea"), QueryTriggerInteraction.Ignore), "시작 위치 아래 물리 바닥");
            report.AppendLine("GROUND " + ground.collider.name);
            for (int round = 1; round <= 4; round++)
            {
                Check(session.RoundId == round, $"{round}회차 시작");
                session.Tick(75000); // 22:30 복귀와 23:30 관측 조건을 구분합니다.
                Check(session.RequestEnterRoom(), "22:30 중간 복귀");
                var screen = (GameObject)new SerializedObject(cameraManager).FindProperty("CCTVScreenPannel").objectReferenceValue;
                Check(cameraManager.CurrentMode == CameraMode.CCTV && screen != null && screen.activeInHierarchy, "부모 Canvas를 포함한 CCTV 화면 활성화");
                Check(!session.RequestObserveCctv(), "22:30 CCTV 확인은 아직 불가");
                Check(!session.RequestLeaveRoom(), "CCTV 확인 전 순찰 차단");
                Check(session.RequestSkip() && session.RequestObserveCctv() && session.RequestLeaveRoom(), "23:30 확인 후 순찰");
                Check(!session.RequestEnterRoom(), "미해결 중 복귀 차단");
                while (session.Missions.Any(m => m.Status == MissionStatus.Locked)) Check(session.RequestSkip(), "다음 일정 오픈");
                foreach (var mission in session.Missions.ToArray())
                {
                    var adapter = adapters.Single(a => a.TargetIds.Contains(mission.TargetId));
                    Check(adapter.Actionable, $"Target={mission.TargetId} 활성 상호작용");
                    adapter.GetComponent<Interact>().Hold();
                }
                Check(session.Phase == SessionPhase.AwaitFinalReturn, "모든 미션 해결");
                Check(session.RequestEnterRoom(), "최종 복귀");
                Check(!session.CanPatrol, "최종 복귀 후 순찰 차단");
                if (round < 4) Check(session.RequestNextRound(), "맵 초기화/다음 회차");
            }
            Check(session.Phase == SessionPhase.GameClear, "4회차 클리어");
            report.AppendLine($"COMPLETE {checks} checks. PreviewScene engine check; keyboard/rendering play test is separate.");
        }
        catch (Exception e) { report.AppendLine("FAIL " + e); Debug.LogException(e); }
        finally
        {
            EditorSceneManager.ClosePreviewScene(scene); Cursor.lockState = oldCursor; Cursor.visible = oldVisible;
            Directory.CreateDirectory("Temp"); File.WriteAllText("Temp/InGameIntegrationResult.txt", report.ToString());
        }
    }
    public static void RunBatch()
    {
        Run();
        EditorApplication.Exit(File.ReadAllText("Temp/InGameIntegrationResult.txt").Contains("COMPLETE ") ? 0 : 1);
    }
}
#endif
