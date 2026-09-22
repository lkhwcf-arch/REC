using System;
using System.Collections.Generic;
using REC.Core;
using UnityEngine;

// 2회차 관 사건의 완료 이벤트를 소비합니다. 미션 상태/해결 권한은 GameSession 소유입니다.
// 대기 시간이 정해지지 않은 연출이므로 제한 시간형 PresentationEffect와 별도 수명을 가집니다.
public sealed class RoundTwoCoffinPresentation : MonoBehaviour
{
    [Header("데이터 연결")]
    [SerializeField, Min(1)] private int roundId = 2;
    [SerializeField, Min(1)] private int questId = 8;
    [SerializeField, Min(1)] private int targetId = 4;
    [Header("여자 귀신")]
    [SerializeField] private GameObject ghostPrefab;
    [SerializeField] private RuntimeAnimatorController ghostController;
    [SerializeField, Min(0.1f)] private float spawnDistance = 2f;
    [SerializeField, Min(0.6f)] private float ghostHeight = 1.7f;
    [SerializeField] private float facingOffset;
    [SerializeField] private Transform fallbackSpawn;
    [Header("인지 및 조작 제한")]
    [SerializeField] private Vector2 viewportHalfSize = new(0.18f, 0.25f);
    [SerializeField, Min(0.1f)] private float recognitionDistance = 5f;
    [SerializeField, Min(0.01f)] private float inputBlockDuration = 1f;
    [SerializeField, Min(0f)] private float disappearDelayAfterBlock = 1.5f;
    [SerializeField, Min(0.02f)] private float sightInterval = 0.05f;
    [SerializeField] private LayerMask obstructionMask = -1;
    [Header("관 내부 임시 안광")]
    [SerializeField] private Vector3 eyesOffset = new(0, -0.12f, 0);
    [SerializeField, Min(0.01f)] private float eyeSpacing = 0.065f;
    [SerializeField, Min(0.001f)] private float eyeSize = 0.025f;

    private enum Stage { Dormant, Open, WaitingForSight, Blocked, WaitingToDisappear, Complete }
    private Stage stage;
    private GameSessionHost host;
    private AnomalyTargetAdapter adapter;
    private PlayerController player;
    private PlayerInteractor interactor;
    private Camera view;
    private Transform lid;
    private Vector3 closedPosition;
    private GameObject ghost, eyes;
    private Animator[] animators;
    private Material eyeMaterial;
    private Vector3 ghostLookPoint;
    private float blockElapsed, sightElapsed, disappearElapsed;
    private bool configured;
    private int physicsMask;

    public void Configure(GameSessionHost owner, IEnumerable<AnomalyTargetAdapter> targets,
        PlayerController actor, PlayerInteractor input, Camera camera)
    {
        if (configured) throw new InvalidOperationException("2회차 연출이 중복 구성되었습니다.");
        foreach (var item in targets)
            if (Array.IndexOf(item.TargetIds, targetId) >= 0) { adapter = item; break; }
        if (adapter == null || ghostPrefab == null || actor == null || input == null || camera == null)
            throw new InvalidOperationException("2회차 관 TargetID / 여자 귀신 / 플레이어 참조를 확인하세요.");
        host = owner; player = actor; interactor = input; view = camera;
        lid = adapter.Target.Visual.transform;
        closedPosition = lid.localPosition;
        physicsMask = obstructionMask.value & ~LayerMask.GetMask("Player", "UI", "InteractionArea", "Ignore Raycast");
        BuildGhost();
        BuildEyes();
        host.Changed += Handle;
        configured = true;
        ResetPresentation();
    }

    private void Handle(SessionEvent message)
    {
        if (message.Kind is "RoundStarted" or "GameOver" or "GameClear" or "ConfigurationError")
        { ResetPresentation(); return; }
        if (!isActiveAndEnabled || message.RoundId != roundId || message.QuestId != questId || message.TargetId != targetId) return;
        if (message.Kind == "MissionOpened" && stage == Stage.Dormant)
        {
            stage = Stage.Open;
            Vector3 center = lid.parent.TransformPoint(closedPosition);
            eyes.transform.SetPositionAndRotation(center + eyesOffset, Quaternion.identity);
            eyes.SetActive(true);
        }
        else if (message.Kind == "MissionResolved" && stage == Stage.Open)
        {
            eyes.SetActive(false);
            stage = SpawnBehindPlayer() ? Stage.WaitingForSight : Stage.Complete;
        }
    }

    private void LateUpdate()
    {
        if (!configured || ghost == null || !ghost.activeSelf) return;
        bool paused = Time.timeScale <= 0f || !Application.isFocused;
        foreach (var animator in animators) animator.speed = paused ? 0f : 1f;
        if (paused) return;
        float delta = Time.unscaledDeltaTime;
        if (stage == Stage.WaitingForSight)
        {
            sightElapsed += delta;
            if (sightElapsed < Mathf.Max(0.02f, sightInterval) || !player.CanInteractNow) return;
            sightElapsed = 0f;
            if (!CanSeeGhost()) return;
            interactor.CancelCurrentInteraction();
            player.SetPresentationLock(this, true);
            blockElapsed = 0f; stage = Stage.Blocked;
            Debug.Log($"[2회차 연출] 귀신 인지: {inputBlockDuration:0.##}초 조작 제한 시작", this);
        }
        else if (stage == Stage.Blocked)
        {
            blockElapsed += delta;
            if (blockElapsed < Mathf.Max(0.01f, inputBlockDuration)) return;
            player.SetPresentationLock(this, false);
            disappearElapsed = 0f;
            stage = Stage.WaitingToDisappear;
        }
        else if (stage == Stage.WaitingToDisappear)
        {
            disappearElapsed += delta;
            if (disappearElapsed < Mathf.Max(0f, disappearDelayAfterBlock)) return;
            ghost.SetActive(false);
            stage = Stage.Complete;
        }
    }

    private bool CanSeeGhost()
    {
        Vector3 point = ghost.transform.TransformPoint(ghostLookPoint);
        Vector3 screen = view.WorldToViewportPoint(point);
        if (screen.z <= 0f || Mathf.Abs(screen.x - 0.5f) > viewportHalfSize.x || Mathf.Abs(screen.y - 0.5f) > viewportHalfSize.y) return false;
        Vector3 delta = point - view.transform.position;
        return delta.sqrMagnitude <= recognitionDistance * recognitionDistance &&
            !Physics.Raycast(view.transform.position, delta.normalized, delta.magnitude, physicsMask, QueryTriggerInteraction.Ignore);
    }

    private bool SpawnBehindPlayer()
    {
        Vector3 forward = Vector3.ProjectOnPlane(view.transform.forward, Vector3.up).normalized;
        if (forward.sqrMagnitude < 0.5f) forward = player.transform.forward;
        Vector3 desired = player.transform.position - forward * spawnDistance;
        Physics.SyncTransforms();
        if (!TryFloorPosition(desired, out Vector3 floor) &&
            (fallbackSpawn == null || !TryFloorPosition(fallbackSpawn.position, out floor)))
        {
            Debug.LogWarning("[2회차 연출] 뒤쪽에 바닥/빈 공간이 없습니다. Fallback Spawn을 입관실의 안전한 위치에 지정하세요.", this);
            return false;
        }
        Vector3 facing = Vector3.ProjectOnPlane(view.transform.position - floor, Vector3.up).normalized;
        ghost.transform.SetPositionAndRotation(floor, Quaternion.LookRotation(facing.sqrMagnitude > 0 ? facing : forward) * Quaternion.Euler(0, facingOffset, 0));
        ghost.SetActive(true);
        Debug.Log("[2회차 연출] 관 해결 완료: 플레이어 뒤쪽에 여자 귀신 생성", this);
        return true;
    }

    private bool TryFloorPosition(Vector3 desired, out Vector3 floor)
    {
        floor = default;
        // 벽 너머에 스폰하지 않고, 플레이어와 같은 층의 바닥으로 제한합니다.
        Vector3 origin = view.transform.position;
        Vector3 end = new(desired.x, origin.y, desired.z);
        if (Physics.Linecast(origin, end, physicsMask, QueryTriggerInteraction.Ignore)) return false;
        if (!Physics.Raycast(desired + Vector3.up, Vector3.down, out RaycastHit hit, 3f, physicsMask, QueryTriggerInteraction.Ignore) || hit.normal.y < 0.7f) return false;
        floor = hit.point + Vector3.up * 0.02f;
        return !Physics.CheckCapsule(floor + Vector3.up * 0.25f, floor + Vector3.up * (ghostHeight - 0.25f), 0.22f, physicsMask, QueryTriggerInteraction.Ignore);
    }

    private void BuildGhost()
    {
        ghost = new GameObject("Round2_CreepyGirl");
        ghost.transform.SetParent(transform, false);
        ghost.SetActive(false);
        GameObject model = Instantiate(ghostPrefab, ghost.transform);
        model.SetActive(true);
        foreach (var node in model.GetComponentsInChildren<Transform>(true)) node.gameObject.layer = 2;
        foreach (var collider in model.GetComponentsInChildren<Collider>(true)) collider.enabled = false;
        foreach (var body in model.GetComponentsInChildren<Rigidbody>(true)) { body.isKinematic = true; body.detectCollisions = false; }
        animators = model.GetComponentsInChildren<Animator>(true);
        foreach (var animator in animators)
        {
            animator.applyRootMotion = false;
            if (ghostController != null) animator.runtimeAnimatorController = ghostController;
        }
        Bounds bounds = InGameSceneBootstrapper.GeometryBounds(model.transform);
        float scale = ghostHeight / Mathf.Max(0.01f, bounds.size.y);
        model.transform.localScale *= scale;
        model.transform.localPosition -= new Vector3(bounds.center.x - ghost.transform.position.x,
            bounds.min.y - ghost.transform.position.y, bounds.center.z - ghost.transform.position.z) * scale;
        ghostLookPoint = new Vector3(0, ghostHeight * 0.8f, 0);
    }

    private void BuildEyes()
    {
        eyes = new GameObject("Round2_CoffinEyes");
        eyes.transform.SetParent(transform, false);
        Shader shader = Shader.Find("Universal Render Pipeline/Unlit");
        if (shader == null) throw new InvalidOperationException("안광용 URP Unlit 셰이더가 없습니다.");
        eyeMaterial = new Material(shader);
        eyeMaterial.SetColor("_BaseColor", Color.white);
        for (int i = 0; i < 2; i++)
        {
            GameObject eye = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            eye.name = i == 0 ? "LeftEye" : "RightEye";
            eye.transform.SetParent(eyes.transform, false);
            eye.transform.localPosition = Vector3.right * ((i - 0.5f) * eyeSpacing);
            eye.transform.localScale = Vector3.one * eyeSize;
            eye.GetComponent<Collider>().enabled = false;
            eye.GetComponent<Renderer>().sharedMaterial = eyeMaterial;
        }
        eyes.SetActive(false);
    }

    private void ResetPresentation()
    {
        if (player != null) player.SetPresentationLock(this, false);
        if (ghost != null) ghost.SetActive(false);
        if (eyes != null) eyes.SetActive(false);
        blockElapsed = sightElapsed = disappearElapsed = 0f;
        stage = Stage.Dormant;
    }

    private void OnDisable() => ResetPresentation();
    private void OnDestroy()
    {
        ResetPresentation();
        if (host != null) host.Changed -= Handle;
        if (ghost != null) Destroy(ghost);
        if (eyes != null) Destroy(eyes);
        if (eyeMaterial != null) Destroy(eyeMaterial);
    }
}
