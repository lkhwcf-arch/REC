using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.EventSystems;

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
private InputAction pointerAction;
    // 조준 UI나 유지 진행 표시를 만들 때 사용 가능
    public Interact CurrentTarget { get; private set; }
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

        moveAction = inputMap.AddAction(
            "Move",
            InputActionType.Value);

        moveAction.AddCompositeBinding("2DVector")
            .With("Up", "<Keyboard>/w")
            .With("Down", "<Keyboard>/s")
            .With("Left", "<Keyboard>/a")
            .With("Right", "<Keyboard>/d");

        lookAction = inputMap.AddAction(
            "Look",
            InputActionType.Value,
            "<Mouse>/delta");
pointerAction = inputMap.AddAction(
    "Pointer",
    InputActionType.Value,
    "<Mouse>/position");
        clickAction = inputMap.AddAction(
            "Interact",
            InputActionType.Button,
            "<Mouse>/leftButton");

        returnToCCTVAction = inputMap.AddAction(
            "ReturnToCCTV",
            InputActionType.Button,
            "<Keyboard>/tab");

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
            Debug.LogError(
                "[PlayerController] CameraManager, Main Camera, " +
                "CameraTarget을 연결하세요.",
                this);

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
        if (!CanControl ||
        !controlsActive ||
        Time.frameCount == controlStartFrame ||
        IsPointerOverUI())
    {
        CurrentTarget = null;
        CancelInteraction();
        return;
    }

    UpdateInteraction();
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

            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
        else
        {
            UnlockCursor();
        }
    }

    private void UpdateMovement(bool canControl)
    {
        Vector2 input = canControl
        ? moveAction.ReadValue<Vector2>()
        : Vector2.zero;

        input = Vector2.ClampMagnitude(input, 1f);

        Vector3 direction =
            transform.right * input.x +
            transform.forward * input.y;

        if (characterController.isGrounded && verticalSpeed < 0f)
            verticalSpeed = -2f;

        verticalSpeed += gravity * Time.deltaTime;

        Vector3 velocity = direction * moveSpeed;
        velocity.y = verticalSpeed;

        CollisionFlags flags = characterController.Move(
            velocity * Time.deltaTime);

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

    transform.Rotate(
        Vector3.up,
        delta.x * mouseSensitivity,
        Space.Self);

    pitch -= delta.y * mouseSensitivity;
    pitch = Mathf.Clamp(pitch, minPitch, maxPitch);

    cameraTarget.localRotation =
        Quaternion.Euler(pitch, 0f, 0f);
    }

    private void UpdateInteraction()
    {
        CurrentTarget = FindTarget();

        if (clickAction.WasPressedThisFrame())
        {
            pressedTarget = CurrentTarget;
            pressedTime = Time.time;
            holdTriggered = false;
            HoldProgress = 0f;
        }

        if (pressedTarget == null)
        {
            CancelInteraction();
            return;
        }

        // 시선이 벗어나거나, 거리가 멀어지거나,
        // 대상이 비활성화되면 취소
        if (CurrentTarget != pressedTarget ||
            !pressedTarget.CanInteract)
        {
            CancelInteraction();
            return;
        }

        bool released = clickAction.WasReleasedThisFrame();
        bool held = clickAction.IsPressed();

        if (!held && !released)
        {
            CancelInteraction();
            return;
        }

        float elapsed = Time.time - pressedTime;
        float duration = Mathf.Max(0.01f, holdDuration);

        HoldProgress = Mathf.Clamp01(elapsed / duration);

        if (!holdTriggered && elapsed >= duration)
        {
            // 이벤트에서 대상이 파괴될 수 있으므로 먼저 기록
            holdTriggered = true;
            pressedTarget.Hold();
        }
        else if (released && !holdTriggered)
        {
            Interact target = pressedTarget;

            CancelInteraction();
            target.Click();
            return;
        }

        if (released)
            CancelInteraction();
    }

    private Interact FindTarget()
    {
        if (playerCamera == null)
        return null;

    Vector2 pointerPosition =
        pointerAction.ReadValue<Vector2>();

    Ray ray = playerCamera.ScreenPointToRay(pointerPosition);

    if (!Physics.Raycast(
            ray,
            out RaycastHit hit,
            interactionDistance,
            interactionRayMask,
            QueryTriggerInteraction.Ignore))
    {
        return null;
    }

    Interact target =
        hit.collider.GetComponentInParent<Interact>();

    return target != null && target.CanInteract
        ? target
        : null;
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