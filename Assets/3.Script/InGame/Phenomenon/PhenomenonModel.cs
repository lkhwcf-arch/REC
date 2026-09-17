using System;

public enum PhenomenonState
{
    Normal,
    Active,
    Resolved
}

public sealed class PhenomenonModel
{
    public string OccurrenceId { get; }

    public PhenomenonState State { get; private set; }
        = PhenomenonState.Normal;

    public PhenomenonModel(string occurrenceId)
    {
        if (string.IsNullOrWhiteSpace(occurrenceId))
        {
            throw new ArgumentException(
                "이상현상 발생 건 ID가 필요합니다.",
                nameof(occurrenceId));
        }

        OccurrenceId = occurrenceId;
    }

    public bool TryActivate()
    {
        if (State != PhenomenonState.Normal)
            return false;

        State = PhenomenonState.Active;
        return true;
    }

    public bool TryMarkResolved()
    {
        if (State != PhenomenonState.Active)
            return false;

        State = PhenomenonState.Resolved;
        return true;
    }
}