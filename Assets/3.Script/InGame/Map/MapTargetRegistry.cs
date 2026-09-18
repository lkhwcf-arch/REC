using System;
using System.Collections.Generic;
using UnityEngine;

public class MapTargetRegistry : MonoBehaviour
{
    [Header("데이터 초기화")]
    [SerializeField] private GameDataBootstrapper dataBootstrapper;

    [Header("이 맵의 대상 목록")]
    [SerializeField] private MapTarget[] targets = Array.Empty<MapTarget>();

    private readonly Dictionary<int, MapTarget> targetsById = new();

    public bool IsReady { get; private set; }
    public int Count => targetsById.Count;

    private void Start()
    {
        if (dataBootstrapper == null || !dataBootstrapper.IsLoaded)
        {
            Debug.LogError("[맵 대상 연결 실패] 데이터 초기화가 완료되지 않았습니다.", this);
            return;
        }

        if (targets == null || targets.Length == 0)
        {
            Debug.LogError("[맵 대상 연결 실패] 대상 목록을 연결하세요.", this);
            return;
        }

        Dictionary<int, MapTarget> pendingTargets = new();
        bool hasError = false;

        foreach (MapTarget target in targets)
        {
            if (target == null)
            {
                hasError = true;
                Debug.LogError("[맵 대상 연결 실패] 목록에 비어 있는 항목이 있습니다.", this);
                continue;
            }

            if (!target.TryValidate(out string error))
            {
                hasError = true;
                Debug.LogError($"[맵 대상 연결 실패] {error}", target);
                continue;
            }

            if (!dataBootstrapper.Data.TryGetData<TargetCodeData>(target.TargetId, out _))
            {
                hasError = true;
                Debug.LogError($"[맵 대상 연결 실패] TargetCodes에 ID={target.TargetId}가 없습니다.", target);
                continue;
            }

            if (!pendingTargets.TryAdd(target.TargetId, target))
            {
                hasError = true;
                Debug.LogError($"[맵 대상 연결 실패] Target ID={target.TargetId}가 중복되었습니다.", target);
            }
        }

        if (hasError)
            return;

        foreach (var pair in pendingTargets)
            targetsById.Add(pair.Key, pair.Value);

        IsReady = true;
        Debug.Log($"[맵 대상 연결 성공] {Count}개", this);
    }

    public bool TryGetTarget(int targetId, out MapTarget target)
    {
        target = null;

        if (!IsReady)
            return false;

        return targetsById.TryGetValue(targetId, out target) && target != null;
    }

    private void OnDestroy()
    {
        IsReady = false;
        targetsById.Clear();
    }
}