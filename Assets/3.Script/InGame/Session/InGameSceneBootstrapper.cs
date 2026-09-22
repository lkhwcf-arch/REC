using System;
using System.Collections.Generic;
using System.Linq;
using REC.Core;
using UnityEngine;

// 이 씬의 CSV 경로만 해석하는 구성 루트입니다. 프리팹 원본/다른 씬에는 컴포넌트를 추가하지 않습니다.
[DefaultExecutionOrder(-200)]
public class InGameSceneBootstrapper : MonoBehaviour
{
    [Serializable] public class CctvPose { public int index; public Vector3 position; public Vector3 lookAt; }
    [SerializeField] private GameDataBootstrapper dataBootstrapper;
    [SerializeField] private Transform mapRoot;
    [SerializeField] private PlayerController player;
    [SerializeField] private PlayerInteractor interactor;
    [SerializeField] private Camera outputCamera;
    [SerializeField] private CameraManager cameras;
    [SerializeField] private Material highlightMaterial;
    [SerializeField] private GameObject[] legacyTestObjects = Array.Empty<GameObject>();
    [SerializeField] private string controlRoomFloorName = "DutyRoom_FloorFinish";
    [SerializeField] private string controlRoomDoorName = "DutyRoom_Door_ASSEMBLY";
    [SerializeField, Range(0, 1)] private float outlineOpacity = 0.9f;
    [SerializeField, Range(0, 1)] private float ghostOpacity = 0.15f;
    [SerializeField, Range(0, 0.2f)] private float outlineWidth = 0.035f;
    [SerializeField] private CctvPose[] cctvPoses = Array.Empty<CctvPose>();
    [Tooltip("기존 I_1~I_13 외에 데이터 테이블 등록 후 허용할 정확한 맵 경로입니다.")]
    [SerializeField] private string[] additionalTargetBindings = Array.Empty<string>();
    public GameSessionHost Host { get; private set; }
    public InGameSessionController Controller { get; private set; }
    public string Error { get; private set; }
    private bool built;
    private void Awake()
    {
        if (player != null)
            player.SetSessionControl(false);
        foreach (var item in legacyTestObjects)
            if (item != null)
                item.SetActive(false);
    }
    private void Start() => Initialize();
    public void Initialize()
    {
        if (built) return;
        built = true;

        try
        {
            if (dataBootstrapper == null || !dataBootstrapper.IsLoaded || mapRoot == null || player == null || interactor == null || outputCamera == null || cameras == null || highlightMaterial == null)
                throw new InvalidOperationException("데이터/맵/플레이어/카메라/하이라이트 참조가 필요합니다.");

            int areaLayer = LayerMask.NameToLayer("InteractionArea");
            if (areaLayer < 0)
                throw new InvalidOperationException("InteractionArea 레이어가 없습니다.");

            var data = dataBootstrapper.Data;

            foreach (var pose in cctvPoses)
                cameras.ConfigureCctvPose(pose.index, mapRoot.TransformPoint(pose.position), mapRoot.TransformPoint(pose.lookAt));

            Transform roomFloor = UniqueNamed(mapRoot, controlRoomFloorName);
            Transform roomDoor = UniqueNamed(mapRoot, controlRoomDoorName);
            Bounds room = GeometryBounds(roomFloor), entrance = GeometryBounds(roomDoor);

            // 무작위 사물 검색은 하지 않습니다. 실제 사용 중인 미션의 데이터 경로만 먼저 검증합니다.
            var rows = data.GetAllData<QuestData>().Where(q => q.Enabled == 1)
                .Select(q => data.GetData<DetailData>(data.GetData<AnomalyData>(q.AnomalyID).MainDetailIDs).TargetID)
                .Distinct().OrderBy(id => id).Select(id => data.GetData<TargetCodeData>(id)).ToArray();

            Dictionary<string, Transform> sources = new(StringComparer.Ordinal);

            foreach (var row in rows)
            {
                if (string.IsNullOrWhiteSpace(row.SceneBinding))
                    throw new InvalidOperationException($"TargetID={row.ID}: SceneBinding이 없습니다.");

                if (Array.IndexOf(additionalTargetBindings, row.SceneBinding) >= 0)
                {
                    sources[row.SceneBinding] = Required(mapRoot, row.SceneBinding);
                    continue;
                }

                if (!row.SceneBinding.StartsWith("Interection_Obj/Interaction/", StringComparison.Ordinal))
                    throw new InvalidOperationException($"TargetID={row.ID}: 허용된 Interaction 경로가 아닙니다.");

                string path = row.SceneBinding.Substring("Interection_Obj/Interaction/".Length);
                string parent = path.Split('/')[0];

                if (!int.TryParse(parent.Replace("I_", ""), out int number) || number < 1 || number > 13 || (number == 7 && path != "I_7/I_7_2"))
                    throw new InvalidOperationException($"TargetID={row.ID}: 허용되지 않은 대상 {path}");
                sources[row.SceneBinding] = Required(mapRoot, row.SceneBinding);
            }
            var doorRows = data.GetAllData<DoorData>().OrderBy(d => d.ID).ToArray();
            var doorSources = doorRows.Select(d => Required(mapRoot, d.SceneBinding)).ToArray();
            var interiors = doorRows.Select(d => GeometryBounds(Required(mapRoot, d.InteriorBinding)).center).ToArray();

            // 장식물에는 입력 컴포넌트를 만들지 않습니다. 벽/바닥/가구의 물리 충돌만 보완합니다.
            EnsureColliders(mapRoot);
            List<AnomalyTargetAdapter> adapters = new();

            foreach (var group in rows.GroupBy(r => r.SceneBinding).OrderBy(g => g.Key.Count(c => c == '/')))
            {
                Transform source = sources[group.Key]; Bounds bounds = GeometryBounds(source);

                var targetRoot = UnitRoot($"Target_{group.First().ID:000}", source.parent, bounds.center);
                // FBX의 100배 Scale과 -90도 축이 데이터의 cm/Y축 의미를 바꾸지 않도록 중립 Visual을 사용합니다.
                var visual = UnitRoot("Visual", targetRoot, bounds.center);
                source.SetParent(visual, true);

                var target = targetRoot.gameObject.AddComponent<MapTarget>(); target.Configure(group.First().ID, visual.gameObject);
                bool addition = data.GetAllData<DetailData>().Any(d => group.Any(r => r.ID == d.TargetID) && d.Phenomenon == "ObjectAddition");
                visual.gameObject.SetActive(!addition);

                var areaObject = UnitRoot("InteractionArea", targetRoot, bounds.center); areaObject.gameObject.layer = areaLayer;
                var area = areaObject.gameObject.AddComponent<BoxCollider>(); area.isTrigger = true;
                area.size = Vector3.Max(bounds.size + Vector3.one * 0.035f, Vector3.one * 0.12f);
                var adapter = targetRoot.gameObject.AddComponent<AnomalyTargetAdapter>(); adapter.Configure(area); adapter.ConfigureAliases(group.Select(r => r.ID).ToArray());
                adapter.GetComponent<Interact>().Configure(InteractionInputType.Hold, false); adapters.Add(adapter);
            }

            for (int i = 0; i < doorRows.Length; i++) BuildDoor(doorSources[i], interiors[i], doorRows[i]);
            // CCTV실 문은 신규 상호작용 대상이 아닙니다. 회차 규칙으로만 열리고 닫히는 출입구입니다.

            var reset = gameObject.AddComponent<RoundResetScope>(); reset.Configure(mapRoot);
            Host = gameObject.AddComponent<GameSessionHost>(); Host.enabled = false;
            Host.Configure(dataBootstrapper, adapters.ToArray(), reset);

            Controller = gameObject.AddComponent<InGameSessionController>();
            Controller.Configure(Host, player, cameras, room, entrance, roomDoor, mapRoot);
            Controller.ResultRequested += OnResultRequested;

            var highlights = gameObject.AddComponent<AnomalyHighlightPresenter>();
            highlights.Configure(adapters, highlightMaterial, outlineOpacity, ghostOpacity, outlineWidth);
            interactor.Configure(player, outputCamera, ~(LayerMask.GetMask("Player", "UI", "InteractionArea", "Ignore Raycast")), 1 << areaLayer);
            outputCamera.nearClipPlane = 0.03f;

            var hud = gameObject.AddComponent<InGameHud>(); hud.Configure(Host, Controller, interactor);
            var coffinPresentation = GetComponent<RoundTwoCoffinPresentation>();
            if (coffinPresentation != null && coffinPresentation.enabled)
                coffinPresentation.Configure(Host, adapters, player, interactor, outputCamera);
            Physics.SyncTransforms();
            var roundThree = GetComponent<RoundThreePresentation>();
            if (roundThree != null && roundThree.enabled) roundThree.Configure(Host, player, outputCamera, mapRoot);
            var roundFour = GetComponent<RoundFourPresentation>();
            if (roundFour != null && roundFour.enabled) roundFour.Configure(Host, Controller, player, interactor, mapRoot);
            Host.Initialize();

            if (Host.Session == null || Host.Session.Phase == SessionPhase.ConfigurationError)
                throw new InvalidOperationException(Host.Session?.Error ?? "회차 초기화 실패");

            Host.enabled = true;

            Debug.Log($"[인게임 연결] 사물 {adapters.Count}개 / TargetID {rows.Length}개 / 문 {doorRows.Length}개 / 활성 미션 {data.GetAllData<QuestData>().Count(q => q.Enabled == 1)}개", this);
        }
        catch (Exception exception)
        {
            Error = exception.Message; if (Host != null) Host.enabled = false;
            if (player != null) player.SetSessionControl(false);
            Debug.LogError($"[인게임 연결 실패] {exception}", this);
        }
    }
    private void OnResultRequested(string sceneName)
    {
        if (Application.isPlaying)
            UnityEngine.SceneManagement.SceneManager.LoadScene(sceneName);
    }

    private void OnDestroy() { if (Controller != null) Controller.ResultRequested -= OnResultRequested; }

    private static void BuildDoor(Transform source, Vector3 interior, DoorData row)
    {
        Bounds bounds = GeometryBounds(source);
        Vector3 width = bounds.size.x >= bounds.size.z ? Vector3.right : Vector3.forward;
        float half = Mathf.Max(bounds.extents.x, bounds.extents.z);
        Vector3 hinge = bounds.center + width * (half * row.HingeSide);
        var pivot = UnitRoot($"Door_{row.ID:000}", source.parent, hinge); source.SetParent(pivot, true);
        Vector3 lever = bounds.center - hinge, inward = interior - bounds.center; inward.y = 0;
        float sign = Vector3.Dot(Quaternion.Euler(0, row.OpenAngle, 0) * lever - lever, inward) >= 0 ? 1 : -1;
        pivot.gameObject.AddComponent<DoorInteraction>().Configure(row, sign * row.OpenAngle);
    }
    internal static Transform UnitRoot(string name, Transform parent, Vector3 position)
    {
        var node = new GameObject(name).transform; node.SetPositionAndRotation(position, Quaternion.identity);
        node.SetParent(parent, true); return node;
    }
    internal static Transform Required(Transform root, string path)
    {
        Transform node = root.Find(path);
        if (node == null) throw new InvalidOperationException($"맵 경로를 찾을 수 없습니다: {path}");
        return node;
    }
    private static Transform UniqueNamed(Transform root, string name)
    {
        var matches = root.GetComponentsInChildren<Transform>(true).Where(t => t.name == name).ToArray();
        if (matches.Length != 1) throw new InvalidOperationException($"맵 기준 '{name}'은 정확히 하나여야 합니다. 현재 {matches.Length}개");
        return matches[0];
    }
    public static Bounds GeometryBounds(Transform node)
    {
        var renderers = node.GetComponentsInChildren<Renderer>(true);
        if (renderers.Length == 0) throw new InvalidOperationException($"{node.name}: 표시 모델이 없습니다.");
        Bounds bounds = renderers[0].bounds;
        foreach (var renderer in renderers) bounds.Encapsulate(renderer.bounds);
        return bounds;
    }
    private static void EnsureColliders(Transform root)
    {
        foreach (var filter in root.GetComponentsInChildren<MeshFilter>(true))
        {
            if (filter.sharedMesh == null || filter.TryGetComponent<Collider>(out _)) continue;
            // 정확한 MeshCollider로 복합 벽의 문 구멍/계단을 막지 않습니다. 시작 시 한 번만 구성합니다.
            var collider = filter.gameObject.AddComponent<MeshCollider>(); collider.sharedMesh = filter.sharedMesh;
        }
    }
}
