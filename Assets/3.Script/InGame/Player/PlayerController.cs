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
    [SerializeField] private Transform cameraTarget;

    [Header("이동")]
    [SerializeField] private float moveSpeed = 3f;
    [SerializeField] private float gravity = -20f;

    [Header("시점")]
    [SerializeField] private float mouseSensitivity = 0.12f;
    [SerializeField] private float minPitch = -80f;
    [SerializeField] private float maxPitch = 80f;

    private CharacterController characterController;

    private InputActionMap inputMap;
    private InputAction moveAction;
    private InputAction lookAction;
    private InputAction returnToCCTVAction;

    private float verticalSpeed;
    private float pitch;

    // 이번 프레임에 플레이어 조작으로 실제 수평 이동이 발생했는지 확인.
    private bool movedThisFrame;
    private int movementSampleFrame = -1;
    public bool IsMoving => isActiveAndEnabled && CanControl && movementSampleFrame == Time.frameCount && movedThisFrame;
    private bool controlsActive;
    private bool applicationFocused = true;
    private int controlStartFrame = -1;
    private bool sessionControlAllowed = true;
    private readonly System.Collections.Generic.HashSet<object> presentationLocks = new();
    public void SetPresentationLock(object owner, bool locked)
    {
        if (owner == null) return;
        if (locked) presentationLocks.Add(owner);
        else presentationLocks.Remove(owner);
    }
    public event System.Action ReturnRequested;
    public void SetSessionControl(bool allowed) => sessionControlAllowed = allowed;
    public void Teleport(Vector3 position, Quaternion rotation)
    {
         movedThisFrame = false;
        movementSampleFrame = -1;
        characterController ??= GetComponent<CharacterController>();
        characterController.enabled = false; 
        transform.SetPositionAndRotation(position, rotation); 
        characterController.enabled = true;
        verticalSpeed = 0; pitch = 0;
        
        if (cameraTarget != null) 
            cameraTarget.localRotation = Quaternion.identity;
    }

    private bool CanControl =>
        applicationFocused &&
        sessionControlAllowed &&
        presentationLocks.Count == 0 &&
        cameraManager != null &&
        cameraManager.IsPlayerView &&
        player != null &&
        player.CanAct &&
        Time.timeScale > 0f;

    public bool CanInteractNow =>
        isActiveAndEnabled &&
        CanControl &&
        controlsActive &&
        Time.frameCount != controlStartFrame;

    private void Awake()
    {
        if (player == null)
            player = new Player();

        characterController = GetComponent<CharacterController>();

        inputMap = new InputActionMap("PlayerControls");

        moveAction = inputMap.AddAction("Move", InputActionType.Value);

        moveAction.AddCompositeBinding("2DVector")
            .With("Up", "<Keyboard>/w")
            .With("Down", "<Keyboard>/s")
            .With("Left", "<Keyboard>/a")
            .With("Right", "<Keyboard>/d");

        lookAction = inputMap.AddAction(
            "Look",
            InputActionType.Value,
            "<Mouse>/delta");

        returnToCCTVAction = inputMap.AddAction("ReturnToCCTV", InputActionType.Button, "<Keyboard>/tab");

        if (cameraTarget != null)
        {
            pitch = Mathf.DeltaAngle(0f, cameraTarget.localEulerAngles.x);
        }
    }

    private void OnEnable()
    {
        inputMap.Enable();
    }

    private void Start()
    {
        if (cameraManager == null || cameraTarget == null)
        {
            Debug.LogError(
                "[PlayerController] CameraManager와 CameraTarget을 연결하세요.",
                this);

            enabled = false;
        }
    }

    private void OnDisable()
    {
        inputMap?.Disable();
        controlsActive = false;
        UnlockCursor();
    }

    private void OnDestroy()
    {
        inputMap?.Dispose();
    }

    private void Update()
    {
        bool canControl = CanControl;

        if (canControl &&
            returnToCCTVAction.WasPressedThisFrame())
        {
            if (ReturnRequested != null) ReturnRequested.Invoke();
            else cameraManager.ShowCCTV();
            canControl = CanControl;
        }

        SynchronizeControlState(canControl);

        // 시점 전환 프레임의 마우스 입력은 무시합니다.
        if (canControl && Time.frameCount != controlStartFrame)
            UpdateLook();

        // CCTV 중에도 중력과 바닥 접촉 처리는 유지합니다.
        UpdateMovement(canControl);
    }

    private void SynchronizeControlState(bool canControl)
    {
        if (controlsActive == canControl)
            return;

        controlsActive = canControl;

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
        Vector2 input = canControl
            ? moveAction.ReadValue<Vector2>()
            : Vector2.zero;

        input = Vector2.ClampMagnitude(input, 1f);

        Vector3 direction = transform.right * input.x + transform.forward * input.y;

        if (characterController.isGrounded && verticalSpeed < 0f)
            verticalSpeed = -2f;

        verticalSpeed += gravity * Time.deltaTime;

        Vector3 velocity = direction * moveSpeed;
        velocity.y = verticalSpeed;

        Vector3 positionBeforeMove = transform.position;
        CollisionFlags flags = characterController.Move(velocity * Time.deltaTime);
        Vector3 displacement = transform.position - positionBeforeMove;
        displacement.y = 0f;
        // 입력만 있고 벽에 막힌 경우에는 발소리를 재생하지 않음.
        // 중력에 의한 수직 이동도 발소리 판정에서 제외.
        float minimumDistance = 0.01f * Time.deltaTime;
        movedThisFrame = canControl && input.sqrMagnitude > 0.0001f && displacement.sqrMagnitude > minimumDistance * minimumDistance;
        movementSampleFrame = Time.frameCount;
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
            UnlockCursor();
        }
    }

    private bool IsPointerOverUI()
    {
        return EventSystem.current != null &&
               EventSystem.current.IsPointerOverGameObject();
    }
}
