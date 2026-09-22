using System;
using REC.Core;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

// 플레이어 입력/문 통과/UI 요청을 도메인 명령으로 바꾸며 완료 이벤트로 시점을 갱신합니다.
[DefaultExecutionOrder(100)]
public class InGameSessionController : MonoBehaviour
{
    private GameSessionHost host;
    private PlayerController player;
    private CameraManager cameras;
    private Transform roomDoor;
    private Bounds room;
    private Vector3 entrance, spawn, roomPosition, lastOutside;
    private Quaternion spawnRotation;
    private bool paused, observed, changingScene;
    private float previousTimeScale = 1;
    public bool IsPaused => paused;
    public bool IsEnding => host?.Session?.Phase == SessionPhase.Ending || endingPreview;
    private bool endingPreview;
    public void SetEndingPreview(bool value) { endingPreview = value; ViewChanged?.Invoke(); }
    public string Notice { get; private set; }
    public event Action ViewChanged;
    public event Action<string> ResultRequested;
    public bool NearEntrance => player != null && Vector3.Distance(player.transform.position, entrance) < 2.8f;

    private PlayerInteractor interactor;
    private PresentationDirector presentation;
    public bool Observed => observed;
    public void Configure(GameSessionHost owner, PlayerController controller, CameraManager manager, Bounds roomBounds, Bounds doorBounds, Transform door, Transform map)
    {
        host = owner; player = controller; cameras = manager; room = roomBounds; roomDoor = door;
        Vector3 outward = doorBounds.center - room.center; outward.y = 0; outward.Normalize();
        entrance = new Vector3(doorBounds.center.x, room.max.y + 0.06f, doorBounds.center.z);
        spawn = entrance + outward * 1.5f; roomPosition = entrance - outward * 1.5f;
        spawnRotation = Quaternion.LookRotation(outward, Vector3.up); lastOutside = spawn;
        host.Changed += OnChanged; player.ReturnRequested += RequestReturn;
        cameras.ModeAllowed = mode => host.Session != null && (mode == CameraMode.Player ? host.Session.CanPatrol : mode == CameraMode.CCTV && host.Session.Phase is SessionPhase.ControlRoom or SessionPhase.RoundComplete);
        interactor = player.GetComponent<PlayerInteractor>();
        presentation = GetComponent<PresentationDirector>();
    }
    private void Update()
    {
        if (host?.Session == null || changingScene || IsEnding) return;
        var keyboard = Keyboard.current;
        if (keyboard != null && keyboard.escapeKey.wasPressedThisFrame && !host.Session.IsTerminal) SetPaused(!paused);
        if (!paused && keyboard != null && keyboard.nKey.wasPressedThisFrame) RequestSkip();
        var session = host.Session;
        player.SetSessionControl(!paused && session.CanPatrol && !session.IsTerminal);
        if (!session.CanPatrol || paused)
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
        // UI뿐 아니라 실제 CCTV실 진입도 제한합니다. 별도 사물 상호작용은 추가하지 않습니다.
        Vector3 position = player.transform.position;
        bool inside = position.x > room.min.x && position.x < room.max.x && position.z > room.min.z && position.z < room.max.z && Mathf.Abs(position.y - room.max.y) < 2;
        if (session.Phase == SessionPhase.AnomalyPatrol && inside)
        {
            player.Teleport(lastOutside, player.transform.rotation); SetNotice("남은 미션을 모두 해결한 뒤 복귀할 수 있습니다.");
        }
        else if (!inside && session.CanPatrol) lastOutside = position;
    }
    public void RequestReturn()
    {
        if (IsEnding) return;
        if (paused) return;
        if (!NearEntrance) { SetNotice("CCTV실 문 앞으로 이동한 뒤 Tab을 누르세요."); return; }
        if (host.Session?.RequestEnterRoom() != true) SetNotice("남은 미션을 모두 해결한 뒤 복귀할 수 있습니다.");
    }
    public void RequestObserve()
    {
        if (IsEnding) return;
        if (host.Session?.RequestObserveCctv() != true) SetNotice("23:30부터 CCTV를 확인할 수 있습니다.");
    }
    public void RequestPatrol()
    {
        if (IsEnding) return;
        if (host.Session?.RequestLeaveRoom() != true) SetNotice("CCTV를 먼저 확인하세요.");
    }
    public void RequestSkip()
    {
        if (IsEnding) return;
        if (host.Session?.RequestSkip() != true) SetNotice("지금은 다음 일정으로 건너뛸 수 없습니다.");
    }
    public void RequestNextRound() => host.Session?.RequestNextRound();
    public void SetPaused(bool value)
    {
        if (IsEnding && value) return;
        if (paused == value)
            return;

        paused = value;

        if (paused)
        {
            interactor?.CancelCurrentInteraction();
            previousTimeScale = Time.timeScale;
            Time.timeScale = 0;
        }
        else
        {
            Time.timeScale = previousTimeScale;
        }

        presentation?.SetSuspended(Time.timeScale <= 0f || !Application.isFocused);
        ViewChanged?.Invoke();
    }
    private void OnChanged(SessionEvent message)
    {
        var session = host.Session;
        if (message.Kind == "RoundStarted")
        {
            interactor?.CancelCurrentInteraction();

            if (paused)
                SetPaused(false);

            changingScene = false;
            observed = false;

            player.Teleport(spawn, spawnRotation);
            lastOutside = spawn;
            cameras.ShowPlayer();

            SetNotice("초기 순찰을 진행하세요. 23:00에는 CCTV실로 자동 복귀합니다.");
        }
        if (message.Kind is "IntermediateReturned" or "ForcedReturned")
        {
            interactor?.CancelCurrentInteraction();
            player.SetSessionControl(false);
            player.Teleport(roomPosition, spawnRotation);
            cameras.ShowCCTV();

            SetNotice(message.Kind == "ForcedReturned"
                ? "23:00이 되어 CCTV실로 복귀했습니다. 23:30에 CCTV를 확인하세요."
                : "23:30에 CCTV를 확인하고 순찰을 시작하세요.");
        }
        if (message.Kind == "CctvObserved")
        {
            observed = true; SetNotice("CCTV 확인 완료. 현장 순찰을 시작할 수 있습니다.");
        }
        if (message.Kind == "PatrolStarted")
        {
            player.Teleport(spawn, spawnRotation);
            cameras.ShowPlayer(); SetNotice("이상현상을 찾아 마우스 왼쪽 버튼을 길게 눌러 해결하세요.");
        }
        if (message.Kind == "AllMissionsResolved")
            SetNotice("미션을 모두 해결했습니다. CCTV실로 최종 복귀하세요.");
        if (message.Kind == "EndingStarted")
        {
            interactor?.CancelCurrentInteraction();
            SetNotice("엔딩 재생 중입니다.");
        }
        if (message.Kind == "RoundCompleted")
        {
            cameras.ShowCCTV();
            SetNotice("회차 완료. 다음 회차를 시작하면 맵이 초기화됩니다.");
        }
        if (roomDoor != null)
            roomDoor.gameObject.SetActive(session.Phase != SessionPhase.ControlRoom);
        player.SetSessionControl(session.CanPatrol && !paused);

        if (message.Kind is "RoundStarted" or "GameOver" or "GameClear" or "ConfigurationError")
            interactor?.CancelCurrentInteraction();

        presentation?.Handle(message);

        if (message.Kind is "GameOver" or "GameClear")
        {
            changingScene = true; if (paused) SetPaused(false);
            Cursor.lockState = CursorLockMode.None; Cursor.visible = true;
            ResultRequested?.Invoke(message.Kind == "GameClear" ? "GameClear" : "GameOver");
        }
        ViewChanged?.Invoke();
    }
    private void SetNotice(string text) { Notice = text; ViewChanged?.Invoke(); }
    private void OnDestroy()
    {
        if (host != null) host.Changed -= OnChanged;
        if (player != null) player.ReturnRequested -= RequestReturn;
        if (cameras != null) cameras.ModeAllowed = null;
        if (paused) Time.timeScale = previousTimeScale;
    }
}
