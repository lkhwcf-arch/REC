using System;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;

public static class CsvMapper
{
    // 같은 데이터 클래스의 속성 정보를 반복해서 검색하지 않습니다.
    private static class PropertyCache<T>
    {
        public static readonly PropertyInfo[] Properties = typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance);
    }

    public static List<T> ConvertRows<T>(IReadOnlyList<Dictionary<string, string>> rows, string sourceName) where T : class, new()
    {
        if (rows == null)
            throw new ArgumentNullException(nameof(rows));

        // 현재 등록하는 게임 데이터 시트는 최소 한 행이 필요합니다.
        if (rows.Count == 0)
        {
            throw new FormatException($"[{sourceName}] 데이터 행이 없습니다.");
        }

        PropertyInfo[] properties = PropertyCache<T>.Properties;

        ValidateColumns(rows[0], properties, sourceName);

        var result = new List<T>(rows.Count);

        for (int rowIndex = 0; rowIndex < rows.Count; rowIndex++)
        {
            Dictionary<string, string> row = rows[rowIndex];
            var data = new T();

            foreach (PropertyInfo property in properties)
            {
                if (!row.TryGetValue(property.Name, out string raw))
                {
                    throw new FormatException($"[{sourceName}] 데이터 {rowIndex + 1}번째: " + $"'{property.Name}' 열이 없습니다.");
                }

                object value;

                try
                {
                    value = ConvertValue(raw, property.PropertyType);
                }
                catch (Exception exception) when (exception is FormatException || exception is OverflowException)
                {
                    throw new FormatException($"[{sourceName}] 데이터 {rowIndex + 1}번째, " + $"열 '{property.Name}', 값 '{raw}': " +
                        exception.Message, exception);
                }

                property.SetValue(data, value);
            }

            result.Add(data);
        }

        return result;
    }

    private static void ValidateColumns(
        Dictionary<string, string> firstRow,
        PropertyInfo[] properties,
        string sourceName)
    {
        if (properties.Length == 0)
        {
            throw new FormatException($"[{sourceName}] 데이터 클래스에 공개 속성이 없습니다.");
        }

        var expectedColumns = new HashSet<string>(StringComparer.Ordinal);

        foreach (PropertyInfo property in properties)
        {
            if (property.GetIndexParameters().Length != 0 || property.GetSetMethod() == null)
            {
                throw new FormatException($"[{sourceName}] '{property.Name}' 속성에는 " + "공개 set 접근자가 필요하며 인덱서는 사용할 수 없습니다.");
            }

            if (!IsSupportedType(property.PropertyType))
            {
                throw new FormatException($"[{sourceName}] '{property.Name}'의 자료형 " + $"'{property.PropertyType.Name}'은 지원하지 않습니다.");
            }

            expectedColumns.Add(property.Name);

            if (!firstRow.ContainsKey(property.Name))
            {
                throw new FormatException($"[{sourceName}] 필수 열 '{property.Name}'이 없습니다.");
            }
        }

        foreach (string column in firstRow.Keys)
        {
            if (!expectedColumns.Contains(column))
            {
                throw new FormatException($"[{sourceName}] 열 '{column}'에 대응하는 " + "데이터 속성이 없습니다.");
            }
        }
    }

    private static bool IsSupportedType(Type type)
    {
        Type actualType = Nullable.GetUnderlyingType(type) ?? type;

        return actualType == typeof(string)
            || actualType == typeof(int)
            || actualType == typeof(long)
            || actualType == typeof(float)
            || actualType == typeof(double)
            || actualType == typeof(int[]);
    }

    private static object ConvertValue(string raw, Type type)
    {
        // 문자열은 원문을 보존합니다.
        if (type == typeof(string))
            return raw;

        Type nullableType = Nullable.GetUnderlyingType(type);

        if (nullableType != null)
        {
            if (string.IsNullOrWhiteSpace(raw))
                return null;

            type = nullableType;
        }

        if (type == typeof(int[]))
        {
            if (string.IsNullOrWhiteSpace(raw))
                return Array.Empty<int>();

            string[] parts = raw.Split(
                new[] { ',', ';' },
                StringSplitOptions.None);

            var values = new int[parts.Length];

            for (int i = 0; i < parts.Length; i++)
            {
                values[i] = int.Parse(parts[i].Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture);
            }

            return values;
        }

        string text = raw.Trim();

        if (text.Length == 0)
        {
            throw new FormatException("필수 숫자 값이 비어 있습니다.");
        }

        if (type == typeof(int))
        {
            return int.Parse(text, NumberStyles.Integer, CultureInfo.InvariantCulture);
        }

        if (type == typeof(long))
        {
            return long.Parse(text, NumberStyles.Integer, CultureInfo.InvariantCulture);
        }

        if (type == typeof(float))
        {
            float value = float.Parse(text, NumberStyles.Float, CultureInfo.InvariantCulture);

            if (float.IsNaN(value) || float.IsInfinity(value))
                throw new FormatException("유한한 숫자가 필요합니다.");

            return value;
        }

        if (type == typeof(double))
        {
            double value = double.Parse(text, NumberStyles.Float, CultureInfo.InvariantCulture);

            if (double.IsNaN(value) || double.IsInfinity(value))
                throw new FormatException("유한한 숫자가 필요합니다.");

            return value;
        }

        throw new FormatException($"지원하지 않는 자료형입니다: {type.Name}");
    }
}