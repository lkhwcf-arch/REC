#if UNITY_EDITOR
using System;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

public static class InGameIntegrationAudit
{
    [MenuItem("Tools/REC/인게임 프리팹 구조 점검")]
    public static void WriteGeometry()
    {
        StringBuilder report = new();
        foreach (string path in new[] { "Assets/2.Model/Prefabs/Obj/Interection/Interaction.prefab", "Assets/2.Model/Prefabs/Obj/Door/Door.prefab", "Assets/2.Model/Prefabs/Obj/Structure/Structure.prefab" })
        {
            GameObject root = PrefabUtility.LoadPrefabContents(path);
            try
            {
                report.AppendLine(path);
                foreach (Transform node in root.GetComponentsInChildren<Transform>(true))
                {
                    var renderers = node.GetComponentsInChildren<Renderer>(true);
                    if (renderers.Length == 0) continue;
                    Bounds bounds = renderers[0].bounds;
                    foreach (var renderer in renderers) bounds.Encapsulate(renderer.bounds);
                    report.AppendLine($"{AnimationUtility.CalculateTransformPath(node, root.transform)} | pos={node.position:F3} | bounds={bounds.center:F3} size={bounds.size:F3} | scale={node.lossyScale:F3}");
                }
            }
            finally { PrefabUtility.UnloadPrefabContents(root); }
        }
        Directory.CreateDirectory("Temp"); File.WriteAllText("Temp/InGameGeometry.txt", report.ToString());
    }
}
#endif
