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
    private float holdDuration = 2f;

    // 벽과 대상은 포함하고 Player 레이어는 제외
    [SerializeField]
    private LayerMask interactionRayMask = ~0;
    [SerializeField] private LayerMask interactionAreaMask;

    private InputAction clickAction;

    private Interact currentTarget;
    private Interact pressedTarget;
    private float pressedTime;
    private bool holdTriggered;
    private int enabledFrame;
    private readonly RaycastHit[] areaHits = new RaycastHit[64];
    public void Configure(PlayerController controller, Camera camera, int solidMask, int areaMask)
    { playerController = controller; playerCamera = camera; interactionRayMask = solidMask; interactionAreaMask = areaMask; }

    public event Action<Interact> TargetChanged;
    public event Action<Interact> InteractionStarted;
    public event Action<Interact> InteractionEnded;
    private bool interactionInProgress;

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
            interactionInProgress = true;

            Debug.Log("[입력] 홀드 시작 이벤트 발생", this);
            InteractionStarted?.Invoke(pressedTarget);
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
        EndInteraction();

        if (target != null && target.CanInteract)
            target.Hold();
    }

    private Interact FindTarget()
    {
        if (playerCamera is null)
            return null;

        Ray ray = playerCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
        float maxDistance = interactionDistance;
        Interact target = null;
        // 벽과 실제 사물 중 가장 가까운 충돌 지점을 찾는다.
        if (Physics.Raycast(ray, out RaycastHit solidHit, maxDistance, interactionRayMask, QueryTriggerInteraction.Ignore))
        {
            maxDistance = solidHit.distance;
            target = FindEnabledInteraction(solidHit.collider.transform);
        }

        // 앞에서 찾은 벽이나 사물보다 가까운 감지 영역만 선택합니다.
        int count = Physics.RaycastNonAlloc(ray, areaHits, maxDistance + 0.01f, interactionAreaMask, QueryTriggerInteraction.Collide);
        if (count == areaHits.Length) return null;
        float nearest = maxDistance + 0.01f;
        for (int i = 0; i < count; i++)
        {
            Interact candidate = FindEnabledInteraction(areaHits[i].collider.transform);
            if (candidate == null || areaHits[i].distance > nearest) continue;
            nearest = areaHits[i].distance; target = candidate;
        }

        return target != null && target.CanInteract ? target : null;
    }
    private static Interact FindEnabledInteraction(Transform node)
    {
        while (node != null)
        {
            if (node.TryGetComponent(out Interact interaction) && interaction.CanInteract) return interaction;
            node = node.parent;
        }
        return null;
    }

    private void CancelHold()
    {
        EndInteraction();
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

    private void OnApplicationPause(bool paused)
    {
        if (paused)
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

    private void EndInteraction()
    {
        if (!interactionInProgress)
            return;

        Interact target = pressedTarget;
        interactionInProgress = false;
        InteractionEnded?.Invoke(target);
    }
    public void CancelCurrentInteraction() => ResetInteraction();
}
