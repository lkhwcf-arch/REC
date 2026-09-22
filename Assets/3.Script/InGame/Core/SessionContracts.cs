using System;
using System.Collections.Generic;

namespace REC.Core
{
    public enum SessionPhase
    {
        LearningPatrol,
        ControlRoom,
        AnomalyPatrol,
        AwaitFinalReturn,
        RoundComplete, GameClear,
        GameOver,
        ConfigurationError,
        Ending
    }
    public enum MissionStatus
    {
        Locked,
        Active,
        Resolved
    }
    public enum MovementKind
    {
        Offset,
        Rotation,
        GroupPosition
    }
    public enum MovementAxis
    { X, Y, Z }

    public readonly struct MovementSpec
    {
        public readonly MovementKind Kind;
        public readonly MovementAxis Axis;
        public readonly float Value;
        public MovementSpec(MovementKind kind, MovementAxis axis, float value) { Kind = kind; Axis = axis; Value = value; }
    }

    // Unity 오브젝트 참조는 어댑터 내부에만 보관합니다. 명령 경계에서는 ID를 사용합니다.
    public interface IAnomalyBody
    {
        int TargetId { get; }
        bool Supports(string action, MovementSpec movement, out string error);
        void CaptureBaseline();
        void RestoreBaseline();
        void SetVisible(bool visible);
        void Move(MovementSpec movement);
        void SetShadow(bool visible);
        void SetReplacement(bool enabled);
        void SetActionable(bool enabled);
    }

    public interface IRandomSource { int Next(int exclusiveMax); }
    public class SeededRandom : IRandomSource
    {
        private readonly Random random;
        public SeededRandom(int seed) { random = new Random(seed); }
        public int Next(int exclusiveMax) => random.Next(exclusiveMax);
    }

    public class SessionRules
    {
        public long MillisecondsPerMinute { get; }
        public int StartMinute { get; }
        public long IntermediateReturnMs { get; }
        public long FirstObservationMs { get; }
        public long DeadlineMs { get; }
        public SessionRules(long millisecondsPerMinute = 2500, int startMinute = 1320, int returnMinute = 60, int firstMinute = 90, int durationMinutes = 480)
        {
            if (millisecondsPerMinute <= 0 || startMinute < 0 || startMinute >= 1440 || returnMinute <= 0 || firstMinute < returnMinute || durationMinutes <= firstMinute)
                throw new ArgumentException("시간 설정 범위가 올바르지 않습니다.");
            MillisecondsPerMinute = millisecondsPerMinute; StartMinute = startMinute;
            IntermediateReturnMs = checked(returnMinute * millisecondsPerMinute);
            FirstObservationMs = checked(firstMinute * millisecondsPerMinute);
            DeadlineMs = checked(durationMinutes * millisecondsPerMinute);
        }
    }

    public class MissionRuntime
    {
        public long OccurrenceId { get; }
        public int QuestId { get; }
        public int AnomalyId { get; }
        public int TargetId { get; }
        public int DirectionGroupId { get; }
        public long OpensAtMs { get; }
        public string BeginAction { get; }
        public string ResolveAction { get; }
        public MovementSpec Movement { get; }
        public MissionStatus Status { get; internal set; }
        public MissionRuntime(long occurrenceId, QuestData quest, AnomalyData anomaly, DetailData detail, MovementSpec movement)
        {
            OccurrenceId = occurrenceId; QuestId = quest.ID; AnomalyId = anomaly.ID; TargetId = detail.TargetID;
            DirectionGroupId = quest.DirectionGroupID; OpensAtMs = quest.RealTime;
            BeginAction = anomaly.BeginMethod; ResolveAction = anomaly.ResolveMethod; Movement = movement;
        }
    }

    public readonly struct SessionEvent
    {
        public readonly string Kind;
        public readonly int RoundId, QuestId, TargetId, DirectionGroupId;
        public readonly long OccurrenceId, ElapsedMs;
        public SessionEvent(string kind, int roundId, long elapsedMs, MissionRuntime mission = null)
        {
            Kind = kind; RoundId = roundId; ElapsedMs = elapsedMs;
            QuestId = mission?.QuestId ?? 0; TargetId = mission?.TargetId ?? 0;
            DirectionGroupId = mission?.DirectionGroupId ?? 0; OccurrenceId = mission?.OccurrenceId ?? 0;
        }
    }
}
