using System;
public enum ResultPhase
{
    Waiting,
    ShowingResult,
    WaitingForInput,
    Transitioning
}
public class ResultModel
{
    public ResultPhase CurrentPhase { get; private set; } = ResultPhase.Waiting;
    public bool CanAcceptInput => CurrentPhase == ResultPhase.WaitingForInput;
    public event Action<ResultPhase> PhaseChanged;

    public bool TryShowResult()
    {
        if (CurrentPhase != ResultPhase.Waiting)
            return false;

        ChangePhase(ResultPhase.ShowingResult);
        return true;
    }
    public bool TryEnableInput()
    {
        if (CurrentPhase != ResultPhase.ShowingResult)
            return false;

        ChangePhase(ResultPhase.WaitingForInput);
        return true;
    }
    public bool TryBeginTransition()
    {
        if (!CanAcceptInput)
            return false;

        ChangePhase(ResultPhase.Transitioning);
        return true;
    }

    private void ChangePhase(ResultPhase nextPhase)
    {
        CurrentPhase = nextPhase;
        PhaseChanged?.Invoke(CurrentPhase);
    }
}
