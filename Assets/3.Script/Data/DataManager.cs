using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface ICSVData
{
    int ID { get; set; }
}

public class DataManager
{
    // Lazy 싱글톤 인스턴스
    private static readonly DataManager instance = new DataManager();
    public static DataManager Instance => instance;
    // Type별로 (ID, Data) 딕셔너리를 보관하는 마스터 딕셔너리
    private readonly Dictionary<Type, object> masterDict = new Dictionary<Type, object>();
    // 시트에서 파싱된 전체 스테이지 데이터 리스트

    private DataManager()
    {
        LoadAllData();
    }
    private void LoadAllData()
    {
        masterDict.Clear();

        // 새로운 시트가 생기면 이 파싱 규칙 한 줄만 추가하면 됨
        //LoadTable<CharacterData>("Character", ParseCharacterData);

    }
    /// <summary>
    /// [통합 로드 함수] 시트 이름과 파서만 전달하면 어떤 데이터 타입이든 자동 로드 및 저장
    /// </summary>
    private void LoadTable<T>(String fileName, Func<Dictionary<string, string>, T> parsFunc) where T : class, ICSVData
    {
        TextAsset csvAsset = Resources.Load<TextAsset>($"Data/{fileName}");
        if (csvAsset == null)
        {
            Debug.LogError($"[DataManager] Resources/Data/{fileName}.csv");
            return;
        }
        List<Dictionary<string, string>> rows = CSVParser.Read(csvAsset);
        Dictionary<int, T> tableDict = new Dictionary<int, T>();

        foreach (var row in rows)
        {
            T data = parsFunc(row);
            if (data == null || data.ID == 0) continue;

            if (!tableDict.ContainsKey(data.ID))
            {
                tableDict.Add(data.ID, data);
            }
            else
            {
                Debug.LogWarning($"[DataManager] {fileName} 테이블에 중복된 ID가 존재합니다: {data.ID}");
            }
        }
        masterDict[typeof(T)] = tableDict;
        Debug.Log($"[DataManager] {fileName} 로드 완료 ({tableDict.Count}개 개체)");
    }
    #region 파서 로직 (CSV Row -> Data Object)

    /*    private StageData ParseStageData(Dictionary<string, string> row)
        {
            int id = CSVParser.ParseInt(row, "ID");
            if (id == 0) return null;

            return new StageData
            {
                ID = id,
                StageName = CSVParser.ParseString(row, "StageName"),
                MonsterIDs = CSVParser.ParseIntList(row, "MonsterIDs"),
                BossMonsterIDs = CSVParser.ParseIntList(row, "BossMonsterIDs"),
                TargetKillCount = CSVParser.ParseInt(row, "TargetKillCount"),
                NextStageID = CSVParser.ParseInt(row, "NextStageID"),
                BackgroundPrefab = CSVParser.ParseString(row, "BackgroundPrefab"),
                RewardGold = CSVParser.ParseInt(row, "RewardGold"),
            };
        }*/


    #endregion

    /// <summary>
    /// ID 기반 단일 데이터 조회 (예: GetData<CharacterData>(101))
    /// </summary>
    public T GetData<T>(int id) where T : class, ICSVData
    {
        if (masterDict.TryGetValue(typeof(T), out object dictObj))
        {
            var dict = dictObj as Dictionary<int, T>;
            if (dict != null && dict.TryGetValue(id, out T data))
            {
                return data;
            }
        }

        Debug.LogError($"[DataManager] {typeof(T).Name} 테이블에서 ID {id}를 찾을 수 없습니다.");
        return null;
    }

    /// <summary>
    /// 특정 데이터 전체 목록 조회 (예: GetAllData<StageData>())
    /// </summary>
    public IEnumerable<T> GetAllData<T>() where T : class, ICSVData
    {
        if (masterDict.TryGetValue(typeof(T), out object dictObj))
        {
            var dict = dictObj as Dictionary<int, T>;
            if (dict != null)
            {
                return dict.Values;
            }
        }

        return null;
    }
}