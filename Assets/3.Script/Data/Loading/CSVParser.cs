using System;
using System.Collections.Generic;
using UnityEngine;

public static class CSVParser
{
    private static readonly char[] TRIM_CHARS = { '\"', ' ', '\t', '\r', '\n' };
    public static List<Dictionary<string, string>> Read(TextAsset data)
    {
        if (data == null)
            throw new ArgumentNullException(nameof(data));

        return Read(data.text, data.name);
    }

    public static List<Dictionary<string, string>> Read(string text, string sourceName)
    {
        if (string.IsNullOrWhiteSpace(text))
            throw new FormatException($"[{sourceName}] CSV 내용이 비어 있습니다.");

        var records = ReadRecords(text, sourceName);

        if (records.Count == 0)
            throw new FormatException($"[{sourceName}] CSV 헤더가 없습니다.");

        List<string> headers = records[0].Cells;
        var headerNames = new HashSet<string>(StringComparer.Ordinal);

        for (int i = 0; i < headers.Count; i++)
        {
            // 열 이름의 바깥 공백만 제거합니다.
            headers[i] = headers[i].Trim();

            if (headers[i].Length == 0)
            {
                throw new FormatException($"[{sourceName}] {records[0].Line}줄: " + $"{i + 1}번째 열 이름이 비어 있습니다.");
            }

            if (!headerNames.Add(headers[i]))
            {
                throw new FormatException($"[{sourceName}] {records[0].Line}줄: " + $"열 이름이 중복되었습니다: {headers[i]}");
            }
        }

        var result = new List<Dictionary<string, string>>();

        for (int i = 1; i < records.Count; i++)
        {
            var record = records[i];

            if (record.Cells.Count != headers.Count)
            {
                throw new FormatException($"[{sourceName}] {record.Line}줄: " + $"열은 {headers.Count}개여야 하지만 " + $"{record.Cells.Count}개입니다.");
            }

            var row = new Dictionary<string, string>(
                StringComparer.Ordinal);

            for (int column = 0; column < headers.Count; column++)
            {
                // 실제 데이터의 공백과 빈 문자열은 보존합니다.
                row.Add(headers[column], record.Cells[column]);
            }

            result.Add(row);
        }

        return result;
    }

    private static List<(int Line, List<string> Cells)> ReadRecords(string text, string sourceName)
    {
        var records = new List<(int Line, List<string> Cells)>();
        var cells = new List<string>();
        var value = new System.Text.StringBuilder();

        bool insideQuotes = false;
        bool closedQuote = false;
        bool recordStarted = false;

        int line = 1;
        int recordLine = 1;

        // UTF-8 파일 시작 표시가 있다면 건너뜁니다.
        int startIndex = text.Length > 0 && text[0] == '\uFEFF'
            ? 1
            : 0;

        for (int i = startIndex; i < text.Length; i++)
        {
            char current = text[i];

            if (insideQuotes)
            {
                if (current == '"')
                {
                    if (i + 1 < text.Length && text[i + 1] == '"')
                    {
                        value.Append('"');
                        i++;
                    }
                    else
                    {
                        insideQuotes = false;
                        closedQuote = true;
                    }
                }
                else if (current == '\r' || current == '\n')
                {
                    if (current == '\r' &&
                        i + 1 < text.Length &&
                        text[i + 1] == '\n')
                    {
                        i++;
                    }

                    value.Append('\n');
                    line++;
                }
                else
                {
                    value.Append(current);
                }

                continue;
            }

            if (current == ',')
            {
                cells.Add(value.ToString());
                value.Clear();

                closedQuote = false;
                recordStarted = true;
                continue;
            }

            if (current == '\r' || current == '\n')
            {
                // 완전히 빈 줄은 건너뜁니다.
                // 쉼표가 있는 빈 데이터 행은 보존합니다.
                if (recordStarted || cells.Count > 0 || value.Length > 0)
                {
                    cells.Add(value.ToString());
                    records.Add((recordLine, cells));
                }

                cells = new List<string>();
                value.Clear();
                closedQuote = false;
                recordStarted = false;

                if (current == '\r' && i + 1 < text.Length && text[i + 1] == '\n')
                {
                    i++;
                }

                line++;
                recordLine = line;
                continue;
            }

            if (closedQuote)
            {
                throw new FormatException($"[{sourceName}] {line}줄: " + "닫는 따옴표 뒤에는 쉼표 또는 줄바꿈이 필요합니다.");
            }

            if (current == '"')
            {
                if (value.Length > 0)
                {
                    throw new FormatException($"[{sourceName}] {line}줄: " + "따옴표는 칸의 시작에서 사용해야 합니다.");
                }

                insideQuotes = true;
                recordStarted = true;
                continue;
            }

            value.Append(current);
            recordStarted = true;
        }

        if (insideQuotes)
        {
            throw new FormatException($"[{sourceName}] {recordLine}줄에서 시작한 " + "따옴표가 닫히지 않았습니다.");
        }

        // 마지막 줄에 줄바꿈이 없어도 처리합니다.
        if (recordStarted || cells.Count > 0 || value.Length > 0)
        {
            cells.Add(value.ToString());
            records.Add((recordLine, cells));
        }

        return records;
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
