#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;
using UnityEngine.SceneManagement;

// 빌드용 씬 복사본에 충돌을 포함합니다. 원본 씬/프리팹은 저장하거나 수정하지 않습니다.
[BuildCallbackVersion(1)]
public sealed class InGameCollisionBuildProcessor : IProcessSceneWithReport
{
    public int callbackOrder => 0;

    public void OnProcessScene(Scene scene, BuildReport report)
    {
        if (!BuildPipeline.isBuildingPlayer) return;

        int added = 0;
        foreach (GameObject root in scene.GetRootGameObjects())
        foreach (var bootstrapper in root.GetComponentsInChildren<InGameSceneBootstrapper>(true))
        {
            var serialized = new SerializedObject(bootstrapper);
            var map = serialized.FindProperty("mapRoot").objectReferenceValue as Transform;
            if (map == null)
                throw new BuildFailedException($"{scene.name}: 바닥 충돌을 구성할 Map Root가 없습니다.");

            // 런타임 EnsureColliders와 같은 대상을 처리하되 메시 데이터 제거 전에 구성합니다.
            foreach (var filter in map.GetComponentsInChildren<MeshFilter>(true))
            {
                if (filter.sharedMesh == null || filter.TryGetComponent<Collider>(out _)) continue;
                var collider = filter.gameObject.AddComponent<MeshCollider>();
                collider.sharedMesh = filter.sharedMesh;
                added++;
            }
        }
        if (added > 0)
            Debug.Log($"[빌드 맵 충돌] {scene.name}: MeshCollider {added}개를 빌드 씬에 포함했습니다.");
    }
}
#endif
