using UnityEngine;

enum ButtonType
{
    StartButton,
    ExplaneButton,
    OutButton,
    ExitButton
}

public class ButtonManager : MonoBehaviour
{
    public static ButtonManager Instance { get; private set; }
    [SerializeField] private StartMenuView menuView;
    [SerializeField]
    private string gameSceneName = "InGame";
    private MenuModel menuModel;
    private MenuController menuController;


    private void Awake()
    {
        // 이미 다른 인스턴스가 존재하는 경우, 현재 인스턴스를 파괴
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
        // inspector 연결 여부를 확인
        if (menuView == null || !menuView.IsConfigured)
        {
            Debug.LogWarning("[ButtonManager] StartMenuView가 할당되지 않았거나, 구성되지 않았습니다.");
            return;
        }
        // 기존 패널의 활성상태와 동일하게 model을 만든다.
        menuModel = new MenuModel(menuView.IsExplanationPanelOpen);
        //위에서 만든 모델을 컨트롤러에 전달
        menuController = new MenuController(menuModel, new MenuNavigation(gameSceneName));
        // 모델 상태가 바뀌면 하면 표시 함수를 실행
        menuModel.ExplanationChanged += OnExplanationChanged;
        // 최초 화면 상태도 모델과 맟춰서 표시
        OnExplanationChanged(menuModel.IsExplanationPanelOpen);
    }

    private void OnExplanationChanged(bool isOpen)
    {
        if (menuView == null)
            return;

        menuView.RenderExplanation(isOpen);
    }

    private void OnDestroy()
    {
        if (menuModel != null)
        {
            menuModel.ExplanationChanged -= OnExplanationChanged;
        }

        if (Instance == this)
        {
            Instance = null;
        }
    }

    public void OpenPanel(GameObject panel)
    {
        if (panel == null)
            return;

        if (menuView != null && menuView.IsExplanationPanel(panel))
        {
            ShowExplanation();
            return;
        }
        panel.SetActive(true);
    }

    public void ClosePanel(GameObject panel)
    {
        if (panel == null)
            return;
        if (menuView != null && menuView.IsExplanationPanel(panel))
        {
            OutPannel();
            return;
        }
        panel.SetActive(false);
    }

    public void OnClickButton(string buttonName)
    {
        if (System.Enum.TryParse(buttonName, out ButtonType buttonType))
        {
            switch (buttonType)
            {
                case ButtonType.StartButton:
                    StartGame();
                    break;
                case ButtonType.ExplaneButton:
                    ShowExplanation();
                    break;
                case ButtonType.ExitButton:
                    ExitGame();
                    break;
                case ButtonType.OutButton:
                    OutPannel();
                    break;
                default:
                    Debug.LogWarning($"[ButtonManager] 알 수 없는 버튼 타입: {buttonName}");
                    break;
            }
        }
        else
        {
            Debug.LogWarning($"[ButtonManager] 버튼 이름을 ButtonType으로 변환할 수 없습니다: {buttonName}");
        }
    }

    private void ShowExplanation()
    {
        if (menuController == null)
        {
            Debug.LogWarning("[ButtonManager] MenuController가 초기화되지 않았습니다.");
            return;
        }
        menuController.OpenExplanation();
    }

    private void StartGame()
    {
        // 게임 시작 로직 구현
        Debug.Log("게임 시작");
        if (menuController == null)
        {
            Debug.LogWarning("[ButtonManager] MenuController가 초기화되지 않았습니다.");
            return;
        }
        menuController.StartGame();
    }
    private void OutPannel()
    {
        if (menuController == null)
        {
            Debug.LogWarning("[ButtonManager] 설명 패널이 할당되지 않았습니다.");
            return;
        }
        menuController.CloseExplanation();
    }
    private void ExitGame()
    {
        if (menuController == null)
        {
            Debug.LogWarning("[ButtonManager] MenuController가 초기화되지 않았습니다.");
            return;
        }
        menuController.ExitGame();
    }
}
