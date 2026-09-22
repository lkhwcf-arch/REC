using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;

// 화면 효과는 HUD(정렬 100) 아래에 배치합니다. 흔들림은 카메라 렌더 동안만 적용하고 즉시 복구합니다.
public sealed class PresentationScreenView : MonoBehaviour
{
    private Camera view;
    private Image blackout;
    private Transform appliedCamera;
    private Vector3 savedPosition;
    private Quaternion savedRotation;
    private float shakeTime, shakeStrength;
    public void Configure(Camera camera)
    {
        view = camera;
        var root = new GameObject("PresentationBlackout", typeof(RectTransform), typeof(Canvas));
        root.transform.SetParent(transform, false);
        var canvas = root.GetComponent<Canvas>(); canvas.renderMode = RenderMode.ScreenSpaceOverlay; canvas.sortingOrder = 90;
        var panel = new GameObject("Black", typeof(RectTransform), typeof(Image)); panel.transform.SetParent(root.transform, false);
        blackout = panel.GetComponent<Image>(); blackout.raycastTarget = false;
        var rect = blackout.rectTransform; rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one; rect.offsetMin = rect.offsetMax = Vector2.zero;
        SetBlackout(0);
    }
    public void SetBlackout(float alpha)
    {
        if (blackout != null) { blackout.color = new Color(0, 0, 0, Mathf.Clamp01(alpha)); blackout.enabled = alpha > 0f; }
    }
    public void SetShake(float elapsed, float strength) { shakeTime = elapsed; shakeStrength = strength; }
    public void ResetView() { SetBlackout(0); shakeStrength = 0; RestoreCamera(); }
    private void OnEnable()
    {
        RenderPipelineManager.beginCameraRendering += BeginCamera;
        RenderPipelineManager.endCameraRendering += EndCamera;
    }
    private void BeginCamera(ScriptableRenderContext _, Camera camera)
    {
        if (camera != view || shakeStrength <= 0 || Time.timeScale <= 0 || !Application.isFocused) return;
        RestoreCamera(); appliedCamera = camera.transform; savedPosition = appliedCamera.position; savedRotation = appliedCamera.rotation;
        appliedCamera.rotation = savedRotation * Quaternion.Euler(Mathf.Sin(shakeTime * 47f), Mathf.Sin(shakeTime * 61f), Mathf.Sin(shakeTime * 39f)) * Quaternion.identity;
        // 각도를 강도로 보간하므로 원본 시점 회전은 누적되지 않습니다.
        appliedCamera.rotation = Quaternion.SlerpUnclamped(savedRotation, appliedCamera.rotation, shakeStrength);
        appliedCamera.position = savedPosition + camera.transform.right * (Mathf.Sin(shakeTime * 71f) * shakeStrength * 0.01f);
    }
    private void EndCamera(ScriptableRenderContext _, Camera camera) { if (camera == view) RestoreCamera(); }
    private void RestoreCamera()
    {
        if (appliedCamera == null) return;
        appliedCamera.SetPositionAndRotation(savedPosition, savedRotation); appliedCamera = null;
    }
    private void OnDisable()
    {
        RenderPipelineManager.beginCameraRendering -= BeginCamera;
        RenderPipelineManager.endCameraRendering -= EndCamera;
        ResetView();
    }
}
