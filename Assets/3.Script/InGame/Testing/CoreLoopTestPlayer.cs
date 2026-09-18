using REC.Core;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;

// 시험 공간 입력 어댑터. 게임 규칙은 GameSession의 명령을 통과해야만 바뀝니다.
public class CoreLoopTestPlayer : MonoBehaviour
{
    [SerializeField, Min(0.1f)] private float moveSpeed = 5;
    [SerializeField, Min(0.1f)] private float interactionDistance = 3;
    [SerializeField, Min(0.1f)] private float holdSeconds = 1;
    [SerializeField, Range(0, 1)] private float outlineOpacity = 0.9f;
    [SerializeField, Range(0, 1)] private float ghostOpacity = 0.15f;
    [SerializeField, Range(0, 0.2f)] private float outlineWidth = 0.04f;
    private readonly RaycastHit[] hits = new RaycastHit[32];
    private GameSessionHost host;
    private CharacterController controller;
    private Camera view;
    private AnomalyTargetAdapter hovered, pressed;
    private float verticalSpeed, pitch, hold;
    private bool requireRelease, menuOpen;
    private MeshRenderer bodyRenderer, outlineRenderer;
    private MeshFilter bodyFilter, outlineFilter;
    private Material bodyMaterial, outlineMaterial;
    public AnomalyTargetAdapter Hovered => hovered;
    public float HoldProgress => hold / holdSeconds;
    public bool NearRoom => transform.position.z < -8;
    public bool MenuOpen => menuOpen;
    public void Configure(GameSessionHost owner, Material highlight, float opacity = 0.9f, float ghost = 0.15f, float width = 0.04f)
    {
        host = owner;
        outlineOpacity = opacity; ghostOpacity = ghost; outlineWidth = width;
        controller = gameObject.AddComponent<CharacterController>(); controller.height = 1.8f; controller.radius = 0.3f; controller.center = Vector3.up * 0.9f;
        var cameraObject = new GameObject("Test Camera"); cameraObject.transform.SetParent(transform, false); cameraObject.transform.localPosition = Vector3.up * 1.6f;
        view = cameraObject.AddComponent<Camera>(); view.nearClipPlane = 0.05f; view.farClipPlane = 60; view.fieldOfView = 65;
        cameraObject.AddComponent<AudioListener>();
        if (highlight != null)
        {
            bodyMaterial = new Material(highlight) { renderQueue = 3000 }; outlineMaterial = new Material(highlight) { renderQueue = 3001 };
            bodyMaterial.SetFloat("_StencilComp", (float)CompareFunction.Always); bodyMaterial.SetFloat("_StencilPass", (float)StencilOp.Replace); bodyMaterial.SetFloat("_StencilWriteMask", 1);
            outlineMaterial.SetFloat("_StencilComp", (float)CompareFunction.NotEqual); outlineMaterial.SetFloat("_StencilPass", (float)StencilOp.Keep); outlineMaterial.SetFloat("_StencilWriteMask", 0);
            bodyRenderer = MakeHighlight("Hover Body", bodyMaterial, out bodyFilter); outlineRenderer = MakeHighlight("Hover Outline", outlineMaterial, out outlineFilter);
        }
        ResetForRound();
    }
    private MeshRenderer MakeHighlight(string name, Material material, out MeshFilter filter)
    {
        var item = new GameObject(name); item.transform.SetParent(host.transform, false); filter = item.AddComponent<MeshFilter>();
        var renderer = item.AddComponent<MeshRenderer>(); renderer.sharedMaterial = material; renderer.enabled = false;
        renderer.shadowCastingMode = ShadowCastingMode.Off; renderer.receiveShadows = false; return renderer;
    }
    public void ResetForRound() { Teleport(new Vector3(0, 0.1f, -8)); pitch = 0; menuOpen = false; transform.rotation = Quaternion.identity; view.transform.localPosition = Vector3.up * 1.6f; view.transform.localRotation = Quaternion.identity; }
    public void EnterControlRoom() { Teleport(new Vector3(0, 0.1f, -12)); view.transform.localPosition = Vector3.up * 1.6f; view.transform.localRotation = Quaternion.identity; }
    public void LeaveControlRoom() { Teleport(new Vector3(0, 0.1f, -8)); view.transform.localPosition = Vector3.up * 1.6f; view.transform.localRotation = Quaternion.identity; pitch = 0; menuOpen = false; }
    public void ObserveCctv() { view.transform.position = new Vector3(0, 4.6f, -9); view.transform.rotation = Quaternion.Euler(18, 0, 0); }
    private void Teleport(Vector3 position)
    {
        controller.enabled = false; transform.position = position; controller.enabled = true; verticalSpeed = 0; CancelHold();
    }
    private void Update()
    {
        if (host == null || host.Session == null) return;
        var keyboard = Keyboard.current; var mouse = Mouse.current;
        if (keyboard != null && keyboard.escapeKey.wasPressedThisFrame) menuOpen = !menuOpen;
        bool active = host.Session.CanPatrol && !menuOpen && Application.isFocused && Time.timeScale > 0;
        Cursor.lockState = active ? CursorLockMode.Locked : CursorLockMode.None; Cursor.visible = !active;
        if (!active) { hovered = null; CancelHold(); ShowHighlight(); return; }
        if (keyboard != null)
        {
            if (keyboard.tabKey.wasPressedThisFrame && NearRoom) host.RequestReturn();
            if (keyboard.nKey.wasPressedThisFrame) host.RequestSkip();
        }
        if (!host.Session.CanPatrol) { CancelHold(); return; }
        Vector2 move = Vector2.zero;
        if (keyboard != null) move = new Vector2((keyboard.dKey.isPressed ? 1 : 0) - (keyboard.aKey.isPressed ? 1 : 0), (keyboard.wKey.isPressed ? 1 : 0) - (keyboard.sKey.isPressed ? 1 : 0));
        if (mouse != null)
        {
            Vector2 delta = mouse.delta.ReadValue() * 0.1f; transform.Rotate(0, delta.x, 0);
            pitch = Mathf.Clamp(pitch - delta.y, -80, 80); view.transform.localRotation = Quaternion.Euler(pitch, 0, 0);
        }
        if (controller.isGrounded && verticalSpeed < 0) verticalSpeed = -2;
        verticalSpeed = Mathf.Max(-40, verticalSpeed - 20 * Time.deltaTime);
        Vector3 direction = transform.TransformDirection(new Vector3(move.x, 0, move.y).normalized) * moveSpeed;
        direction.y = verticalSpeed; controller.Move(direction * Time.deltaTime);
        // 벽 충돌과 별도로 카메라가 실외로 나갈 수 없는 시험 공간 경계입니다.
        Vector3 position = transform.position; position.x = Mathf.Clamp(position.x, -12.3f, 12.3f); position.z = Mathf.Clamp(position.z, -14.3f, 14.3f);
        if (!host.Session.CanEnterRoom) position.z = Mathf.Max(-9.4f, position.z);
        if (position.y < -1) { Teleport(new Vector3(0, 0.1f, -8)); } else transform.position = position;
        hovered = host.Session.Phase == SessionPhase.AnomalyPatrol ? FindTarget() : null;
        if (mouse == null || !mouse.leftButton.isPressed) { CancelHold(); requireRelease = false; }
        else if (mouse.leftButton.wasPressedThisFrame && hovered != null && !requireRelease) { pressed = hovered; hold = 0; }
        if (pressed != null)
        {
            if (pressed != hovered || !pressed.Actionable) { CancelHold(); requireRelease = true; }
            else
            {
                hold += Time.deltaTime;
                if (hold >= holdSeconds) { pressed.GetComponent<Interact>().Hold(); CancelHold(); requireRelease = true; }
            }
        }
        ShowHighlight();
    }
    private AnomalyTargetAdapter FindTarget()
    {
        var ray = view.ViewportPointToRay(new Vector3(0.5f, 0.5f));
        int count = Physics.RaycastNonAlloc(ray, hits, interactionDistance, ~0, QueryTriggerInteraction.Collide);
        if (count == hits.Length) return null; // 포화 시 잘못된 관통 선택 대신 선택하지 않습니다.
        float nearestSolid = interactionDistance, nearestTarget = float.MaxValue;
        AnomalyTargetAdapter selected = null;
        for (int i = 0; i < count; i++)
        {
            var hit = hits[i]; if (hit.collider == controller) continue;
            if (!hit.collider.isTrigger) nearestSolid = Mathf.Min(nearestSolid, hit.distance);
            var candidate = hit.collider.GetComponentInParent<AnomalyTargetAdapter>();
            if (candidate != null && candidate.Actionable && hit.distance < nearestTarget) { selected = candidate; nearestTarget = hit.distance; }
        }
        return nearestTarget <= nearestSolid + 0.001f ? selected : null;
    }
    private void ShowHighlight()
    {
        if (bodyRenderer == null) return;
        bool visible = hovered != null && hovered.Actionable;
        bodyRenderer.enabled = visible; outlineRenderer.enabled = visible;
        if (!visible) return;
        var source = hovered.Target.Visual.transform; var mesh = source.GetComponent<MeshFilter>();
        if (mesh == null || mesh.sharedMesh == null) { bodyRenderer.enabled = false; outlineRenderer.enabled = false; return; }
        bodyFilter.sharedMesh = mesh.sharedMesh; outlineFilter.sharedMesh = mesh.sharedMesh;
        bodyRenderer.transform.SetPositionAndRotation(source.position, source.rotation); bodyRenderer.transform.localScale = source.lossyScale;
        outlineRenderer.transform.SetPositionAndRotation(source.position, source.rotation); outlineRenderer.transform.localScale = source.lossyScale;
        Vector3 center = mesh.sharedMesh.bounds.center;
        bodyMaterial.SetVector("_MeshCenter", center); outlineMaterial.SetVector("_MeshCenter", center);
        bodyMaterial.SetColor("_Color", new Color(1, 1, 1, hovered.Target.IsVisible ? 0 : ghostOpacity));
        outlineMaterial.SetColor("_Color", new Color(1, 1, 1, outlineOpacity)); outlineMaterial.SetFloat("_Expansion", outlineWidth);
    }
    private void CancelHold() { pressed = null; hold = 0; }
    private void OnApplicationFocus(bool focused) { if (!focused) { CancelHold(); requireRelease = true; } }
    private void OnDisable() { CancelHold(); Cursor.lockState = CursorLockMode.None; Cursor.visible = true; }
    private void OnDestroy() { if (bodyMaterial != null) Destroy(bodyMaterial); if (outlineMaterial != null) Destroy(outlineMaterial); }
}
