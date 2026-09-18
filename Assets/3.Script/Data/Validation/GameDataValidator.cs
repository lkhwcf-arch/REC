using System;
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

    private static T Require<T>(DataManager data, int id, string source, List<string> errors) where T : class, ICSVData
    {
        if (data.TryGetData<T>(id, out T value))
            return value;

        errors.Add($"[{source}] 참조한 {typeof(T).Name} ID={id}가 없습니다.");
        return null;
    }
}