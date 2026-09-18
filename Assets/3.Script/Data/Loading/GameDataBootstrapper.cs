using System;
using System.Collections.Generic;
using UnityEngine;

public sealed class GameDataBootstrapper : MonoBehaviour
{
    [Header("CSV 파일 목록")]
    [SerializeField] private GameDataCatalog catalog;

    public DataManager Data { get; private set; }
    public bool IsLoaded => Data != null;

    private void Awake()
    {
        if (catalog == null)
        {
            Debug.LogError("[데이터 초기화] CSV 파일 목록을 연결하세요.", this);
            enabled = false;
            return;
        }

        if (!catalog.TryValidate(out string error))
        {
            Debug.LogError($"[데이터 초기화] {error}", this);
            enabled = false;
            return;
        }

        // 모두 성공하기 전까지 외부에 공개하지 않는 임시 저장소입니다.
        var pendingData = new DataManager();

        var readers = new Dictionary<string, Action<TextAsset>>(StringComparer.OrdinalIgnoreCase)
        {
            { "Anomaly", csv => LoadTable<AnomalyData>(csv, pendingData) },
            { "Detail", csv => LoadTable<DetailData>(csv, pendingData) },
            { "MovementParameter", csv => LoadTable<MovementParameterData>(csv, pendingData) },
            { "TargetCodes", csv => LoadTable<TargetCodeData>(csv, pendingData) },
            { "Quest", csv => LoadTable<QuestData>(csv, pendingData) },
            { "Schedule", csv => LoadTable<ScheduleData>(csv, pendingData) },
            { "Round", csv => LoadTable<RoundData>(csv, pendingData) }
        };

        var loadedNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        bool hasError = false;

        foreach (TextAsset csv in catalog.CsvFiles)
        {
            if (!readers.TryGetValue(csv.name, out Action<TextAsset> reader))
            {
                hasError = true;
                Debug.LogError($"[데이터 초기화] '{csv.name}'의 데이터 형식이 등록되지 않았습니다.", this);
                continue;
            }

            try
            {
                reader(csv);
                loadedNames.Add(csv.name);
            }
            catch (FormatException exception)
            {
                hasError = true;
                Debug.LogError($"[데이터 로드 실패] {exception.Message}", this);
            }
        }

        foreach (string requiredName in readers.Keys)
        {
            if (loadedNames.Contains(requiredName))
                continue;

            hasError = true;
            Debug.LogError($"[데이터 초기화] 필수 시트 '{requiredName}'가 누락되었거나 로드에 실패했습니다.", this);
        }

        if (hasError)
        {
            Debug.LogError("[데이터 초기화 실패] 저장소를 공개하지 않습니다.", this);
            enabled = false;
            return;
        }

        List<string> validationErrors = GameDataValidator.Validate(pendingData);

        if (validationErrors.Count > 0)
        {
            foreach (string message in validationErrors)
                Debug.LogError($"[데이터 검증 실패] {message}", this);

            Debug.LogError($"[데이터 초기화 실패] 참조 검증 오류 {validationErrors.Count}개. 저장소를 공개하지 않습니다.", this);
            enabled = false;
            return;
        }
        pendingData.Seal();
        Data = pendingData;
        Debug.Log("[데이터 참조 검증 성공]", this);
        Debug.Log($"[데이터 저장 완료] {Data.TableCount}개 테이블", this);

        // // 저장소의 ID 조회 기능을 확인하는 임시 코드입니다.
        // if (Data.TryGetData<AnomalyData>(1, out AnomalyData sample))
        //     Debug.Log($"[ID 조회 성공] ID: {sample.ID} | 상세 ID: {sample.MainDetailIDs} | 발생: {sample.BeginMethod} | 복구: {sample.ResolveMethod}", this);
    }

    private void LoadTable<T>(TextAsset csv, DataManager destination) where T : class, ICSVData, new()
    {
        var rows = CSVParser.Read(csv);
        List<T> data = CsvMapper.ConvertRows<T>(rows, csv.name);
        destination.AddTable(data, csv.name);
        Debug.Log($"[테이블 저장 성공] {csv.name}: {data.Count}개", this);
    }
}