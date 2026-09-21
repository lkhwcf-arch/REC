using System;
using System.Collections.Generic;

namespace REC.Core
{
    // 회차/시간/미션의 단일 상태 소유자. 입력 어댑터는 요청만 전달합니다.
    public class GameSession
    {
        private readonly DataManager data;
        private readonly MissionPlanner planner;
        private readonly AnomalyRuntime anomalies;
        private readonly SessionRules rules;
        private List<MissionRuntime> missions = new();
        private IReadOnlyList<MissionRuntime> readOnlyMissions;
        private long nextOccurrenceId;
        private int nextOpening;
        private bool observed, intermediateReturned, started;
        public int RoundId { get; private set; }
        public long ElapsedMs { get; private set; }
        public SessionPhase Phase { get; private set; } = SessionPhase.LearningPatrol;
        public string Error { get; private set; }
        public IReadOnlyList<MissionRuntime> Missions => readOnlyMissions;
        public SessionRules Rules => rules;
        public int DisplayMinute => (rules.StartMinute + (int)(ElapsedMs / rules.MillisecondsPerMinute)) % 1440;
        public bool IsTerminal => Phase is SessionPhase.GameClear or SessionPhase.GameOver or SessionPhase.ConfigurationError;
        public bool CanPatrol => Phase is SessionPhase.LearningPatrol or SessionPhase.AnomalyPatrol or SessionPhase.AwaitFinalReturn;
        public bool CanEnterRoom => Phase is SessionPhase.LearningPatrol or SessionPhase.ControlRoom or SessionPhase.AwaitFinalReturn;
        public bool IsReturnOverdue => !intermediateReturned && ElapsedMs >= rules.IntermediateReturnMs;
        public event Action<SessionEvent> Changed;

        public GameSession(DataManager data, MissionPlanner planner, AnomalyRuntime anomalies, SessionRules rules)
        {
            this.data = data; this.planner = planner; this.anomalies = anomalies; this.rules = rules;
            readOnlyMissions = missions.AsReadOnly();
        }
        public bool Start(int firstRoundId)
        {
            if (started) return false;
            started = true;
            try
            {
                // 미완성 맵은 사전 연결 오류로 보고합니다. 후보를 몰래 제외하여 추첨 확률을 바꾸지 않습니다.
                foreach (var quest in data.GetAllData<QuestData>())
                {
                    if (quest.Enabled == 0) continue;
                    if (quest.RealTime < rules.FirstObservationMs || quest.RealTime >= rules.DeadlineMs) throw new InvalidOperationException($"Quest={quest.ID}: 오픈 시각이 첫 사건~마감 전 범위를 벗어났습니다.");
                    anomalies.Validate(planner.Create(quest, 0));
                }
                anomalies.Capture(); BeginRound(firstRoundId); return true;
            }
            catch (Exception exception) { Fault(exception); return false; }
        }
        private void BeginRound(int roundId)
        {
            data.GetData<RoundData>(roundId);
            List<MissionRuntime> allocated = planner.Allocate(roundId, ref nextOccurrenceId);
            anomalies.Reset(); missions = allocated; readOnlyMissions = missions.AsReadOnly();
            RoundId = roundId; ElapsedMs = 0; nextOpening = 0; observed = false; intermediateReturned = false;
            Phase = SessionPhase.LearningPatrol; Publish("RoundStarted");
        }
        public void Tick(long elapsedMilliseconds)
        {
            if (!started || elapsedMilliseconds < 0 || IsTerminal || Phase == SessionPhase.RoundComplete) return;
            long remaining = rules.DeadlineMs - ElapsedMs;
            ElapsedMs += Math.Min(elapsedMilliseconds, remaining);
            try
            {
                while (nextOpening < missions.Count && missions[nextOpening].OpensAtMs <= ElapsedMs)
                {
                    MissionRuntime mission = missions[nextOpening];
                    anomalies.Activate(mission); nextOpening++; Publish("MissionOpened", mission);
                }
                if (ElapsedMs >= rules.DeadlineMs && !AllResolved())
                {
                    Phase = SessionPhase.GameOver; anomalies.Freeze(); Publish("GameOver");
                }
            }
            catch (Exception exception) { Fault(exception); }
        }
        public bool RequestEnterRoom()
        {
            if (!started || !CanEnterRoom) return false;
            if (Phase == SessionPhase.AwaitFinalReturn)
            {
                Phase = data.GetData<RoundData>(RoundId).NextRoundID == 0 ? SessionPhase.GameClear : SessionPhase.RoundComplete;
                anomalies.Freeze(); Publish(Phase == SessionPhase.GameClear ? "GameClear" : "RoundCompleted"); return true;
            }
            if (Phase == SessionPhase.ControlRoom) return true;
            intermediateReturned = true; Phase = SessionPhase.ControlRoom; Publish("IntermediateReturned"); return true;
        }
        public bool RequestObserveCctv()
        {
            if (Phase != SessionPhase.ControlRoom || ElapsedMs < rules.FirstObservationMs || observed) return false;
            observed = true; Publish("CctvObserved"); return true;
        }
        public bool RequestLeaveRoom()
        {
            if (Phase != SessionPhase.ControlRoom || !observed) return false;
            Phase = SessionPhase.AnomalyPatrol; Publish("PatrolStarted"); return true;
        }
        public bool RequestResolve(long occurrenceId)
        {
            if (Phase != SessionPhase.AnomalyPatrol || ElapsedMs >= rules.DeadlineMs) return false;
            foreach (MissionRuntime mission in missions)
            {
                if (mission.OccurrenceId != occurrenceId || mission.Status != MissionStatus.Active) continue;
                try
                {
                    anomalies.Resolve(mission);
                    if (AllResolved()) Phase = SessionPhase.AwaitFinalReturn;
                    Publish("MissionResolved", mission);
                    if (Phase == SessionPhase.AwaitFinalReturn) Publish("AllMissionsResolved");
                    return true;
                }
                catch (Exception exception) { Fault(exception); return false; }
            }
            return false;
        }
        public long GetActiveOccurrence(int targetId)
        {
            foreach (MissionRuntime mission in missions)
                if (mission.TargetId == targetId && mission.Status == MissionStatus.Active) return mission.OccurrenceId;
            return 0;
        }
        public bool RequestSkip()
        {
            if (!started || IsTerminal || Phase is SessionPhase.RoundComplete or SessionPhase.AwaitFinalReturn) return false;
            long destination;
            if (Phase == SessionPhase.ControlRoom && ElapsedMs < rules.FirstObservationMs) destination = rules.FirstObservationMs;
            else if (Phase == SessionPhase.AnomalyPatrol && nextOpening < missions.Count) destination = missions[nextOpening].OpensAtMs;
            else return false;
            Tick(destination - ElapsedMs); Publish("TimeSkipped"); return !IsTerminal;
        }
        public bool RequestNextRound()
        {
            if (Phase != SessionPhase.RoundComplete) return false;
            try { BeginRound(data.GetData<RoundData>(RoundId).NextRoundID); return true; }
            catch (Exception exception) { Fault(exception); return false; }
        }
        public void Stop(bool restoreMap = true) { if (started && restoreMap) anomalies.Reset(); started = false; }
        private bool AllResolved()
        {
            foreach (MissionRuntime mission in missions) if (mission.Status != MissionStatus.Resolved) return false;
            return missions.Count > 0;
        }
        private void Fault(Exception exception)
        {
            Error = exception.Message; Phase = SessionPhase.ConfigurationError; anomalies.Freeze(); Publish("ConfigurationError");
        }
        private void Publish(string kind, MissionRuntime mission = null) => Changed?.Invoke(new SessionEvent(kind, RoundId, ElapsedMs, mission));
    }
}
