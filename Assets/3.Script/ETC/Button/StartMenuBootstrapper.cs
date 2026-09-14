using UnityEngine;

[RequireComponent(typeof(ButtonManager))]
public class StartMenuBootstrapper : MonoBehaviour
{
    [SerializeField] private StartMenuView menuView;
    [SerializeField] private string gameSceneName = "InGame";
    private MenuModel menuModel;

    private void Start()
    {
        ButtonManager buttonManager = GetComponent<ButtonManager>();
        if (menuView == null || !menuView.IsConfigured)
        {
            Debug.LogError("[StartMenuBootstrapper] Menu View와 " + "Explanation Panel을 연결하세요.", this);
            return;
        }
        if (string.IsNullOrWhiteSpace(gameSceneName))
        {
            Debug.LogError("[StartMenuBootstrapper] 게임 씬 이름이 유효하지 않습니다.", this);
            return;
        }
        // 1. 현재 화면 상태로 model생성
        menuModel = new MenuModel(menuView.IsExplanationPanelOpen);
        //2. unity 기능을 실행할 서비스 생성
        IMenuNavigation navigation = new MenuNavigation(gameSceneName);
        //3. Model과 서비스를 controller에 연결
        MenuController menuController = new MenuController(menuModel, navigation);
        //4. 기존 버튼 창구에 constroller와 view를 연결
        buttonManager.Initialize(menuView, menuController);
        //5. 상태 변경 알림과 화면 연결
        menuModel.ExplanationChanged += OnenExplanationChanged;
        OnenExplanationChanged(menuModel.IsExplanationPanelOpen);
    }
    private void OnenExplanationChanged(bool isOpen)
    {
        if (menuView == null)
        {
            Debug.LogError("[StartMenuBootstrapper] Menu View가 null입니다.", this);
            return;
        }
        menuView.RenderExplanation(isOpen);
    }
    private void OnDestroy()
    {
        if (menuModel != null)
        {
            menuModel.ExplanationChanged -= OnenExplanationChanged;
        }
    }
}