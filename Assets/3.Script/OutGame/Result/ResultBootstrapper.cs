using UnityEngine;
using UnityEngine.InputSystem;
public sealed class ResultBootstrapper : MonoBehaviour
{

    [Header("타이틀 복귀")]
    [SerializeField]
    private string titleSceneName = "Start";

    [Header("화면 연결")]
    [SerializeField] private ResultView resultView;

    [Header("화면 진입 후 시간")]
    [SerializeField, Min(0f)] private float resultDelay = 1f;

    [SerializeField, Min(0f)]
    private float inputDelay = 3f;
    private ResultModel resultModel;
    private ResultController resultController;

    private void Awake()
    {
        if (resultView == null || !resultView.IsConfigured)
        {
            Debug.LogError("[ResultBootStrapper] result view 와 두텍스트를 연결하세요", this);
            enabled = false;
            return;
        }

        resultView.HideAll();
        if (!AreTimingsValid())
        {
            Debug.LogError("[ResultBootStrapper] 시간 설정을 확인하세요 0 이상이며 input delay가 result delay 이상일때", this);
            enabled = false;
            return;
        }

        if (string.IsNullOrWhiteSpace(titleSceneName) || !Application.CanStreamedLevelBeLoaded(titleSceneName))
        {
            Debug.LogError("[ResultBootstrapper] Title Scene name과 빌드씬 목록을 확인", this);
            enabled = false;
            return;
        }
        resultModel = new ResultModel();

        IResultNavigation navigation =
            new ResultNavigation(titleSceneName);
        resultController = new ResultController(
            resultModel,
            resultDelay,
            inputDelay,
            navigation);
        resultModel.PhaseChanged += OnPhaseChanged;
        // 모델 생성만으로는 이벤트가 발생하지 않으므로 
        // 최초 상태를 직접 화면에 반영
        OnPhaseChanged(resultModel.CurrentPhase);
    }
    void Update()
    {
        if (resultController == null || resultModel == null)
            return;

        // 시간 진행 전부터 입력 가능한 상태였는지 기억
        bool couldAcceptInput = resultModel.CanAcceptInput;
        resultController.Tick(Time.unscaledDeltaTime);
        //이번 프레임에 처음 입력 가능 상태로 바뀌었다면 
        // 이 프레임의 키 입력은 사용하지 않는다.
        if (!couldAcceptInput || !resultModel.CanAcceptInput)
            return;
        if (!Application.isFocused)
            return;

        if (WasNewKeyboardPress())
        {
            resultController.RequestReturnToTitle();
        }
    }
    private void OnPhaseChanged(ResultPhase phase)
    {
        if (resultView == null)
            return;

        switch (phase)
        {
            case ResultPhase.Waiting:
                resultView.HideAll();
                break;

            case ResultPhase.ShowingResult:
                resultView.ShowResult();
                break;

            case ResultPhase.WaitingForInput:
                resultView.ShowContinuePrompt();
                break;

            case ResultPhase.Transitioning:
                // 씬 이동 처리는 다음 단계에서 연결
                break;
        }
    }
    private void OnDestroy()
    {
        if (resultModel != null)
        {
            resultModel.PhaseChanged -= OnPhaseChanged;

        }
    }
    private bool AreTimingsValid()
    {
        return !float.IsNaN(resultDelay)
            && !float.IsInfinity(resultDelay)
            && !float.IsNaN(inputDelay)
            && !float.IsInfinity(inputDelay)
            && resultDelay >= 0f
            && inputDelay >= resultDelay;
    }


    private bool WasNewKeyboardPress()
    {
        Keyboard keyboard = Keyboard.current;

        if (keyboard == null)
            return false;

        foreach (var key in keyboard.allKeys)
        {
            if (key == null)
                continue;

            if (key.wasPressedThisFrame)
                return true;
        }
        return false;
    }
}
