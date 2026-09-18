using System;
using System.Linq;
using System.Collections.Generic;

public static class GameDataValidator
{
    public static List<string> Validate(DataManager data)
    {
        if (data is null)
            throw new ArgumentNullException(nameof(data));

        List<string> errors = new();

        ValidateAnomalies(data, errors);
        ValidateDetails(data, errors);
        ValidateMovements(data, errors);
        ValidateQuests(data, errors);
        ValidateSchedules(data, errors);
        ValidateRounds(data, errors);
        ValidateScheduleRules(data, errors);
        ValidateRoundRules(data, errors);

        return errors;
    }

    private static void ValidateAnomalies(DataManager data, List<string> errors)
    {
        foreach (AnomalyData anomaly in data.GetAllData<AnomalyData>())
        {
            DetailData detail = Require<DetailData>(data, anomaly.MainDetailIDs, $"Anomaly ID={anomaly.ID}, MainDetailIDs", errors);

            if (detail is not null && detail.AnomalyID != anomaly.ID)
                errors.Add($"[Anomaly ID={anomaly.ID}] 상세 ID={detail.ID}의 AnomalyID가 {detail.AnomalyID}입니다. 소속이 일치하지 않습니다.");
        }
    }

    private static void ValidateDetails(DataManager data, List<string> errors)
    {
        foreach (DetailData detail in data.GetAllData<DetailData>())
        {
            Require<AnomalyData>(data, detail.AnomalyID, $"Detail ID={detail.ID}, AnomalyID", errors);
            Require<TargetCodeData>(data, detail.TargetID, $"Detail ID={detail.ID}, TargetID", errors);

            // 0은 이동 설정을 사용하지 않는다는 의미입니다.
            if (detail.MoveParamID == 0)
                continue;

            MovementParameterData movement = Require<MovementParameterData>(data, detail.MoveParamID, $"Detail ID={detail.ID}, MoveParamID", errors);

            if (movement is not null && movement.TargetID != detail.TargetID)
                errors.Add($"[Detail ID={detail.ID}] TargetID={detail.TargetID}와 이동 설정 ID={movement.ID}의 TargetID={movement.TargetID}가 다릅니다.");
        }
    }

    private static void ValidateMovements(DataManager data, List<string> errors)
    {
        foreach (MovementParameterData movement in data.GetAllData<MovementParameterData>())
            Require<TargetCodeData>(data, movement.TargetID, $"MovementParameter ID={movement.ID}, TargetID", errors);
    }

    private static void ValidateQuests(DataManager data, List<string> errors)
    {
        foreach (QuestData quest in data.GetAllData<QuestData>())
        {
            Require<RoundData>(data, quest.RoundID, $"Quest ID={quest.ID}, RoundID", errors);
            Require<AnomalyData>(data, quest.AnomalyID, $"Quest ID={quest.ID}, AnomalyID", errors);

            ScheduleData schedule = Require<ScheduleData>(data, quest.ScheduleID, $"Quest ID={quest.ID}, ScheduleID", errors);

            if (schedule is not null && schedule.RoundID != quest.RoundID)
                errors.Add($"[Quest ID={quest.ID}] RoundID={quest.RoundID}와 일정 ID={schedule.ID}의 RoundID={schedule.RoundID}가 다릅니다.");

            if (quest.RealTime < 0)
                errors.Add($"[Quest ID={quest.ID}] RealTime은 0 이상이어야 합니다. 현재 값: {quest.RealTime}");
        }
    }

    private static void ValidateSchedules(DataManager data, List<string> errors)
    {
        foreach (ScheduleData schedule in data.GetAllData<ScheduleData>())
            Require<RoundData>(data, schedule.RoundID, $"Schedule ID={schedule.ID}, RoundID", errors);
    }

    private static void ValidateRounds(DataManager data, List<string> errors)
    {
        foreach (RoundData round in data.GetAllData<RoundData>())
        {
            // 0은 다음 회차가 없다는 의미입니다.
            if (round.NextRoundID == 0)
                continue;

            Require<RoundData>(data, round.NextRoundID, $"Round ID={round.ID}, NextRoundID", errors);

            if (round.NextRoundID == round.ID)
                errors.Add($"[Round ID={round.ID}] 다음 회차가 자기 자신을 가리키고 있습니다.");
        }
    }

    private static void ValidateScheduleRules(DataManager data, List<string> errors)
    {
        var questsBySchedule = data.GetAllData<QuestData>().ToLookup(quest => quest.ScheduleID);

        foreach (ScheduleData schedule in data.GetAllData<ScheduleData>())
        {
            QuestData[] candidates = questsBySchedule[schedule.ID].ToArray();
            string source = $"[Schedule ID={schedule.ID}]";

            if (schedule.SlotOrder <= 0)
                errors.Add($"{source} SlotOrder는 1 이상이어야 합니다.");

            if (schedule.CandidateCount != candidates.Length)
                errors.Add($"{source} CandidateCount={schedule.CandidateCount}, 실제 후보 수={candidates.Length}로 다릅니다.");

            if (schedule.DrawCount <= 0 || schedule.DrawCount > candidates.Length)
                errors.Add($"{source} DrawCount={schedule.DrawCount}는 1 이상이며 실제 후보 수={candidates.Length} 이하여야 합니다.");

            long actualWeightSum = candidates.Sum(quest => (long)quest.Weight);

            if (schedule.WeightSum < 0 || schedule.WeightSum != actualWeightSum)
                errors.Add($"{source} WeightSum={schedule.WeightSum}, 실제 가중치 합계={actualWeightSum}로 올바르지 않습니다.");

            if (candidates.Length == 0)
                continue;

            long openTime = candidates[0].RealTime;
            int spawnType = candidates[0].SpawnType;

            foreach (QuestData quest in candidates)
            {
                if (quest.RealTime != openTime)
                    errors.Add($"{source} Quest ID={quest.ID}의 RealTime={quest.RealTime}이 같은 일정의 기준값={openTime}과 다릅니다.");

                if (quest.SpawnType is not (0 or 1))
                    errors.Add($"[Quest ID={quest.ID}] SpawnType은 0 또는 1이어야 합니다.");

                if (quest.SpawnType != spawnType)
                    errors.Add($"{source} 고정·랜덤 후보를 하나의 일정에 혼합할 수 없습니다.");

                if (quest.Weight < 0)
                    errors.Add($"[Quest ID={quest.ID}] Weight는 음수일 수 없습니다.");

                if (quest.SpawnType == 0 && quest.Weight != 0)
                    errors.Add($"[Quest ID={quest.ID}] 고정 후보의 Weight는 0이어야 합니다.");
            }

            if (spawnType == 0 && schedule.DrawCount != candidates.Length)
                errors.Add($"{source} 고정 일정은 모든 후보를 배정해야 하므로 DrawCount와 후보 수가 같아야 합니다.");

            if (spawnType == 1)
            {
                int selectableCount = candidates.Count(quest => quest.Weight > 0);

                if (schedule.DrawCount > selectableCount)
                    errors.Add($"{source} DrawCount={schedule.DrawCount}에 비해 양수 가중치 후보가 {selectableCount}개로 부족합니다.");
            }
        }
    }
    private static void ValidateRoundRules(DataManager data, List<string> errors)
    {
        var schedulesByRound = data.GetAllData<ScheduleData>().ToLookup(schedule => schedule.RoundID);
        var questsBySchedule = data.GetAllData<QuestData>().ToLookup(quest => quest.ScheduleID);

        foreach (RoundData round in data.GetAllData<RoundData>())
        {
            ScheduleData[] schedules = schedulesByRound[round.ID].ToArray();
            HashSet<int> slotOrders = new();
            long fixedCount = 0;
            long randomCount = 0;

            if (round.ScheduleCount <= 0 || round.ScheduleCount != schedules.Length)
                errors.Add($"[Round ID={round.ID}] ScheduleCount={round.ScheduleCount}, 실제 일정 수={schedules.Length}로 올바르지 않습니다.");

            if (round.FixedCount < 0 || round.RandomCount < 0)
                errors.Add($"[Round ID={round.ID}] FixedCount와 RandomCount는 음수일 수 없습니다.");

            foreach (ScheduleData schedule in schedules)
            {
                if (!slotOrders.Add(schedule.SlotOrder))
                    errors.Add($"[Round ID={round.ID}] SlotOrder={schedule.SlotOrder}가 중복되었습니다.");

                QuestData[] candidates = questsBySchedule[schedule.ID].ToArray();

                // 잘못된 일정의 세부 오류는 일정 검증에서 출력합니다.
                if (candidates.Length == 0 || schedule.DrawCount <= 0)
                    continue;

                if (candidates.All(quest => quest.SpawnType == 0))
                    fixedCount += schedule.DrawCount;
                else if (candidates.All(quest => quest.SpawnType == 1))
                    randomCount += schedule.DrawCount;
            }

            if (round.FixedCount != fixedCount)
                errors.Add($"[Round ID={round.ID}] FixedCount={round.FixedCount}, 일정 기준 고정 배정 수={fixedCount}로 다릅니다.");

            if (round.RandomCount != randomCount)
                errors.Add($"[Round ID={round.ID}] RandomCount={round.RandomCount}, 일정 기준 랜덤 배정 수={randomCount}로 다릅니다.");
        }

        ValidateRoundCycles(data, errors);
    }

    private static void ValidateRoundCycles(DataManager data, List<string> errors)
    {
        HashSet<int> checkedRounds = new();

        foreach (RoundData start in data.GetAllData<RoundData>())
        {
            HashSet<int> path = new();
            int currentId = start.ID;

            while (currentId != 0 && !checkedRounds.Contains(currentId))
            {
                if (!path.Add(currentId))
                {
                    errors.Add($"[Round ID={start.ID}] 다음 회차를 따라가면 ID={currentId}로 되돌아오는 순환이 있습니다.");
                    break;
                }

                // 존재하지 않는 참조는 기존 ValidateRounds에서 출력합니다.
                if (!data.TryGetData<RoundData>(currentId, out RoundData current))
                    break;

                currentId = current.NextRoundID;
            }

            checkedRounds.UnionWith(path);
        }
    }
    private static T Require<T>(DataManager data, int id, string source, List<string> errors) where T : class, ICSVData
    {
        if (data.TryGetData<T>(id, out T value))
            return value;

        errors.Add($"[{source}] 참조한 {typeof(T).Name} ID={id}가 없습니다.");
        return null;
    }
}