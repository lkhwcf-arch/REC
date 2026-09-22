using System.Linq;
using REC.Core;
using UnityEngine;
using UnityEngine.UI;

// 기존 메뉴 패키지에 의존하지 않는 인게임 표시 어댑터입니다. 시계는 표시 분이 바뀔 때만 갱신합니다.
public class InGameHud : MonoBehaviour
{
    private GameSessionHost host;
    private InGameSessionController controller;
    private PlayerInteractor interactor;
    private Text clock, status, notice, prompt;
    private GameObject controls;
    private Button observe, patrol, next, skip, resume;
    private Font font;
    private int lastMinute = -1;
    private Interact lastTarget;
    public void Configure(GameSessionHost owner, InGameSessionController flow, PlayerInteractor input)
    {
        host = owner; controller = flow; interactor = input;
        font = Font.CreateDynamicFontFromOSFont(new[] { "Malgun Gothic", "Arial" }, 22);
        var root = new GameObject("InGameHUD", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        root.transform.SetParent(transform, false); var canvas = root.GetComponent<Canvas>(); canvas.renderMode = RenderMode.ScreenSpaceOverlay; canvas.sortingOrder = 100;
        var scaler = root.GetComponent<CanvasScaler>(); scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize; scaler.referenceResolution = new Vector2(1920, 1080);
        clock = Label(root.transform, "Clock", new Vector2(30, -25), new Vector2(600, 45), 30);
        status = Label(root.transform, "Status", new Vector2(30, -80), new Vector2(900, 40), 22);
        notice = Label(root.transform, "Notice", new Vector2(30, -125), new Vector2(1200, 70), 22);
        Label(root.transform, "Keys", new Vector2(30, -205), new Vector2(1100, 40), 19).text = "WASD 이동 · 마우스 시점 · Tab CCTV실 복귀 · N 다음 일정 · Esc 일시정지";
        prompt = Label(root.transform, "Prompt", Vector2.zero, new Vector2(800, 80), 24);
        prompt.rectTransform.anchorMin = prompt.rectTransform.anchorMax = new Vector2(0.5f, 0.28f); prompt.rectTransform.pivot = new Vector2(0.5f, 0.5f); prompt.alignment = TextAnchor.MiddleCenter;
        var dot = Label(root.transform, "Crosshair", Vector2.zero, new Vector2(30, 30), 24); dot.text = "·";
        dot.rectTransform.anchorMin = dot.rectTransform.anchorMax = new Vector2(0.5f, 0.5f); dot.rectTransform.pivot = new Vector2(0.5f, 0.5f); dot.alignment = TextAnchor.MiddleCenter;
        controls = new GameObject("SessionControls", typeof(RectTransform)); controls.transform.SetParent(root.transform, false);
        var panel = (RectTransform)controls.transform; panel.anchorMin = panel.anchorMax = new Vector2(1, 1); panel.pivot = new Vector2(1, 1); panel.anchoredPosition = new Vector2(-30, -25); panel.sizeDelta = new Vector2(290, 330);
        skip = MakeButton("다음 일정으로", 0, controller.RequestSkip);
        observe = MakeButton("CCTV 확인", 60, controller.RequestObserve);
        patrol = MakeButton("현장 순찰 시작", 120, controller.RequestPatrol);
        next = MakeButton("다음 회차 시작", 180, controller.RequestNextRound);
        resume = MakeButton("계속하기", 240, () => controller.SetPaused(false));
        host.Changed += OnChanged; controller.ViewChanged += Refresh; interactor.TargetChanged += OnTarget;
        Refresh();
    }
    private Text Label(Transform parent, string name, Vector2 position, Vector2 size, int fontSize)
    {
        var node = new GameObject(name, typeof(RectTransform), typeof(Text), typeof(Outline)); node.transform.SetParent(parent, false);
        var text = node.GetComponent<Text>(); text.font = font; text.fontSize = fontSize; text.color = Color.white; text.raycastTarget = false;
        var rect = text.rectTransform; rect.anchorMin = rect.anchorMax = new Vector2(0, 1); rect.pivot = new Vector2(0, 1); rect.anchoredPosition = position; rect.sizeDelta = size;
        return text;
    }
    private Button MakeButton(string caption, float y, UnityEngine.Events.UnityAction action)
    {
        var node = new GameObject(caption, typeof(RectTransform), typeof(Image), typeof(Button)); node.transform.SetParent(controls.transform, false);
        var rect = (RectTransform)node.transform; rect.anchorMin = rect.anchorMax = new Vector2(0, 1); rect.pivot = new Vector2(0, 1); rect.anchoredPosition = new Vector2(0, -y); rect.sizeDelta = new Vector2(290, 50);
        node.GetComponent<Image>().color = new Color(0.08f, 0.09f, 0.12f, 0.95f);
        var text = Label(node.transform, "Text", Vector2.zero, rect.sizeDelta, 23); text.text = caption; text.alignment = TextAnchor.MiddleCenter;
        var button = node.GetComponent<Button>(); button.onClick.AddListener(action); return button;
    }
    private void OnChanged(SessionEvent _) => Refresh();
    private void Refresh()
    {
        if (host.Session == null) { status.text = "인게임 준비 중"; return; }
        var session = host.Session;
        status.text = $"{session.RoundId}회차 · 해결 {session.Missions.Count(m => m.Status == MissionStatus.Resolved)} / {session.Missions.Count}";
        notice.text = session.Phase == SessionPhase.ConfigurationError ? session.Error : controller.Notice;
        controls.SetActive(!controller.IsEnding && (controller.IsPaused || !session.CanPatrol));
        skip.gameObject.SetActive(session.Phase is SessionPhase.ControlRoom or SessionPhase.AnomalyPatrol);
        observe.gameObject.SetActive(session.Phase == SessionPhase.ControlRoom); observe.interactable = session.ElapsedMs >= session.Rules.FirstObservationMs && !controller.Observed;
        patrol.gameObject.SetActive(session.Phase == SessionPhase.ControlRoom); patrol.interactable = controller.Observed;
        next.gameObject.SetActive(session.Phase == SessionPhase.RoundComplete); resume.gameObject.SetActive(controller.IsPaused);
    }
    private void OnTarget(Interact target)
    {
        lastTarget = target;
        prompt.text = target == null ? "" : target.InputType == InteractionInputType.Click ? "문 열기 · 마우스 왼쪽 클릭" : "해결하기 · 마우스 왼쪽 버튼 1초 유지";
    }
    private void Update()
    {
        if (host?.Session == null) return;
        int minute = host.Session.DisplayMinute;
        if (lastMinute != minute) { lastMinute = minute; clock.text = $"{minute / 60:00}:{minute % 60:00}"; Refresh(); }
        if (lastTarget != null && !lastTarget.CanInteract) OnTarget(null);
    }
    private void OnDestroy()
    {
        if (host != null) host.Changed -= OnChanged;
        if (controller != null) controller.ViewChanged -= Refresh;
        if (interactor != null) interactor.TargetChanged -= OnTarget;
        if (font != null) { if (Application.isPlaying) Destroy(font); else DestroyImmediate(font); }
    }
}
