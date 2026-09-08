using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;

public static class CSVParser
{
    // 정규표현식 상수 정의 (클래스 내부 선언)
    private static readonly string SPLIT_RE = @",(?=(?:[^""]*""[^""]*"")*[^""]*$)";
    private static readonly char[] TRIM_CHARS = { '\"', ' ', '\t', '\r', '\n' };
    public static List<Dictionary<string, string>> Read(TextAsset data)
    {
        var list = new List<Dictionary<string, string>>();
        // 1. TextAsset null 체크
        if (data == null || string.IsNullOrEmpty(data.text))
        {
            Debug.LogError("[CSVParser] TextAsset 데이터가 비어있거나 null입니다.");
            return list;
        }

        // 2. StringReader를 사용해 한 줄씩 안전하게 읽기
        using (StringReader reader = new StringReader(data.text))
        {
            string headerLine = reader.ReadLine();
            if (string.IsNullOrEmpty(headerLine)) return list;

            // 헤더 파싱
            string[] header = Regex.Split(headerLine, SPLIT_RE);
            for (int i = 0; i < header.Length; i++)
            {
                header[i] = header[i].Trim(TRIM_CHARS);
            }

            // 본문 데이터 한 줄씩 읽기
            string line;
            while ((line = reader.ReadLine()) != null)
            {
                if (string.IsNullOrWhiteSpace(line)) continue;

                string[] values = Regex.Split(line, SPLIT_RE);
                if (values.Length == 0 || string.IsNullOrEmpty(values[0])) continue;

                var entry = new Dictionary<string, string>();
                for (var j = 0; j < header.Length && j < values.Length; j++)
                {
                    string value = values[j].Trim(TRIM_CHARS).Replace("\\n", "\n");
                    entry[header[j]] = value;
                }
                list.Add(entry);
            }
        }

        return list;
    }
    public static int ParseInt(Dictionary<string, string> row, string key, int defaultValue = 0)
    {
        if (row != null && row.TryGetValue(key, out string val) && int.TryParse(val, out int result))
            return result;

        return defaultValue;
    }
    public static string ParseString(Dictionary<string, string> row, string key, string defaultValue = "")
    {
        if (row != null && row.TryGetValue(key, out string val))
        {
            return val;
        }
        return defaultValue;
    }

    // ... 기존 Read, ParseInt, ParseString 등의 코드 ...

    /// <summary>
    /// CSV의 특정 컬럼 문자열(예: "101,102,103" 또는 "101;102;103")을 List<int>로 파싱
    /// </summary>
    /// <param name="row">CSV 행 데이터 Dictionary</param>
    /// <param name="columnName">열 이름</param>
    /// <param name="delimiters">구분자 배열 (기본값: ',' 및 ';')</param>
    /// </summary>
    public static List<int> ParseIntList(Dictionary<string, string> row, string columnName, char[] delimiters = null)
    {
        List<int> resultList = new List<int>();

        if (!row.TryGetValue(columnName, out string rawValue) || string.IsNullOrWhiteSpace(rawValue))
        {
            return resultList;
        }

        // 대괄호 '[', ']' 가 포함되어 있다면 제거
        string cleanedValue = rawValue.Replace("[", "").Replace("]", "").Trim();

        if (string.IsNullOrWhiteSpace(cleanedValue))
        {
            return resultList;
        }

        if (delimiters == null || delimiters.Length == 0)
        {
            delimiters = new char[] { ',', ';' };
        }

        // 유니티 6 (.NET Standard 2.1) 지원
        string[] tokens = cleanedValue.Split(delimiters, StringSplitOptions.RemoveEmptyEntries);

        foreach (string token in tokens)
        {
            string trimmedToken = token.Trim();

            if (int.TryParse(trimmedToken, out int parsedInt))
            {
                resultList.Add(parsedInt);
            }
            else
            {
                Debug.LogWarning($"[CSVParser] ParseIntList 변환 실패 - 컬럼 '{columnName}', 값: '{token}'");
            }
        }
        return resultList;
    }
}