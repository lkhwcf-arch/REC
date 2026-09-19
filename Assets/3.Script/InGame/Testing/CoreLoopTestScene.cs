using System;
using System.Collections.Generic;
using System.Linq;
using REC.Core;
using UnityEngine;
using UnityEngine.Rendering;

// TestScene 전용 구성입니다. 원래 테스트 오브젝트는 삭제하지 않고 플레이 중에만 숨깁니다.
[DefaultExecutionOrder(-500)]
public class CoreLoopTestScene : MonoBehaviour
{
    [SerializeField] private GameDataBootstrapper dataBootstrapper;
    [SerializeField] private Material highlightMaterial;
    [SerializeField, Min(1)] private int firstRoundId = 1;
    [SerializeField] private int randomSeed = 1709;
    [SerializeField, Range(0, 1)] private float outlineOpacity = 0.9f;
    [SerializeField, Range(0, 1)] private float ghostOpacity = 0.15f;
    [SerializeField, Range(0, 0.2f)] private float outlineWidth = 0.04f;
    private readonly List<Material> ownedMaterials = new();
    private GameSessionHost host;
    private CoreLoopTestPlayer player;
    private BoxCollider roomGate;
    private void Awake()
    {
        if (!enabled) return;
        if (gameObject.scene.name != "TestScene") { enabled = false; return; }
        foreach (var root in gameObject.scene.GetRootGameObjects())
        {
            if (root == gameObject || root.GetComponent<GameDataBootstrapper>() != null || !root.activeSelf) continue;
            root.SetActive(false);
        }
    }
    private void Start()
    {
        if (dataBootstrapper == null || !dataBootstrapper.IsLoaded) { Debug.LogError("[코어 시험] GameDataRoot 연결/CSV 로드를 확인하세요.", this); return; }
        Material wall = MakeMaterial(new Color(0.3f, 0.34f, 0.4f));
        Material floor = MakeMaterial(new Color(0.16f, 0.18f, 0.22f));
        Material item = MakeMaterial(new Color(0.65f, 0.72f, 0.8f));
        Material shadow = MakeMaterial(new Color(0.02f, 0.02f, 0.025f));
        Cube("Floor", new Vector3(0, -0.25f, 0), new Vector3(26, 0.5f, 30), floor);
        Cube("Ceiling", new Vector3(0, 5.25f, 0), new Vector3(26, 0.5f, 30), wall);
        Cube("Left", new Vector3(-13, 2.5f, 0), new Vector3(0.5f, 5, 30), wall);
        Cube("Right", new Vector3(13, 2.5f, 0), new Vector3(0.5f, 5, 30), wall);
        Cube("Front", new Vector3(0, 2.5f, 15), new Vector3(26, 5, 0.5f), wall);
        Cube("Back", new Vector3(0, 2.5f, -15), new Vector3(26, 5, 0.5f), wall);
        Cube("ControlRoom_Left", new Vector3(-7.25f, 2.5f, -10), new Vector3(11.5f, 5, 0.4f), wall);
        Cube("ControlRoom_Right", new Vector3(7.25f, 2.5f, -10), new Vector3(11.5f, 5, 0.4f), wall);
        roomGate = Cube("ControlRoom_Gate", new Vector3(0, 2.5f, -10), new Vector3(3, 5, 0.4f), wall).GetComponent<BoxCollider>();
        var lamp = new GameObject("Test Light"); lamp.transform.SetParent(transform); lamp.transform.rotation = Quaternion.Euler(65, -25, 0);
        var roomLight = lamp.AddComponent<Light>(); roomLight.type = LightType.Directional; roomLight.intensity = 1.2f;

        List<AnomalyTargetAdapter> bindings = new();
        var details = dataBootstrapper.Data.GetAllData<DetailData>().OrderBy(d => d.TargetID).GroupBy(d => d.TargetID);
        int index = 0;
        foreach (var group in details)
        {
            var root = new GameObject($"TestTarget_{group.Key:000}"); root.transform.SetParent(transform);
            root.transform.localPosition = new Vector3(-10 + index % 6 * 4, 0.75f, -5 + index / 6 * 4); index++;
            var visual = Cube("Visual", Vector3.zero, new Vector3(0.8f, 1.4f, 0.6f), item, root.transform);
            bool addition = group.Any(d => d.Phenomenon == "ObjectAddition");
            visual.SetActive(!addition);
            var areaObject = new GameObject("InteractionArea"); areaObject.transform.SetParent(root.transform, false);
            var area = areaObject.AddComponent<BoxCollider>(); area.isTrigger = true; area.size = new Vector3(1.2f, 1.6f, 1);
            var mapTarget = root.AddComponent<MapTarget>(); mapTarget.Configure(group.Key, visual);
            var adapter = root.AddComponent<AnomalyTargetAdapter>();
            GameObject shadowObject = null;
            if (group.Any(d => d.Phenomenon == "ShadowAddition"))
            {
                shadowObject = Cube("ShadowPlaceholder", new Vector3(0.6f, -0.71f, 0.6f), new Vector3(1.6f, 0.03f, 1.6f), shadow, root.transform);
                shadowObject.GetComponent<Collider>().enabled = false; shadowObject.SetActive(false);
            }
            AnomalyTargetAdapter.GroupPose[] poses = null;
            if (group.Any(d => d.Phenomenon == "ObjectGroupMovement"))
            {
                poses = new AnomalyTargetAdapter.GroupPose[2];
                for (int i = 0; i < poses.Length; i++)
                {
                    var member = Cube($"GroupMember_{i}", new Vector3(i == 0 ? -0.8f : 0.8f, 0, 0), new Vector3(0.4f, 0.4f, 0.4f), item, visual.transform);
                    poses[i] = new AnomalyTargetAdapter.GroupPose { member = member.transform, abnormalLocalPosition = new Vector3(i == 0 ? -1 : 1, 0.5f, 0.6f), abnormalLocalEuler = new Vector3(0, 35, 0) };
                }
            }
            adapter.Configure(area, shadowObject, poses); bindings.Add(adapter);
        }
        var reset = gameObject.AddComponent<RoundResetScope>(); reset.Configure(transform);
        host = gameObject.AddComponent<GameSessionHost>();
        host.Configure(dataBootstrapper, bindings.ToArray(), reset, firstRoundId, randomSeed);
        var playerObject = new GameObject("Test Player"); playerObject.transform.SetParent(transform);
        player = playerObject.AddComponent<CoreLoopTestPlayer>();
        player.Configure(host, highlightMaterial, outlineOpacity, ghostOpacity, outlineWidth);
        host.Changed += OnSessionChanged;
        host.Initialize();
        var ui = gameObject.AddComponent<CoreLoopTestHud>(); ui.Configure(host, player);
        Debug.Log("[코어 시험] CSV 연결 사물을 생성했습니다. 그룹 이동 위치와 그림자 모양은 테스트용 대체 표현입니다.", this);
    }
    private void OnSessionChanged(SessionEvent message)
    {
        if (message.Kind == "RoundStarted") player.ResetForRound();
        if (message.Kind == "IntermediateReturned") player.EnterControlRoom();
        if (message.Kind == "PatrolStarted") player.LeaveControlRoom();
        if (message.Kind is "RoundCompleted" or "GameClear") player.EnterControlRoom();
        if (roomGate != null)
        {
            bool locked = !host.Session.CanEnterRoom;
            roomGate.enabled = locked; roomGate.GetComponent<Renderer>().enabled = locked;
        }
    }
    private Material MakeMaterial(Color color)
    {
        var shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null) throw new InvalidOperationException("URP Lit Shader가 필요합니다.");
        var material = new Material(shader); material.SetColor("_BaseColor", color); ownedMaterials.Add(material); return material;
    }
    private GameObject Cube(string name, Vector3 position, Vector3 scale, Material material, Transform parent = null)
    {
        var cube = GameObject.CreatePrimitive(PrimitiveType.Cube); cube.name = name;
        cube.transform.SetParent(parent != null ? parent : transform, false); cube.transform.localPosition = position; cube.transform.localScale = scale;
        cube.GetComponent<Renderer>().sharedMaterial = material; return cube;
    }
    private void OnDestroy()
    {
        if (host != null) host.Changed -= OnSessionChanged;
        foreach (var material in ownedMaterials) if (material != null) Destroy(material);
    }
}
