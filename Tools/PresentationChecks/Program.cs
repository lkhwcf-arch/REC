using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using REC.Core;

internal static class Program
{
    private static int checks;
    private static void Check(bool value, string message)
    {
        checks++;
        if (!value) throw new Exception(message);
    }
    private static DataManager Load()
    {
        var data = new DataManager();
        Add<AnomalyData>(data, "Anomaly"); Add<DetailData>(data, "Detail");
        Add<MovementParameterData>(data, "MovementParameter"); Add<TargetCodeData>(data, "TargetCodes");
        Add<QuestData>(data, "Quest"); Add<ScheduleData>(data, "Schedule"); Add<RoundData>(data, "Round"); Add<DoorData>(data, "Door");
        var errors = GameDataValidator.Validate(data);
        Check(errors.Count == 0, string.Join("\n", errors));
        data.Seal(); return data;
    }
    private static void Add<T>(DataManager data, string name) where T : class, ICSVData, new()
    {
        string csv = File.ReadAllText(Path.Combine("Assets/GameData/CSV", name + ".csv"));
        data.AddTable(CsvMapper.ConvertRows<T>(CSVParser.Read(csv, name), name), name);
    }
    private static GameSession Create(DataManager data, int seed, int endingQuest)
    {
        var bodies = data.GetAllData<TargetCodeData>().ToDictionary(t => t.ID, t => (IAnomalyBody)new Body(t.ID));
        return new GameSession(data, new MissionPlanner(data, new SeededRandom(seed)), new AnomalyRuntime(bodies, new ActionCatalog()), new SessionRules(), endingQuest);
    }
    private static void Patrol(GameSession session)
    {
        Check(session.RequestEnterRoom(), "중간 복귀");
        Check(session.RequestSkip(), "관측 시각으로 진행");
        Check(session.RequestObserveCctv() && session.RequestLeaveRoom(), "관측 후 순찰");
    }
    private static void Main()
    {
        var data = Load();
        // CSV를 바꾸지 않고, 실제 배정된 마지막 회차 미션을 엔딩 미션으로 주입해 계약을 검증합니다.
        for (int seed = 0; seed < 20; seed++)
        {
            long occurrence = 0;
            var allocation = new MissionPlanner(data, new SeededRandom(seed)).Allocate(4, ref occurrence);
            int endingQuest = allocation[0].QuestId;
            var session = Create(data, seed, endingQuest);
            var events = new List<string>(); session.Changed += e => events.Add(e.Kind);
            Check(!session.RequestCompleteEnding(), "시작 전 엔딩 완료 거부");
            Check(session.Start(4), session.Error);
            Patrol(session);
            var ending = session.Missions.Single(m => m.QuestId == endingQuest);
            Check(!session.CanResolve(ending) && !session.RequestResolve(ending.OccurrenceId), "잠금 미션이 남으면 영정사진 해결 거부");
            while (session.Missions.Any(m => m.Status == MissionStatus.Locked)) Check(session.RequestSkip(), "나머지 미션 오픈");
            Check(!session.RequestResolve(ending.OccurrenceId), "다른 활성 미션이 남아도 엔딩 해결 거부");
            foreach (var mission in session.Missions.Where(m => m != ending)) Check(session.RequestResolve(mission.OccurrenceId), "다른 미션 해결");
            Check(session.CanResolve(ending) && session.RequestResolve(ending.OccurrenceId), "마지막 영정사진 해결");
            Check(session.Phase == SessionPhase.Ending && !session.CanPatrol && !session.CanEnterRoom, "엔딩 전용 상태");
            Check(events[^2] == "MissionResolved" && events[^1] == "EndingStarted", "완료 결과 뒤에 엔딩 시작 이벤트");
            Check(!session.RequestResolve(ending.OccurrenceId), "엔딩 도중 중복 해결 거부");
            Check(!session.RequestSkip() && !session.RequestEnterRoom() && !session.RequestNextRound(), "엔딩 도중 흐름 변경 거부");
            long before = session.ElapsedMs;
            session.Tick(session.Rules.DeadlineMs * 2);
            Check(session.ElapsedMs == before && session.Phase == SessionPhase.Ending, "영상 동안 시간/게임오버 정지");
            Check(session.RequestCompleteEnding() && session.Phase == SessionPhase.GameClear, "최종 복귀 없이 영상 종료로 클리어");
            Check(!session.RequestCompleteEnding() && events.Count(e => e == "GameClear") == 1, "중복 영상 완료 무시");
        }
        // 비활성 Quest 28을 등록해도 미션을 새로 배정하거나 정상 최종 복귀를 막으면 안 됩니다.
        var disabled = Create(data, 1709, 28);
        Check(disabled.Start(4), disabled.Error);
        Check(disabled.Missions.All(m => m.QuestId != 28), "비활성 엔딩 미션 유지");
        Patrol(disabled);
        while (disabled.Missions.Any(m => m.Status == MissionStatus.Locked)) Check(disabled.RequestSkip(), "기존 일정 유지");
        foreach (var mission in disabled.Missions) Check(disabled.RequestResolve(mission.OccurrenceId), "기존 미션 해결");
        Check(disabled.Phase == SessionPhase.AwaitFinalReturn && disabled.RequestEnterRoom(), "엔딩 미션 미배정 시 기존 최종 복귀 유지");
        Check(disabled.Phase == SessionPhase.GameClear, "기존 클리어 경로 유지");
        Console.WriteLine($"PASS: {checks} presentation domain assertions; CSV unchanged, Unity play test not included.");
    }
    private sealed class Body : IAnomalyBody
    {
        public int TargetId { get; }
        public Body(int id) { TargetId = id; }
        public bool Supports(string action, MovementSpec movement, out string error) { error = null; return true; }
        public void CaptureBaseline() { }
        public void RestoreBaseline() { }
        public void SetVisible(bool value) { }
        public void Move(MovementSpec movement) { }
        public void SetShadow(bool value) { }
        public void SetReplacement(bool value) { }
        public void SetActionable(bool value) { }
    }
}
