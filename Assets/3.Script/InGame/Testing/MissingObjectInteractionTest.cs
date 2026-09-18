using UnityEngine;

[RequireComponent(typeof(MapTarget), typeof(Interact))]
public class MissingObjectInteractionTest : MonoBehaviour
{
    [SerializeField] private BoxCollider interactionArea;

    private MapTarget target;
    private Interact interaction;
    private bool initialized;
    private bool isMissing;

    private void Awake()
    {
        target = GetComponent<MapTarget>();
        interaction = GetComponent<Interact>();

        interaction.SetInteractionEnabled(false);

        if (interactionArea != null)
            interactionArea.enabled = false;

        if (!target.TryValidate(out string error))
        {
            Debug.LogError($"[삭제 복구 시험] {error}", this);
            enabled = false;
            return;
        }

        if (interactionArea == null || !interactionArea.isTrigger)
        {
            Debug.LogError("[삭제 복구 시험] Trigger가 켜진 감지 영역을 연결하세요.", this);
            enabled = false;
            return;
        }

        Transform areaTransform = interactionArea.transform;
        Transform visualTransform = target.Visual.transform;

        if (areaTransform == transform || !areaTransform.IsChildOf(transform) ||
            areaTransform == visualTransform || areaTransform.IsChildOf(visualTransform) ||
            visualTransform.IsChildOf(areaTransform))
        {
            Debug.LogError("[삭제 복구 시험] 감지 영역과 Visual은 서로 분리된 자식으로 배치하세요.", this);
            enabled = false;
            return;
        }

        initialized = true;
        ApplyPresentation();
    }

    private void OnEnable()
    {
        if (initialized)
            ApplyPresentation();
    }

    private void OnDisable()
    {
        if (interaction != null)
            interaction.SetInteractionEnabled(false);

        if (interactionArea != null)
            interactionArea.enabled = false;
    }

    [ContextMenu("시험/사물 사라지게 하기")]
    public void BeginMissing()
    {
        if (!Application.isPlaying || !initialized || !isActiveAndEnabled || isMissing)
            return;

        isMissing = true;
        ApplyPresentation();
        Debug.Log($"[사물 삭제 시험] TargetID={target.TargetId}", this);
    }

    public void Restore()
    {
        if (!Application.isPlaying || !initialized || !isActiveAndEnabled || !isMissing)
            return;

        isMissing = false;
        ApplyPresentation();
        Debug.Log($"[사물 복구 시험 성공] TargetID={target.TargetId}", this);
    }

    private void ApplyPresentation()
    {
        target.Visual.SetActive(!isMissing);
        interactionArea.enabled = isMissing;
        interaction.SetInteractionEnabled(isMissing);
    }
}