using System;
using REC.Core;
using UnityEngine;

[RequireComponent(typeof(MapTarget), typeof(Interact))]
public class AnomalyTargetAdapter : MonoBehaviour, IAnomalyBody, IRecoveryAudioTarget
{
    [Serializable]
    public class GroupPose
    {
        public Transform member;
        public Vector3 abnormalLocalPosition;
        public Vector3 abnormalLocalEuler;
    }
    [SerializeField] private BoxCollider interactionArea;
    [SerializeField] private GameObject shadowVisual;
    [SerializeField] private GameObject replacementVisual;
    [SerializeField] private GroupPose[] groupPoses = Array.Empty<GroupPose>();
    private MapTarget target;
    private Interact interaction;
    private Snapshot[] baseline;
    private GameSession session;
    public int TargetId => Target.TargetId;
    public MapTarget Target => target != null ? target : target = GetComponent<MapTarget>();
    public bool Actionable { get; private set; }
    private int[] targetIds;
    public int[] TargetIds => targetIds ??= new[] { TargetId };
    public void ConfigureAliases(int[] ids) => targetIds = ids;
    private struct Snapshot
    {
        public Transform transform;
        public Vector3 position, scale;
        public Quaternion rotation;
        public bool active;
        public Snapshot(Transform transform)
        {
            this.transform = transform; position = transform.localPosition; scale = transform.localScale;
            rotation = transform.localRotation; active = transform.gameObject.activeSelf;
        }
        public void Restore()
        {
            if (transform == null) return;
            transform.localPosition = position; transform.localRotation = rotation; transform.localScale = scale;
            transform.gameObject.SetActive(active);
        }
    }
    public void Configure(BoxCollider area, GameObject shadow = null, GroupPose[] group = null)
    {
        interactionArea = area; shadowVisual = shadow; groupPoses = group ?? Array.Empty<GroupPose>();
    }
    public void Bind(GameSession owner)
    {
        Unbind(); session = owner; interaction = GetComponent<Interact>(); interaction.HoldCompleted += RequestResolve;
    }
    public void Unbind()
    {
        if (interaction != null) interaction.HoldCompleted -= RequestResolve;
        session = null;
    }
    private void RequestResolve()
    {
        if (TryGetRecoveryMission(out MissionRuntime mission))
            session.RequestResolve(mission.OccurrenceId);
    }
    private bool TryGetRecoveryMission(out MissionRuntime selected)
    {
        selected = null;

        if (session == null || !Actionable || session.Phase != SessionPhase.AnomalyPatrol ||
            session.ElapsedMs >= session.Rules.DeadlineMs)
            return false;

        var missions = session.Missions;
        var ids = TargetIds;

        for (int i = 0; i < missions.Count; i++)
        {
            MissionRuntime candidate = missions[i];

            if (candidate.Status != MissionStatus.Active || !session.CanResolve(candidate))
                continue;

            if (selected != null && candidate.OccurrenceId >= selected.OccurrenceId)
                continue;

            for (int j = 0; j < ids.Length; j++)
            {
                if (candidate.TargetId != ids[j])
                    continue;

                selected = candidate;
                break;
            }
        }

        return selected != null;
    }
    public bool Supports(string action, MovementSpec movement, out string error)
    {
        if (!Target.TryValidate(out error)) return false;
        if (!gameObject.activeInHierarchy)
        { error = "대상 루트는 활성 상태여야 합니다."; return false; }
        bool initiallyVisible = baseline != null ? baseline[0].active : Target.IsVisible;
        if (action == "ObjectAddition" && initiallyVisible)
        {
            error = "추가형 사물의 Visual은 초기 상태에서 꺼져 있어야 합니다.";
            return false;
        }
        if (action == "ObjectDeletion" && !initiallyVisible)
        {
            error = "소실형 사물의 Visual은 초기 상태에서 켜져 있어야 합니다.";
            return false;
        }
        if (interactionArea == null || !interactionArea.isTrigger || !interactionArea.transform.IsChildOf(transform) || interactionArea.transform.IsChildOf(Target.Visual.transform))
        {
            error = "Visual 바깥 자식에 Trigger 감지 영역을 연결하세요.";
            return false;
        }
        if (action == "ShadowAddition" && shadowVisual == null)
        {
            error = "그림자 표시 사물이 없습니다.";
            return false;
        }
        if (action == "ObjectReplacement" && replacementVisual == null)
        {
            error = "교체 사물이 없습니다.";
            return false;
        }
        if (shadowVisual != null && (shadowVisual == gameObject || !shadowVisual.transform.IsChildOf(transform) || shadowVisual.transform.IsChildOf(Target.Visual.transform)))
        {
            error = "그림자 표시는 Visual과 독립된 대상 루트의 자식이어야 합니다.";
            return false;
        }
        if (replacementVisual != null && (replacementVisual == gameObject || !replacementVisual.transform.IsChildOf(transform) || replacementVisual.transform.IsChildOf(Target.Visual.transform)))
        {
            error = "교체 표시는 Visual과 독립된 대상 루트의 자식이어야 합니다.";
            return false;
        }
        if (action == "ObjectGroupMovement")
        {
            if (movement.Kind != MovementKind.GroupPosition || groupPoses.Length == 0)
            {
                error = "그룹 이동 위치 설정이 없습니다.";
                return false;
            }

            foreach (var pose in groupPoses)
                if (pose == null || pose.member == null ||
                    !pose.member.IsChildOf(Target.Visual.transform) || pose.member == Target.Visual.transform)
                {
                    error = "그룹 구성원은 Visual 내부 자식이어야 합니다.";
                    return false;
                }
        }
        if (action == "ObjectMovement" && movement.Kind == MovementKind.GroupPosition)
        {
            error = "그룹 이동에는 ObjectGroupMovement가 필요합니다.";
            return false;
        }
        error = null;
        return true;
    }
    public void CaptureBaseline()
    {
        Transform[] transforms = Target.Visual.GetComponentsInChildren<Transform>(true);
        baseline = new Snapshot[transforms.Length];
        for (int i = 0; i < transforms.Length; i++) baseline[i] = new Snapshot(transforms[i]);
    }
    public void RestoreBaseline()
    {
        if (baseline == null) return;
        foreach (var snapshot in baseline) snapshot.Restore();
        if (shadowVisual != null) shadowVisual.SetActive(false);
        if (replacementVisual != null) replacementVisual.SetActive(false);
    }
    public void SetVisible(bool visible) => Target.SetVisible(visible);
    public void Move(MovementSpec movement)
    {
        if (movement.Kind == MovementKind.GroupPosition)
        {
            foreach (var pose in groupPoses) { pose.member.localPosition = pose.abnormalLocalPosition; pose.member.localRotation = Quaternion.Euler(pose.abnormalLocalEuler); }
            return;
        }
        Vector3 axis = movement.Axis switch { MovementAxis.X => Vector3.right, MovementAxis.Y => Vector3.up, _ => Vector3.forward };
        Transform visual = Target.Visual.transform;
        if (movement.Kind == MovementKind.Offset)
            visual.localPosition += axis * movement.Value;
        else
            visual.localRotation *= Quaternion.AngleAxis(movement.Value, axis);
    }
    public void SetShadow(bool visible) => shadowVisual.SetActive(visible);
    public void SetReplacement(bool enabled) { Target.SetVisible(!enabled); replacementVisual.SetActive(enabled); }
    public void SetActionable(bool enabled)
    {
        Actionable = enabled;
        interaction ??= GetComponent<Interact>();
        interaction.SetInteractionEnabled(enabled);
        if (interactionArea == null)
            return;
        interactionArea.transform.SetPositionAndRotation(Target.Visual.transform.position, Target.Visual.transform.rotation);
        interactionArea.enabled = enabled; // 그룹·그림자·소실도 동일한 진입점을 사용합니다.
    }
    public bool TryGetRecoveryAudio(out RecoverySoundType soundType, out Transform soundAnchor)
    {
        soundType = RecoverySoundType.None;
        soundAnchor = null;

        if (!TryGetRecoveryMission(out MissionRuntime mission))
            return false;

        soundType = mission.ResolveAction switch
        {
            "ObjectAddition" => RecoverySoundType.RestoreMissing,
            "ObjectDeletion" => RecoverySoundType.RemoveAdded,
            _ => RecoverySoundType.None
        };

        // 이동 복구 메서드의 문자열을 추측하지 않고, 확인된 발생 행동으로 분류합니다.
        if (soundType == RecoverySoundType.None &&
            (mission.BeginAction == "ObjectMovement" || mission.BeginAction == "ObjectGroupMovement"))
            soundType = RecoverySoundType.RestoreMovement;

        if (soundType == RecoverySoundType.None)
            return false;

        soundAnchor = interactionArea != null ? interactionArea.transform : transform;
        return true;
    }

    private void OnDestroy() => Unbind();
}
