using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

// 복합 모델의 모든 Mesh를 공유하고 재질은 두 개만 소유합니다. 소실 중에도 외곽선은 남습니다.
public class AnomalyHighlightPresenter : MonoBehaviour
{
    private class Item
    {
        public AnomalyTargetAdapter owner;
        public Transform source, body, outline;
        public MeshRenderer bodyRenderer, outlineRenderer;
        public MaterialPropertyBlock bodyProperties, outlineProperties;
    }
    private readonly List<Item> items = new();
    private Material bodyMaterial, outlineMaterial;
    private float opacity, ghost, width;
    private static readonly int ColorId = Shader.PropertyToID("_Color"), ExpansionId = Shader.PropertyToID("_Expansion"), CenterId = Shader.PropertyToID("_MeshCenter");
    public void Configure(IEnumerable<AnomalyTargetAdapter> targets, Material material, float outlineOpacity, float ghostOpacity, float outlineWidth)
    {
        opacity = outlineOpacity; ghost = ghostOpacity; width = outlineWidth;
        bodyMaterial = new Material(material) { renderQueue = 3000 }; outlineMaterial = new Material(material) { renderQueue = 3001 };
        bodyMaterial.SetFloat("_StencilComp", (float)CompareFunction.Always); bodyMaterial.SetFloat("_StencilPass", (float)StencilOp.Replace); bodyMaterial.SetFloat("_StencilWriteMask", 1);
        outlineMaterial.SetFloat("_StencilComp", (float)CompareFunction.NotEqual); outlineMaterial.SetFloat("_StencilPass", (float)StencilOp.Keep); outlineMaterial.SetFloat("_StencilWriteMask", 0);
        foreach (var target in targets)
            foreach (var filter in target.Target.Visual.GetComponentsInChildren<MeshFilter>(true))
            {
                if (filter.sharedMesh == null) continue;
                // 자식이 별도의 데이터 대상이라면 그 대상의 표시 소유권을 따릅니다.
                if (filter.GetComponentInParent<AnomalyTargetAdapter>() != target) continue;
                var item = new Item { owner = target, source = filter.transform, bodyProperties = new(), outlineProperties = new() };
                item.bodyRenderer = Create(filter.sharedMesh, bodyMaterial, "HighlightBody"); item.body = item.bodyRenderer.transform;
                item.outlineRenderer = Create(filter.sharedMesh, outlineMaterial, "HighlightOutline"); item.outline = item.outlineRenderer.transform;
                Vector3 center = filter.sharedMesh.bounds.center;
                item.bodyProperties.SetVector(CenterId, center); item.outlineProperties.SetVector(CenterId, center);
                items.Add(item);
            }
    }
    private MeshRenderer Create(Mesh mesh, Material material, string name)
    {
        var node = new GameObject(name); node.transform.SetParent(transform, false); node.layer = LayerMask.NameToLayer("Ignore Raycast");
        node.AddComponent<MeshFilter>().sharedMesh = mesh;
        var renderer = node.AddComponent<MeshRenderer>();
        var materials = new Material[mesh.subMeshCount]; for (int i = 0; i < materials.Length; i++) materials[i] = material;
        renderer.sharedMaterials = materials; renderer.shadowCastingMode = ShadowCastingMode.Off; renderer.receiveShadows = false; renderer.enabled = false;
        return renderer;
    }
    private void LateUpdate()
    {
        foreach (var item in items)
        {
            bool show = item.owner != null && item.owner.Actionable;
            item.bodyRenderer.enabled = show; item.outlineRenderer.enabled = show;
            if (!show || item.source == null) continue;
            Copy(item.source, item.body); Copy(item.source, item.outline);
            item.bodyProperties.SetColor(ColorId, new Color(1, 1, 1, item.source.gameObject.activeInHierarchy ? 0 : ghost));
            item.bodyProperties.SetFloat(ExpansionId, 0); item.bodyRenderer.SetPropertyBlock(item.bodyProperties);
            item.outlineProperties.SetColor(ColorId, new Color(1, 1, 1, opacity)); item.outlineProperties.SetFloat(ExpansionId, width); item.outlineRenderer.SetPropertyBlock(item.outlineProperties);
        }
    }
    private static void Copy(Transform source, Transform destination)
    { destination.SetPositionAndRotation(source.position, source.rotation); destination.localScale = source.lossyScale; }
    private void OnDestroy()
    {
        if (bodyMaterial != null) { if (Application.isPlaying) Destroy(bodyMaterial); else DestroyImmediate(bodyMaterial); }
        if (outlineMaterial != null) { if (Application.isPlaying) Destroy(outlineMaterial); else DestroyImmediate(outlineMaterial); }
    }
}
