using System;
using UnityEngine;

public sealed class GameDataBootstrapper : MonoBehaviour
{
    [Header("CSV 파일 목록")]
    [SerializeField]
    private GameDataCatalog catalog;

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

        bool hasError = false;

        foreach (TextAsset csv in catalog.CsvFiles)
        {
            try
            {
                var rows = CSVParser.Read(csv);
                Debug.Log($"[CSV 해석 성공] {csv.name}: 데이터 {rows.Count}행", this);
            }
            catch (FormatException exception)
            {
                hasError = true;
                Debug.LogError($"[CSV 해석 실패] {exception.Message}", this);
            }
        }

        if (hasError)
        {
            Debug.LogError("[데이터 초기화 실패] CSV 형식을 확인하세요.", this);

            enabled = false;
            return;
        }

        Debug.Log($"[CSV 해석 완료] {catalog.CsvFiles.Count}개", this);
    }
}