using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using REC.Core;

class Program
{
    private static int checks;
    static void Assert(bool condition, string description) { checks++; if (!condition) throw new Exception(description); }
    static DataManager Load()
    {
        var data = new DataManager();
        Add<AnomalyData>(data, "Anomaly"); Add<DetailData>(data, "Detail"); Add<MovementParameterData>(data, "MovementParameter");
        Add<TargetCodeData>(data, "TargetCodes"); Add<QuestData>(data, "Quest"); Add<ScheduleData>(data, "Schedule"); Add<RoundData>(data, "Round");
        var errors = GameDataValidator.Validate(data); Assert(errors.Count == 0, string.Join("\n", errors)); data.Seal(); return data;
    }
    static void Add<T>(DataManager data, string name) where T : class, ICSVData, new()
    {
        string text = File.ReadAllText(Path.Combine("Assets", "GameData", "CSV", name + ".csv"));
        data.AddTable(CsvMapper.ConvertRows<T>(CSVParser.Read(text, name), name), name);
    }
    static Dictionary<int, IAnomalyBody> Bodies(DataManager data) => data.GetAllData<TargetCodeData>().ToDictionary(t => t.ID, t => (IAnomalyBody)new Body(t.ID));
    static GameSession Session(DataManager data, int seed, Dictionary<int, IAnomalyBody> bodies = null)
    {
        return new GameSession(data, new MissionPlanner(data, new SeededRandom(seed), 0.01f), new AnomalyRuntime(bodies ?? Bodies(data), new ActionCatalog()), new SessionRules());
    }
    static void Main()
    {
        var data = Load();
        var rules = new SessionRules();
        Assert(rules.FirstObservationMs == 225000 && rules.DeadlineMs == 1200000, "실제 ms/게임 분 변환");
        for (int seed = 0; seed < 100; seed++)
        {
            var session = Session(data, seed); Assert(session.Start(1), session.Error);
            HashSet<long> ids = new();
            for (int round = 1; round <= 4; round++)
            {
                Assert(session.RoundId == round && session.ElapsedMs == 0, "회차 시작 초기화");
                Assert(session.Missions.Count == 3 && session.Missions.All(m => m.Status == MissionStatus.Locked), "선배정/잠금");
                foreach (var mission in session.Missions) Assert(ids.Add(mission.OccurrenceId), "회차 간 사건 ID 유일");
                Assert(!session.RequestResolve(session.Missions[0].OccurrenceId), "오픈 전 해결 금지");
                Assert(session.RequestEnterRoom(), "중간 복귀");
                Assert(!session.RequestLeaveRoom() && !session.RequestObserveCctv(), "23:30 전 순찰/관찰 금지");
                Assert(session.RequestSkip() && session.ElapsedMs == 225000 && session.DisplayMinute == 1410, "첫 사건 시간 건너뛰기");
                Assert(session.Missions.Count(m => m.Status == MissionStatus.Active) == 1, "첫 사건 한 번 오픈");
                Assert(session.RequestObserveCctv() && session.RequestLeaveRoom(), "관찰 후 순찰");
                Assert(!session.RequestEnterRoom() && !session.CanEnterRoom, "순찰 중 복귀 금지");
                Assert(session.RequestSkip(), "두 번째 일정");
                Assert(session.Missions.Count(m => m.Status == MissionStatus.Active) == 2, "이전 미해결 유지");
                Assert(session.RequestSkip(), "세 번째 일정");
                foreach (var mission in session.Missions)
                {
                    Assert(session.RequestResolve(mission.OccurrenceId), "해결 명령");
                    Assert(!session.RequestResolve(mission.OccurrenceId), "중복 해결 거부");
                }
                Assert(session.Phase == SessionPhase.AwaitFinalReturn, "모든 미션 후 최종 복귀 대기");
                Assert(session.RequestEnterRoom(), "최종 복귀");
                Assert(!session.CanPatrol && !session.RequestLeaveRoom(), "최종 복귀 후 순찰 금지");
                if (round < 4) Assert(session.RequestNextRound(), "다음 회차");
                else Assert(session.Phase == SessionPhase.GameClear && !session.RequestNextRound(), "최종 클리어");
            }
            session.Stop();
        }
        var failed = Session(data, 1); failed.Start(1); failed.Tick(150000);
        Assert(failed.IsReturnOverdue && failed.Phase != SessionPhase.GameOver, "23시 미복귀는 추가 패배 조건 아님");
        failed.Tick(1050000); Assert(failed.Phase == SessionPhase.GameOver && failed.ElapsedMs == 1200000, "06시 미완료 실패");
        Assert(!failed.RequestResolve(failed.Missions[0].OccurrenceId) && !failed.RequestEnterRoom(), "게임오버 이후 명령 금지");
        var early = Session(data, 7); early.Start(1); early.RequestEnterRoom(); early.RequestSkip(); early.RequestObserveCctv(); early.RequestLeaveRoom();
        Assert(early.RequestResolve(early.Missions[0].OccurrenceId), "첫 미션만 해결");
        Assert(early.Phase == SessionPhase.AnomalyPatrol && !early.RequestEnterRoom(), "미래 잠금 미션이 있으면 조기 최종 복귀 금지");
        early.Tick(early.Rules.DeadlineMs); Assert(early.Phase == SessionPhase.GameOver, "일부만 해결한 상태의 마감");
        var completeLate = Session(data, 8); completeLate.Start(1); completeLate.RequestEnterRoom(); completeLate.RequestSkip(); completeLate.RequestObserveCctv(); completeLate.RequestLeaveRoom();
        completeLate.RequestSkip(); completeLate.RequestSkip();
        foreach (var mission in completeLate.Missions) completeLate.RequestResolve(mission.OccurrenceId);
        completeLate.Tick(completeLate.Rules.DeadlineMs);
        Assert(completeLate.Phase == SessionPhase.AwaitFinalReturn && completeLate.RequestEnterRoom(), "마감 전 전부 해결했다면 추가 복귀 실패 조건 없음");
        var missing = Session(data, 1, new Dictionary<int, IAnomalyBody>());
        Assert(!missing.Start(1) && missing.Phase == SessionPhase.ConfigurationError, "미완성 사물 연결은 설정 오류");
        var sourceA = new MissionPlanner(data, new SeededRandom(42), 0.01f); var sourceB = new MissionPlanner(data, new SeededRandom(42), 0.01f);
        long idA = 0, idB = 0;
        Assert(sourceA.Allocate(1, ref idA).Select(m => m.QuestId).SequenceEqual(sourceB.Allocate(1, ref idB).Select(m => m.QuestId)), "주입 난수 시드 재현");
        bool unitError = false;
        var untypedMovement = data.GetData<MovementParameterData>(2); string savedUnit = untypedMovement.Unit; untypedMovement.Unit = "";
        try { new MissionPlanner(data, new SeededRandom(1)).Create(data.GetData<QuestData>(3), 1); } catch (InvalidOperationException) { unitError = true; }
        untypedMovement.Unit = savedUnit;
        Assert(unitError, "명시하지 않은 이동 단위 거부");
        var bodies = Bodies(data); var runtime = new AnomalyRuntime(bodies, new ActionCatalog()); runtime.Capture();
        var move = sourceA.Create(data.GetData<QuestData>(25), 10001); // Target 17, Y 이동
        var deletion = sourceA.Create(data.GetData<QuestData>(12), 10002); // 같은 Target 17 소실
        runtime.Activate(move); runtime.Activate(deletion);
        Body body = (Body)bodies[17]; Assert(!body.Visible && Math.Abs(body.Offset - 0.5f) < 0.001f, "같은 사물 복합 사건");
        runtime.Resolve(move); Assert(!body.Visible && body.Offset == 0 && body.Actionable, "이동 해결 후 소실 유지");
        runtime.Resolve(deletion); Assert(body.Visible && !body.Actionable, "마지막 사건 해결 후 정상화");
        runtime.Reset(); Assert(bodies.Values.Cast<Body>().All(b => b.Visible && b.Offset == 0 && !b.Actionable), "맵 전체 기준 상태 초기화");
        var reverseBodies = Bodies(data); var reverseRuntime = new AnomalyRuntime(reverseBodies, new ActionCatalog()); reverseRuntime.Capture();
        var repeatedA = sourceA.Create(data.GetData<QuestData>(12), 10003); var repeatedB = sourceA.Create(data.GetData<QuestData>(12), 10004);
        reverseRuntime.Activate(repeatedA); reverseRuntime.Activate(repeatedB); reverseRuntime.Resolve(repeatedA);
        Assert(!((Body)reverseBodies[17]).Visible && ((Body)reverseBodies[17]).Actionable, "같은 소실이 중첩되어도 한 사건만 해결");
        reverseRuntime.Resolve(repeatedB); Assert(((Body)reverseBodies[17]).Visible, "같은 소실의 마지막 사건 정상화");
        Console.WriteLine($"PASS: {checks} assertions (100 seeds × 4 rounds, real CSV, overlap, deadlines, unit validation)");
    }
    class Body : IAnomalyBody
    {
        public int TargetId { get; }
        public bool Visible = true, Actionable;
        public float Offset;
        public Body(int id) { TargetId = id; }
        public bool Supports(string action, MovementSpec movement, out string error) { error = null; return true; }
        public void CaptureBaseline() { }
        public void RestoreBaseline() { Visible = true; Offset = 0; }
        public void SetVisible(bool value) => Visible = value;
        public void Move(MovementSpec movement) => Offset += movement.Value;
        public void SetShadow(bool value) { }
        public void SetReplacement(bool value) { }
        public void SetActionable(bool value) => Actionable = value;
    }
}
