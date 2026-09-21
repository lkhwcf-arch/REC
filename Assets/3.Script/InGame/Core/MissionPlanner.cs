using System;
using System.Collections.Generic;
using System.Linq;

namespace REC.Core
{
    public class MissionPlanner
    {
        private readonly DataManager data;
        private readonly IRandomSource random;

        public MissionPlanner(DataManager data, IRandomSource random)
        {
            this.data = data ?? throw new ArgumentNullException(nameof(data));
            this.random = random ?? throw new ArgumentNullException(nameof(random));
        }
        public List<MissionRuntime> Allocate(int roundId, ref long nextOccurrenceId)
        {
            List<MissionRuntime> result = new();
            var candidatesBySchedule = data.GetAllData<QuestData>().Where(q => q.Enabled == 1 && q.RoundID == roundId).ToLookup(q => q.ScheduleID);
            foreach (var schedule in data.GetAllData<ScheduleData>().Where(s => s.Enabled == 1 && s.RoundID == roundId).OrderBy(s => s.SlotOrder))
            {
                List<QuestData> candidates = candidatesBySchedule[schedule.ID].OrderBy(q => q.ID).ToList();
                if (schedule.DrawCount <= 0 || schedule.DrawCount > candidates.Count) throw new InvalidOperationException($"Schedule={schedule.ID}: 추첨 수가 잘못되었습니다.");
                for (int draw = 0; draw < schedule.DrawCount; draw++)
                {
                    int selected = 0;
                    if (candidates[0].SpawnType == 1)
                    {
                        long total = 0;
                        foreach (var candidate in candidates) total += candidate.Weight;
                        if (total <= 0 || total > int.MaxValue) throw new InvalidOperationException("가중치 합계가 지원 범위를 벗어났습니다.");
                        int value = random.Next((int)total);
                        for (; selected < candidates.Count - 1; selected++) { value -= candidates[selected].Weight; if (value < 0) break; }
                    }
                    QuestData quest = candidates[selected]; candidates.RemoveAt(selected);
                    result.Add(Create(quest, checked(++nextOccurrenceId)));
                }
            }
            if (result.Count == 0) throw new InvalidOperationException($"Round={roundId}: 배정할 미션이 없습니다.");
            result.Sort((a, b) => { int time = a.OpensAtMs.CompareTo(b.OpensAtMs); return time != 0 ? time : a.QuestId.CompareTo(b.QuestId); });
            return result;
        }
        public MissionRuntime Create(QuestData quest, long occurrenceId)
        {
            var anomaly = data.GetData<AnomalyData>(quest.AnomalyID);
            var detail = data.GetData<DetailData>(anomaly.MainDetailIDs);
            if (detail.AnomalyID != anomaly.ID || detail.Phenomenon != anomaly.BeginMethod || detail.Resolve != anomaly.ResolveMethod)
                throw new InvalidOperationException($"Anomaly={anomaly.ID}: 상세 소속 또는 행동 이름이 일치하지 않습니다.");

            MovementSpec movement = default;

            if (anomaly.BeginMethod is "ObjectMovement" or "ObjectGroupMovement")
            {
                var row = data.GetData<MovementParameterData>(detail.MoveParamID);
                if (row.TargetID != detail.TargetID) throw new InvalidOperationException($"이동 설정 {row.ID}의 대상이 다릅니다.");
                if (!Enum.TryParse(row.MoveType, out MovementKind kind) || !Enum.IsDefined(typeof(MovementKind), kind)) throw new InvalidOperationException($"이동 종류 '{row.MoveType}'를 지원하지 않습니다.");
                if (kind == MovementKind.GroupPosition) movement = new MovementSpec(kind, MovementAxis.X, 0);
                else
                {
                    if (!Enum.TryParse(row.Axis, out MovementAxis axis) || !Enum.IsDefined(typeof(MovementAxis), axis) || !row.Value.HasValue || float.IsNaN(row.Value.Value) || float.IsInfinity(row.Value.Value))
                        throw new InvalidOperationException($"이동 설정 {row.ID}의 축/값이 잘못되었습니다.");

                    string unit = row.Unit?.Trim() ?? "";
                    
                    float scale = kind == MovementKind.Rotation
                        ? (unit == "Degree" ? 1f : 0f)
                        : unit switch { "" or "Centimeter" => 0.01f, "Meter" => 1f, "Millimeter" => 0.001f, _ => 0f };

                    if (scale <= 0) 
                        throw new InvalidOperationException($"이동 설정 {row.ID}: 지원하지 않는 단위 '{row.Unit}'입니다.");
                    
                    movement = new MovementSpec(kind, axis, row.Value.Value * scale);
                }
            }
            return new MissionRuntime(occurrenceId, quest, anomaly, detail, movement);
        }
    }
}
