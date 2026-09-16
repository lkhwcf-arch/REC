using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.EventSystems;
using System;

[RequireComponent(typeof(CharacterController))]
public class PlayerController : MonoBehaviour
{
    [Header("플레이어 데이터")]
    [SerializeField] private Player player = new Player();
    public Player Data => player;

    [Header("참조")]
    [SerializeField] private CameraManager cameraManager;

    // 실제 렌더링하는 Main Camera
    [SerializeField] private Camera playerCamera;

    // Player 아래 눈높이 기준점
    [SerializeField] private Transform cameraTarget;

    [Header("이동")]
    [SerializeField] private float moveSpeed = 3f;
    [SerializeField] private float gravity = -20f;

    [Header("시점")]
    [SerializeField] private float mouseSensitivity = 0.12f;
    [SerializeField] private float minPitch = -80f;
    [SerializeField] private float maxPitch = 80f;

    [Header("상호작용")]
    [SerializeField] private float interactionDistance = 3f;
    [SerializeField] private float holdDuration = 1f;

    // 벽과 바닥은 포함하고, Player 레이어는 제외
    [SerializeField] private LayerMask interactionRayMask = ~0;

    private CharacterController characterController;

    private InputActionMap inputMap;
    private InputAction moveAction;
    private InputAction lookAction;
    private InputAction clickAction;
    private InputAction returnToCCTVAction;

    private float verticalSpeed;
    private float pitch;

    private bool controlsActive;
    private bool applicationFocused = true;
    private int controlStartFrame = -1;

    private Interact pressedTarget;
    private float pressedTime;
    private bool holdTriggered;
    //private InputAction pointerAction;
    // 조준 UI나 유지 진행 표시를 만들 때 사용 가능
    
    private Interact currentTarget;
    public event Action<Interact> TargetChanged;

    public Interact CurrentTarget
    {
        get => currentTarget;

        private set
        {
            if(ReferenceEquals(currentTarget, value))
                return;

            currentTarget = value;

            TargetChanged?.Invoke(currentTarget);
        }
    }
    // 입력 판정과 UI에서 같은 유지 시간을 사용
    public float HoldDuration => Mathf.Max(0.01f, holdDuration);
    public float HoldProgress { get; private set; }

    private bool CanControl =>
        applicationFocused &&
        cameraManager != null &&
        cameraManager.IsPlayerView &&
        player != null &&
        player.CanAct &&
        Time.timeScale > 0f;

    private void Awake()
    {
        // 저장된 Inspector 데이터가 null이어도 초기화
        if (player == null)
        {
            player = new Player();
        }
        characterController = GetComponent<CharacterController>();

        inputMap = new InputActionMap("PlayerControls");

        moveAction = inputMap.AddAction("Move", InputActionType.Value);

        moveAction.AddCompositeBinding("2DVector")
            .With("Up", "<Keyboard>/w")
            .With("Down", "<Keyboard>/s")
            .With("Left", "<Keyboard>/a")
            .With("Right", "<Keyboard>/d");

        lookAction = inputMap.AddAction("Look", InputActionType.Value, "<Mouse>/delta");
        //pointerAction = inputMap.AddAction("Pointer", InputActionType.Value, "<Mouse>/position");

        clickAction = inputMap.AddAction("Interact", InputActionType.Button, "<Mouse>/leftButton");

        returnToCCTVAction = inputMap.AddAction("ReturnToCCTV", InputActionType.Button, "<Keyboard>/tab");

        if (cameraTarget != null)
        {
            pitch = Mathf.DeltaAngle(
                0f,
                cameraTarget.localEulerAngles.x);
        }
    }

    private void OnEnable()
    {
        inputMap.Enable();
    }

    private void Start()
    {
        if (cameraManager == null ||
            playerCamera == null ||
            cameraTarget == null)
        {
            Debug.LogError("[PlayerController] CameraManager, Main Camera, CameraTarget을 연결하세요.", this);

            enabled = false;
        }
    }

    private void OnDisable()
    {
        inputMap.Disable();
        controlsActive = false;
        CurrentTarget = null;

        CancelInteraction();
        UnlockCursor();
    }

    private void OnDestroy()
    {
        inputMap.Dispose();
    }

    private void Update()
    {
        bool canControl = CanControl;

        // 플레이어 모드에서 Tab을 누르면 CCTV로 복귀
        if (canControl &&
            returnToCCTVAction.WasPressedThisFrame())
        {
            cameraManager.ShowCCTV();
            canControl = CanControl;
        }

        SynchronizeControlState(canControl);

        // 모드 전환 프레임의 마우스 입력은 무시
        if (canControl && Time.frameCount != controlStartFrame)
            UpdateLook();

        // CCTV 중에도 중력과 바닥 접촉 처리는 유지
        UpdateMovement(canControl);
    }

    private void LateUpdate()
    {
        if (!CanControl || !controlsActive ||
        Time.frameCount == controlStartFrame ||
        IsPointerOverUI())
        {
            CurrentTarget = null;
            CancelInteraction();
            return;
        }

        UpdateInteraction();

         // 상호작용 실행으로 대상이 비활성화되거나
    // 해결 불가능 상태가 되면 안내도 숨깁니다.
        if(CurrentTarget == null || !CurrentTarget.CanInteract)
        {
            CurrentTarget = null;
            CancelInteraction();
        }
    }

    private void SynchronizeControlState(bool canControl)
    {
        if (controlsActive == canControl)
            return;

        controlsActive = canControl;

        CurrentTarget = null;
        CancelInteraction();

        if (canControl)
        {
            controlStartFrame = Time.frameCount;

            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
        else
        {
            UnlockCursor();
        }
    }

    private void UpdateMovement(bool canControl)
    {
        Vector2 input = canControl ? moveAction.ReadValue<Vector2>() : Vector2.zero;

        input = Vector2.ClampMagnitude(input, 1f);

        Vector3 direction = transform.right * input.x + transform.forward * input.y;

        if (characterController.isGrounded && verticalSpeed < 0f)
            verticalSpeed = -2f;

        verticalSpeed += gravity * Time.deltaTime;

        Vector3 velocity = direction * moveSpeed;
        velocity.y = verticalSpeed;

        CollisionFlags flags = characterController.Move(velocity * Time.deltaTime);

        if ((flags & CollisionFlags.Above) != 0 &&
            verticalSpeed > 0f)
        {
            verticalSpeed = 0f;
        }
    }

    private void UpdateLook()
    {
        if (cameraTarget == null || IsPointerOverUI())
            return;

        Vector2 delta = lookAction.ReadValue<Vector2>();

        transform.Rotate(Vector3.up, delta.x * mouseSensitivity, Space.Self);

        pitch -= delta.y * mouseSensitivity;
        pitch = Mathf.Clamp(pitch, minPitch, maxPitch);

        cameraTarget.localRotation = Quaternion.Euler(pitch, 0f, 0f);
    }

    private void UpdateInteraction()
    {
        CurrentTarget = FindTarget();

        // 새로 누른 순간에만 상호작용을 시작합니다.
        if (clickAction.WasPressedThisFrame())
        {
            CancelInteraction();

            if (CurrentTarget == null)
                return;

            // Click 대상은 누르는 순간 한 번 실행합니다.
            if (CurrentTarget.InputType == InteractionInputType.Click)
            {
                CurrentTarget.Click();
                return;
            }

            // Hold 대상은 누르기 시작한 대상과 시간을 기억합니다.
            pressedTarget = CurrentTarget;
            pressedTime = Time.time;
        }

        if (pressedTarget == null)
        {
            CancelInteraction();
            return;
        }

        // 거리·시선 이탈, 대상 비활성화, 입력 방식 변경 시 취소합니다.
        if (CurrentTarget != pressedTarget || !pressedTarget.CanInteract
            || pressedTarget.InputType != InteractionInputType.Hold)
        {
            CancelInteraction();
            return;
        }

        // 버튼을 떼면 완료 판정보다 먼저 취소
        if (clickAction.WasReleasedThisFrame() || !clickAction.IsPressed())
        {
            CancelInteraction();
            return;
        }

        // 계속 누르고 있어도 이미 실행했다면 반복하지 않음
        if (holdTriggered)
            return;

        float elapsed = Time.time - pressedTime;
        float duration = HoldDuration;

        HoldProgress = Mathf.Clamp01(elapsed / duration);

        if (elapsed < duration)
            return;

        // 실행 중 물체가 비활성화될 수 있으므로 먼저 기록
        holdTriggered = true;

        Interact target = pressedTarget;
        target.Hold();
    }
    private Interact FindTarget()
    {
        if (playerCamera == null)
            return null;

        // 카메라 화면의 가로 50% 세로 50%에서 정면으로 발사
        Ray ray = playerCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));

        if (!Physics.Raycast(ray, out RaycastHit hit, interactionDistance, interactionRayMask, QueryTriggerInteraction.Ignore))
        {
            return null;
        }

        Interact tartget = hit.collider.GetComponentInParent<Interact>();
        return tartget != null && tartget.CanInteract ? tartget : null;
    }

    private void CancelInteraction()
    {
        pressedTarget = null;
        pressedTime = 0f;
        holdTriggered = false;
        HoldProgress = 0f;
    }

    private void UnlockCursor()
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    private void OnApplicationFocus(bool hasFocus)
    {
        applicationFocused = hasFocus;

        if (!hasFocus)
        {
            controlsActive = false;
            CurrentTarget = null;

            CancelInteraction();
            UnlockCursor();
        }
    }

    private bool IsPointerOverUI()
    {
        return EventSystem.current != null &&
               EventSystem.current.IsPointerOverGameObject();
    }
}