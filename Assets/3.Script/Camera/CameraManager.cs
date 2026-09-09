using System;
using System.Collections.Generic;
using UnityEngine;
using Unity.Cinemachine;

public enum CameraMode
{
    Title,
    Player,
    CCTV,
    GameOver,
    GameClear
}

public class CameraManager : MonoBehaviour
{
    [Header("씬 시작시 사용 할 모드")]
    [SerializeField] private CameraMode initialMode = CameraMode.Title;

    [Header("사작 화면")]
    [SerializeField] private Camera titleCamera;

    [Header("플레이어 모드")]
    [Tooltip("플레이어 모드에서 실제 화면")]
    [SerializeField] private Camera playerOutputCamera;
    [Tooltip("Player Output Camera에 붙어 있는 Cinemachine Brain")]
    [SerializeField] private CinemachineBrain playerBrain;

    [Tooltip("플레이어 시점을 제어하는 Cinemachine Camera")]
    [SerializeField] private CinemachineCamera playerCinemachineCamera;

    [Header("CCTV 모드")]
    [Tooltip("CCTV 모드에서 실제 화면")]
    [SerializeField] private Camera[] CCTVCameras = new Camera[5];

    [Tooltip("Screen Sapace Overlay 모드에서 CCTV 화면을 보여주기 위한 Camera")]
    [SerializeField] private GameObject CCTVScreenPannel;
    [Header("게임 종료 모드")]
    [SerializeField] private Camera gameoverCamera;
    [SerializeField] private Camera gameclearCamera;

    [SerializeField] private GameObject playerUI;

    // 플레이어 시점으로 복귀할 때 복원할 설정
    private int originalCullingMask;
    private CameraClearFlags originalClearFlags;
    private Color originalBackgroundColor;


    public CameraMode CurrentMode { get; private set; }
    private bool initialized;

    // 플레이어 컨트롤러 등에서 현재 상태를 확인 할 때 사용
    public bool IsPlayerView => initialized && CurrentMode == CameraMode.Player;
    // 입력 제어 등 다른 시스템에 카메라 상태 변경을 알림
    public event Action<CameraMode> ModeChanged;
    private void Start()
    {
        // 다른 시스템이 먼저 모드를 설정했다면 덮어쓰지 않음
        if (!initialized)
            SetMode(initialMode);
    }

    private void Awake()
    {
        if (playerOutputCamera == null)
            return;

        originalCullingMask = playerOutputCamera.cullingMask;
        originalClearFlags = playerOutputCamera.clearFlags;
        originalBackgroundColor = playerOutputCamera.backgroundColor;
    }

    private void SetMode(CameraMode nextMode)
    {
        if (initialized && CurrentMode == nextMode) return;

        // 잘못된 설정으로 현재 화면까지 꺼지는 것을 방지
        if (!CanUseMode(nextMode))
        {
            Debug.LogError($"[CameraManager] {nextMode} 모드의 참조를 확인하세요.", this);
            return;
        }

        DisableAllViews();

        switch (nextMode)
        {
            case CameraMode.Title:
                titleCamera.enabled = true;
                break;
            case CameraMode.Player:
                // 원래 플레이어 카메라 설정 복원
                playerOutputCamera.cullingMask = originalCullingMask;
                playerOutputCamera.clearFlags = originalClearFlags;
                playerOutputCamera.backgroundColor = originalBackgroundColor;

                playerCinemachineCamera.enabled = true;
                playerBrain.enabled = true;
                playerOutputCamera.enabled = true;

                // playerUI 필드를 추가했다면 유지
                if (playerUI != null)
                    playerUI.SetActive(true);
                
                foreach (var cam in CCTVCameras)
                    cam.enabled = false;
                break;
            case CameraMode.CCTV:
                // Main Camera는 장면 오브젝트를 그리지 않고 배경만 출력
                if(playerOutputCamera != null)
                {
                    playerOutputCamera.targetTexture = null;
                    playerOutputCamera.rect = new Rect(0f, 0f, 1f, 1f);
                    playerOutputCamera.cullingMask = 0; // Nothing
                    playerOutputCamera.clearFlags = CameraClearFlags.SolidColor;
                    playerOutputCamera.backgroundColor = Color.black;
                    playerOutputCamera.enabled = true;
                }

                foreach (var cam in CCTVCameras)
                    cam.enabled = true;

                CCTVScreenPannel.SetActive(true);
                break;
            case CameraMode.GameOver:
                gameoverCamera.enabled = true;
                break;
            case CameraMode.GameClear:
                gameclearCamera.enabled = true;
                break;
        }
        CurrentMode = nextMode;
        initialized = true;

        ModeChanged?.Invoke(CurrentMode);
    }

    private void DisableAllViews()
    {
        SetCameraEnabled(titleCamera, false);
        SetCameraEnabled(playerOutputCamera, false);
        SetCameraEnabled(gameoverCamera, false);
        SetCameraEnabled(gameclearCamera, false);

        if (playerBrain != null)
            playerBrain.enabled = false;

        if (playerCinemachineCamera != null)
            playerCinemachineCamera.enabled = false;

        if (CCTVCameras != null)
        {
            foreach (Camera camera in CCTVCameras)
                SetCameraEnabled(camera, false);
        }

        if (CCTVScreenPannel != null)
            CCTVScreenPannel.SetActive(false);

        if (playerUI != null)
            playerUI.SetActive(false);
    }

    private bool CanUseMode(CameraMode mode)
    {
        switch (mode)
        {
            case CameraMode.Title:
                return IsCameraReady(titleCamera);
            case CameraMode.Player:
                return IsCameraReady(playerOutputCamera)
                    && playerBrain != null
                    && playerBrain.gameObject
                        == playerOutputCamera.gameObject
                    && playerCinemachineCamera != null
                    && playerCinemachineCamera.gameObject.activeInHierarchy;
            case CameraMode.CCTV:
                if (CCTVScreenPannel == null || CCTVCameras == null || CCTVCameras.Length == 0)
                    return false;

                foreach (Camera camera in CCTVCameras)
                {
                    if (!IsCameraReady(camera)
                        || camera.targetTexture == null)
                    {
                        return false;
                    }
                }

                return true;
            case CameraMode.GameOver:
                return IsCameraReady(gameoverCamera);
            case CameraMode.GameClear:
                return IsCameraReady(gameclearCamera);
            default:
                return false;
        }
    }
    private static bool IsCameraReady(Camera camera)
    {
        return camera != null && camera.gameObject.activeInHierarchy;
    }

    private static void SetCameraEnabled(Camera camera, bool enabled)
    {
        if (camera != null)
            camera.enabled = enabled;
    }

    // UI Button의 OnClick에서도 연결할 수 있는 함수
    public void ShowTitle() => SetMode(CameraMode.Title);
    public void ShowPlayer() => SetMode(CameraMode.Player);
    public void ShowCCTV() => SetMode(CameraMode.CCTV);
    public void ShowGameOver() => SetMode(CameraMode.GameOver);
    public void ShowGameClear() => SetMode(CameraMode.GameClear);

}