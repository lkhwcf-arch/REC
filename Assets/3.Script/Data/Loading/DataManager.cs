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

    private readonly Dictionary<Type, object> tables = new();
    private bool isSealed;

    public int TableCount => tables.Count;

    internal void AddTable<T>(IReadOnlyList<T> rows, string sourceName) where T : class, ICSVData
    {
        if (isSealed)
            throw new InvalidOperationException("초기화 저장소에는 테이블을 추가 할 수 없다");

        if (rows is null)
            throw new ArgumentNullException(nameof(rows));

        if (tables.ContainsKey(typeof(T)))
            throw new FormatException($"[{sourceName}] '{typeof(T).Name}' 테이블이 중복");
        if (rows.Count == 0)
            throw new FormatException($"[{sourceName}] 데이터가 없습니다.");

        var table = new Dictionary<int, T>(rows.Count);

        for (int i = 0; i < rows.Count; i++)
        {
            T row = rows[i];

            if (row == null)
                throw new FormatException($"[{sourceName}] 데이터 {i + 1}번째가 null.");
            if (row.ID <= 0)
                throw new FormatException($"[{sourceName}] 데이터 {i + 1}번째의 ID는 양수여야 함. 현재 값: {row.ID}");

            if (table.ContainsKey(row.ID))
                throw new FormatException($"[{sourceName}] 중복 ID: {row.ID}");

            table.Add(row.ID, row);
        }
        // 한 시트의 모든 행을 검사한 뒤 등록합니다.
        tables.Add(typeof(T), table);
    }

    internal void Seal()
    {
        isSealed = true;
    }

    public T GetData<T>(int id) where T : class, ICSVData
    {
        Dictionary<int, T> table = GetTable<T>();
        if (table.TryGetValue(id, out T data))
            return data;

        throw new KeyNotFoundException($"[{typeof(T).Name}] ID {id}를 찾을 수 없습니다.");
    }
    public bool TryGetData<T>(int id, out T data) where T : class, ICSVData
    {
        return GetTable<T>().TryGetValue(id, out data);
    }

    public IEnumerable<T> GetAllData<T>() where T : class, ICSVData
    {
        return GetTable<T>().Values;
    }

    private Dictionary<int, T> GetTable<T>() where T : class, ICSVData
    {
        if (!tables.TryGetValue(typeof(T), out object table))
            throw new InvalidOperationException($"'{typeof(T).Name}' 테이블이 등록되지 않았습니다.");

        return (Dictionary<int, T>)table;
    }
}