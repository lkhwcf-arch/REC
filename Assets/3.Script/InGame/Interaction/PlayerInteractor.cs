using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

public sealed class PlayerInteractor : MonoBehaviour
{
    [Header("참조")]
    [SerializeField] private PlayerController playerController;
    [SerializeField] private Camera playerCamera;

    [Header("상호작용")]
    [SerializeField, Min(0.01f)]
    private float interactionDistance = 3f;

    [SerializeField, Min(0.01f)]
    private float holdDuration = 1f;

    // 벽과 대상은 포함하고 Player 레이어는 제외
    [SerializeField]
    private LayerMask interactionRayMask = ~0;

    private InputAction clickAction;

    private Interact currentTarget;
    private Interact pressedTarget;
    private float pressedTime;
    private bool holdTriggered;
    private int enabledFrame;

    public event Action<Interact> TargetChanged;

    public Interact CurrentTarget
    {
        get => currentTarget;

        private set
        {
            if (ReferenceEquals(currentTarget, value))
                return;

            currentTarget = value;
            TargetChanged?.Invoke(currentTarget);
        }
    }

    public float HoldDuration => Mathf.Max(0.01f, holdDuration);
    public float HoldProgress { get; private set; }

    private void Awake()
    {
        clickAction = new InputAction(
            "Interact",
            InputActionType.Button,
            "<Mouse>/leftButton");
    }

    private void OnEnable()
    {
        enabledFrame = Time.frameCount;
        clickAction.Enable();
    }

    private void Start()
    {
        if (playerController != null && playerCamera != null)
            return;

        Debug.LogError(
            "[PlayerInteractor] PlayerController와 Main Camera를 연결하세요.",
            this);

        enabled = false;
    }

    private void LateUpdate()
    {
        if (playerController == null ||
            !playerController.CanInteractNow ||
            Time.frameCount == enabledFrame ||
            IsPointerOverUI())
        {
            ResetInteraction();
            return;
        }

        UpdateInteraction();

        // 실행 결과로 대상이 사라지거나 비활성화될 수 있다
        if (CurrentTarget == null || !CurrentTarget.CanInteract)
            ResetInteraction();
    }

    private void UpdateInteraction()
    {
        CurrentTarget = FindTarget();

        if (clickAction.WasPressedThisFrame())
        {
            CancelHold();

            if (CurrentTarget == null)
                return;

            if (CurrentTarget.InputType == InteractionInputType.Click)
            {
                CurrentTarget.Click();
                return;
            }

            pressedTarget = CurrentTarget;
            pressedTime = Time.time;
        }

        if (pressedTarget == null)
        {
            CancelHold();
            return;
        }

        if (CurrentTarget != pressedTarget ||
            !pressedTarget.CanInteract ||
            pressedTarget.InputType != InteractionInputType.Hold)
        {
            CancelHold();
            return;
        }

        if (clickAction.WasReleasedThisFrame() ||
            !clickAction.IsPressed())
        {
            CancelHold();
            return;
        }

        if (holdTriggered)
            return;

        float elapsed = Time.time - pressedTime;
        float duration = HoldDuration;

        HoldProgress = Mathf.Clamp01(elapsed / duration);

        if (elapsed < duration)
            return;

        // 이벤트에서 대상이 비활성화될 수 있으므로 먼저 기록
        holdTriggered = true;

        Interact target = pressedTarget;
        target.Hold();
    }

    private Interact FindTarget()
    {
        if (playerCamera == null)
            return null;

        Ray ray = playerCamera.ViewportPointToRay(
            new Vector3(0.5f, 0.5f, 0f));

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

    private void CancelHold()
    {
        pressedTarget = null;
        pressedTime = 0f;
        holdTriggered = false;
        HoldProgress = 0f;
    }

    private void ResetInteraction()
    {
        CancelHold();
        CurrentTarget = null;
    }

    private void OnApplicationFocus(bool hasFocus)
    {
        if (!hasFocus)
            ResetInteraction();
    }

    private void OnDisable()
    {
        clickAction?.Disable();
        ResetInteraction();
    }

    private void OnDestroy()
    {
        clickAction?.Dispose();
    }

    private static bool IsPointerOverUI()
    {
        return EventSystem.current != null &&
               EventSystem.current.IsPointerOverGameObject();
    }
}