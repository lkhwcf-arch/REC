using UnityEngine;
using UnityEngine.Rendering;

[RequireComponent(typeof(Interact), typeof(MapTarget))]
public class InteractionHighlightView : MonoBehaviour
{
    [Header("입력 연결")]
    [SerializeField] private PlayerInteractor playerInteractor;

    [Header("표시 연결")]
    [SerializeField] private MeshRenderer highlightBody;
    [SerializeField] private MeshRenderer highlightOutline;
    [SerializeField] private Material highlightMaterial;

    [Header("흰색 하이라이트")]
    [SerializeField, Range(0f, 1f)] private float outlineOpacity = 0.9f;
    [SerializeField, Range(0f, 1f)] private float ghostOpacity = 0.15f;
    [SerializeField, Range(0f, 0.2f)] private float outlineWidth = 0.04f;

    private static readonly int ColorId = Shader.PropertyToID("_Color");
    private static readonly int ExpansionId = Shader.PropertyToID("_Expansion");
    private static readonly int MeshCenterId = Shader.PropertyToID("_MeshCenter");

    private Interact interaction;
    private MapTarget target;
    private Material bodyMaterial;
    private Material outlineMaterial;
    private bool initialized;
    private bool subscribed;
    private bool selected;

    private void Awake()
    {
        interaction = GetComponent<Interact>();
        target = GetComponent<MapTarget>();

        SetVisible(false);

        if (!target.TryValidate(out string error))
        {
            Debug.LogError($"[하이라이트] {error}", this);
            enabled = false;
            return;
        }

        if (playerInteractor == null || highlightBody == null || highlightOutline == null || highlightMaterial == null)
        {
            Debug.LogError("[하이라이트] PlayerInteractor, 표시 Renderer 두 개, Material을 연결하세요.", this);
            enabled = false;
            return;
        }

        if (highlightBody == highlightOutline || !IsSeparateVisual(highlightBody) || !IsSeparateVisual(highlightOutline))
        {
            Debug.LogError("[하이라이트] 표시 오브젝트 두 개는 실제 Visual과 분리된 형제여야 합니다.", this);
            enabled = false;
            return;
        }

        if (highlightMaterial.shader.name != "REC/InteractionHighlight")
        {
            Debug.LogError("[하이라이트] REC/InteractionHighlight Shader를 사용하는 Material을 연결하세요.", this);
            enabled = false;
            return;
        }

        MeshFilter bodyFilter = highlightBody.GetComponent<MeshFilter>();
        MeshFilter outlineFilter = highlightOutline.GetComponent<MeshFilter>();

        if (bodyFilter == null || outlineFilter == null || bodyFilter.sharedMesh == null || bodyFilter.sharedMesh != outlineFilter.sharedMesh)
        {
            Debug.LogError("[하이라이트] 두 표시 오브젝트에 같은 Mesh를 연결하세요.", this);
            enabled = false;
            return;
        }

        bodyMaterial = new Material(highlightMaterial) { renderQueue = 3000 };
        outlineMaterial = new Material(highlightMaterial) { renderQueue = 3001 };

        bodyMaterial.SetFloat("_StencilComp", (float)CompareFunction.Always);
        bodyMaterial.SetFloat("_StencilPass", (float)StencilOp.Replace);
        bodyMaterial.SetFloat("_StencilWriteMask", 1f);

        outlineMaterial.SetFloat("_StencilComp", (float)CompareFunction.NotEqual);
        outlineMaterial.SetFloat("_StencilPass", (float)StencilOp.Keep);
        outlineMaterial.SetFloat("_StencilWriteMask", 0f);

        Vector3 center = bodyFilter.sharedMesh.bounds.center;
        Vector4 meshCenter = new(center.x, center.y, center.z, 0f);
        bodyMaterial.SetVector(MeshCenterId, meshCenter);
        outlineMaterial.SetVector(MeshCenterId, meshCenter);

        ConfigureRenderer(highlightBody, bodyMaterial);
        ConfigureRenderer(highlightOutline, outlineMaterial);
        initialized = true;
    }

    private bool IsSeparateVisual(MeshRenderer renderer)
    {
        return renderer.gameObject != target.Visual && renderer.transform.parent == target.Visual.transform.parent;
    }

    private static void ConfigureRenderer(MeshRenderer renderer, Material material)
    {
        renderer.sharedMaterial = material;
        renderer.shadowCastingMode = ShadowCastingMode.Off;
        renderer.receiveShadows = false;
    }

    private void OnEnable()
    {
        if (!initialized)
            return;

        playerInteractor.TargetChanged += OnTargetChanged;
        subscribed = true;
        OnTargetChanged(playerInteractor.CurrentTarget);
    }

    private void OnDisable()
    {
        if (subscribed && playerInteractor != null)
            playerInteractor.TargetChanged -= OnTargetChanged;

        subscribed = false;
        selected = false;
        SetVisible(false);
    }

    private void OnTargetChanged(Interact current)
    {
        selected = current == interaction && interaction.CanInteract && interaction.InputType == InteractionInputType.Hold;

        if (selected)
            UpdateAppearance();

        SetVisible(selected);
    }

    private void LateUpdate()
    {
        if (!initialized || !selected)
            return;

        if (!interaction.CanInteract || playerInteractor.CurrentTarget != interaction)
        {
            selected = false;
            SetVisible(false);
            return;
        }

        UpdateAppearance();
    }

    private void UpdateAppearance()
    {
        Transform source = target.Visual.transform;
        CopyTransform(source, highlightBody.transform);
        CopyTransform(source, highlightOutline.transform);

        float bodyOpacity = target.Visual.activeInHierarchy ? 0f : ghostOpacity;
        bodyMaterial.SetColor(ColorId, new Color(1f, 1f, 1f, bodyOpacity));
        bodyMaterial.SetFloat(ExpansionId, 0f);

        outlineMaterial.SetColor(ColorId, new Color(1f, 1f, 1f, outlineOpacity));
        outlineMaterial.SetFloat(ExpansionId, outlineWidth);
    }

    private static void CopyTransform(Transform source, Transform destination)
    {
        destination.localPosition = source.localPosition;
        destination.localRotation = source.localRotation;
        destination.localScale = source.localScale;
    }

    private void SetVisible(bool visible)
    {
        if (highlightBody != null)
            highlightBody.enabled = visible;

        if (highlightOutline != null)
            highlightOutline.enabled = visible;
    }

    private void OnDestroy()
    {
        if (bodyMaterial != null)
            Destroy(bodyMaterial);

        if (outlineMaterial != null)
            Destroy(outlineMaterial);
    }
}