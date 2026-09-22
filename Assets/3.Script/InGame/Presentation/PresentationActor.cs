using System;
using UnityEngine;

[Serializable]
public class PresentationActorSettings
{
    public GameObject prefab;
    public RuntimeAnimatorController controller;
    [Min(0.1f)] public float height = 1.7f;
    public float facingOffset;
}

// 연출 모델의 소유권과 재사용 수명. 플레이어와 충돌하거나 게임 난수를 소비하지 않습니다.
public sealed class PresentationActor : IDisposable
{
    public GameObject Root { get; }
    private readonly Animator[] animators;
    private readonly float facingOffset;
    private bool paused;
    public PresentationActor(Transform owner, string name, PresentationActorSettings settings)
    {
        if (settings == null || settings.prefab == null) throw new InvalidOperationException(name + ": 귀신 모델이 없습니다.");
        facingOffset = settings.facingOffset;
        Root = new GameObject(name); Root.transform.SetParent(owner, false); Root.SetActive(false);
        var model = UnityEngine.Object.Instantiate(settings.prefab, Root.transform);
        model.SetActive(true);
        foreach (var node in model.GetComponentsInChildren<Transform>(true)) node.gameObject.layer = 2;
        foreach (var collider in model.GetComponentsInChildren<Collider>(true)) collider.enabled = false;
        foreach (var body in model.GetComponentsInChildren<Rigidbody>(true)) { body.isKinematic = true; body.detectCollisions = false; }
        animators = model.GetComponentsInChildren<Animator>(true);
        foreach (var animator in animators)
        {
            animator.applyRootMotion = false;
            if (settings.controller != null) animator.runtimeAnimatorController = settings.controller;
        }
        Bounds bounds = InGameSceneBootstrapper.GeometryBounds(model.transform);
        float scale = Mathf.Max(0.1f, settings.height) / Mathf.Max(0.01f, bounds.size.y);
        model.transform.localScale *= scale;
        model.transform.localPosition -= new Vector3(bounds.center.x - Root.transform.position.x,
            bounds.min.y - Root.transform.position.y, bounds.center.z - Root.transform.position.z) * scale;
    }
    public void Show(Vector3 feet, Vector3 direction)
    {
        direction = Vector3.ProjectOnPlane(direction, Vector3.up);
        Root.transform.SetPositionAndRotation(feet, Quaternion.LookRotation(direction.sqrMagnitude > 0.001f ? direction : Vector3.forward) * Quaternion.Euler(0, facingOffset, 0));
        Root.SetActive(true);
    }
    public void SetMotion(int motion)
    {
        foreach (var animator in animators)
            foreach (var parameter in animator.parameters)
                if (parameter.name == "Motion" && parameter.type == AnimatorControllerParameterType.Int) animator.SetInteger("Motion", motion);
    }
    public void SetPaused(bool value)
    {
        if (paused == value) return;
        paused = value;
        foreach (var animator in animators) animator.speed = value ? 0f : 1f;
    }
    public void Hide() { if (Root != null) Root.SetActive(false); }
    public void Dispose() { if (Root != null) UnityEngine.Object.Destroy(Root); }
}
