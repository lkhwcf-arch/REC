using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using Michsky.UI.Dark;

public class ReplaceUISounds : EditorWindow
{
    private AudioClip newClickSound;
    private AudioClip newHoverSound;

    [MenuItem("Tools/REC/Replace UI Sounds")]
    public static void ShowWindow()
    {
        GetWindow<ReplaceUISounds>("REC UI Sounds");
    }

    private void OnGUI()
    {
        GUILayout.Space(10);

        GUILayout.Label(
            "REC UI 사운드 일괄 교체",
            EditorStyles.boldLabel
        );

        GUILayout.Space(10);

        // Click Sound
        newClickSound = (AudioClip)EditorGUILayout.ObjectField(
            "새 Click Sound",
            newClickSound,
            typeof(AudioClip),
            false
        );

        GUILayout.Space(5);

        // Hover Sound
        newHoverSound = (AudioClip)EditorGUILayout.ObjectField(
            "새 Hover Sound",
            newHoverSound,
            typeof(AudioClip),
            false
        );

        GUILayout.Space(15);

        EditorGUILayout.HelpBox(
            "프로젝트의 Prefab과 현재 열려있는 Scene에 있는 " +
            "UIElementSound의 Click / Hover Sound를 모두 변경합니다.",
            MessageType.Info
        );

        GUILayout.Space(10);

        GUI.enabled = newClickSound != null || newHoverSound != null;

        if (GUILayout.Button("전체 교체", GUILayout.Height(40)))
        {
            ReplaceAllSounds();
        }

        GUI.enabled = true;

        GUILayout.Space(10);

        if (newClickSound == null && newHoverSound == null)
        {
            EditorGUILayout.HelpBox(
                "Click 또는 Hover 사운드를 하나 이상 넣어주세요.",
                MessageType.Warning
            );
        }
    }

    private void ReplaceAllSounds()
    {
        int sceneCount = 0;
        int prefabCount = 0;

        // =========================
        // 현재 열려있는 Scene
        // =========================

        for (int sceneIndex = 0;
             sceneIndex < SceneManager.sceneCount;
             sceneIndex++)
        {
            Scene scene = SceneManager.GetSceneAt(sceneIndex);

            if (!scene.isLoaded)
                continue;

            GameObject[] rootObjects = scene.GetRootGameObjects();

            foreach (GameObject root in rootObjects)
            {
                UIElementSound[] sounds =
                    root.GetComponentsInChildren<UIElementSound>(true);

                foreach (UIElementSound sound in sounds)
                {
                    bool changed = false;

                    // Click
                    if (newClickSound != null &&
                        sound.clickSound != newClickSound)
                    {
                        Undo.RecordObject(
                            sound,
                            "Replace UI Click Sound"
                        );

                        sound.clickSound = newClickSound;
                        changed = true;
                    }

                    // Hover
                    if (newHoverSound != null &&
                        sound.hoverSound != newHoverSound)
                    {
                        Undo.RecordObject(
                            sound,
                            "Replace UI Hover Sound"
                        );

                        sound.hoverSound = newHoverSound;
                        changed = true;
                    }

                    if (changed)
                    {
                        EditorUtility.SetDirty(sound);
                        sceneCount++;
                    }
                }
            }

            EditorSceneManager.MarkSceneDirty(scene);
        }

        // =========================
        // Prefab
        // =========================

        string[] prefabGUIDs =
            AssetDatabase.FindAssets("t:Prefab");

        foreach (string guid in prefabGUIDs)
        {
            string path =
                AssetDatabase.GUIDToAssetPath(guid);

            GameObject prefabRoot =
                AssetDatabase.LoadAssetAtPath<GameObject>(path);

            if (prefabRoot == null)
                continue;

            UIElementSound[] sounds =
                prefabRoot.GetComponentsInChildren<UIElementSound>(true);

            bool changed = false;

            foreach (UIElementSound sound in sounds)
            {
                // Click
                if (newClickSound != null &&
                    sound.clickSound != newClickSound)
                {
                    sound.clickSound = newClickSound;
                    changed = true;
                    prefabCount++;
                }

                // Hover
                if (newHoverSound != null &&
                    sound.hoverSound != newHoverSound)
                {
                    sound.hoverSound = newHoverSound;
                    changed = true;
                }
            }

            if (changed)
            {
                EditorUtility.SetDirty(prefabRoot);
                PrefabUtility.SavePrefabAsset(prefabRoot);
            }
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        // =========================
        // 결과
        // =========================

        string clickName =
            newClickSound != null
                ? newClickSound.name
                : "변경 안 함";

        string hoverName =
            newHoverSound != null
                ? newHoverSound.name
                : "변경 안 함";

        Debug.Log(
            "========================================\n" +
            "REC UI 사운드 교체 완료!\n" +
            "========================================\n" +
            $"Scene 변경: {sceneCount}개\n" +
            $"Prefab 변경: {prefabCount}개\n" +
            $"Click Sound: {clickName}\n" +
            $"Hover Sound: {hoverName}\n" +
            "========================================"
        );

        EditorUtility.DisplayDialog(
            "교체 완료!",
            $"UI 사운드 교체가 완료되었습니다.\n\n" +
            $"Scene: {sceneCount}개\n" +
            $"Prefab: {prefabCount}개\n\n" +
            $"Click: {clickName}\n" +
            $"Hover: {hoverName}",
            "확인"
        );
    }
}