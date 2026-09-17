using System;
using UnityEngine;

[RequireComponent(typeof(Interact))]
public sealed class MovePhenomenon : MonoBehaviour
{
    [Header("시험용 발생 건")]
    [SerializeField]
    private string occurrenceId = "test-move-001";

    [Header("이상현상 발생 시 이동할 위치")]
    [SerializeField]
    private Transform abnormalPoint;

    private Interact interaction;
    private PhenomenonModel model;

    private Vector3 normalLocalPosition;
    private Quaternion normalLocalRotation;

    // 실제 복구가 끝났을 때만 전달합니다.
    public event Action<string> Resolved;

    private void Awake()
    {
        interaction = GetComponent<Interact>();

        // 정상 상태에서는 해결 대상이 아닙니다.
        interaction.SetInteractionEnabled(false);

        if (abnormalPoint == null ||
            string.IsNullOrWhiteSpace(occurrenceId))
        {
            Debug.LogError(
                "[MovePhenomenon] 발생 건 ID와 이동 위치를 연결하세요.",
                this);

            enabled = false;
            return;
        }

        if (abnormalPoint == transform ||
            abnormalPoint.IsChildOf(transform))
        {
            Debug.LogError(
                "[MovePhenomenon] 이동 위치는 이 물체 바깥에 배치하세요.",
                this);

            enabled = false;
            return;
        }

        // 시작할 때 놓여 있는 위치를 정상 위치로 기억합니다.
        normalLocalPosition = transform.localPosition;
        normalLocalRotation = transform.localRotation;

        model = new PhenomenonModel(occurrenceId);
    }

    private void OnEnable()
    {
        if (interaction != null)
        {
            interaction.SetInteractionEnabled(
                model != null &&
                model.State == PhenomenonState.Active);
        }
    }

    private void OnDisable()
    {
        if (interaction != null)
            interaction.SetInteractionEnabled(false);
    }

    [ContextMenu("시험/이동 이상현상 발생")]
    public void ApplyAnomaly()
    {
        if (!Application.isPlaying ||
            !isActiveAndEnabled ||
            model == null ||
            abnormalPoint == null)
        {
            return;
        }

        if (!model.TryActivate())
            return;

        transform.SetPositionAndRotation(
            abnormalPoint.position,
            abnormalPoint.rotation);

        interaction.SetInteractionEnabled(true);

        Debug.Log(
            $"[이상현상 발생] {model.OccurrenceId}",
            this);
    }

    public void RequestRestore()
    {
        if (!Application.isPlaying ||
            !isActiveAndEnabled ||
            model == null ||
            model.State != PhenomenonState.Active)
        {
            return;
        }

        // 1. 실제 물체를 정상 위치로 복구합니다.
        transform.localPosition = normalLocalPosition;
        transform.localRotation = normalLocalRotation;

        // 2. 복구 완료 상태를 기록합니다.
        if (!model.TryMarkResolved())
            return;

        // 3. 추가 상호작용을 막습니다.
        interaction.SetInteractionEnabled(false);

        // 4. 복구 완료 후에만 알립니다.
        Debug.Log(
            $"[복구 완료] {model.OccurrenceId}",
            this);

        Resolved?.Invoke(model.OccurrenceId);
    }
}