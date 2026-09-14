using System;

public sealed class ResultController
{
    private readonly ResultModel model;

    private readonly float resultDelay;
    private readonly float inputDelay;

    private float elapsedTime;

    public ResultController(
        ResultModel model,
        float resultDelay,
        float inputDelay)
    {
        if (model == null)
        {
            throw new ArgumentNullException(nameof(model));
        }

        if (float.IsNaN(resultDelay) || float.IsInfinity(resultDelay) || resultDelay < 0f)
        {
            throw new ArgumentOutOfRangeException(nameof(resultDelay), "결과 표시 시간은 0 이상의 유한한 값이어야 합니다.");
        }

        if (float.IsNaN(inputDelay) || float.IsInfinity(inputDelay) || inputDelay < resultDelay)
        {
            throw new ArgumentOutOfRangeException(nameof(inputDelay), "입력 허용 시간은 결과 표시 시간 이상이어야 합니다.");
        }

        this.model = model;
        this.resultDelay = resultDelay;
        this.inputDelay = inputDelay;

        elapsedTime = 0f;
    }

    public void Tick(float deltaTime)
    {
        if (float.IsNaN(deltaTime) || float.IsInfinity(deltaTime) || deltaTime < 0f)
        {
            throw new ArgumentOutOfRangeException(nameof(deltaTime), "경과 시간은 0 이상의 유한한 값이어야 합니다.");
        }

        if (model.CurrentPhase == ResultPhase.WaitingForInput || model.CurrentPhase == ResultPhase.Transitioning)
        {
            return;
        }

        elapsedTime += deltaTime;

        if (model.CurrentPhase == ResultPhase.Waiting && elapsedTime >= resultDelay)
        {
            model.TryShowResult();
        }

        if (model.CurrentPhase == ResultPhase.ShowingResult && elapsedTime >= inputDelay)
        {
            model.TryEnableInput();
        }
    }
}