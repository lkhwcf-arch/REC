using System;
using System.Collections.Generic;
using UnityEngine;


[CreateAssetMenu(fileName = "GameDataCatalog", menuName = "REC/데이터/CSV 파일 목록")]
public sealed class GameDataCatalog : ScriptableObject
{
    [SerializeField] private TextAsset[] csvFiles = Array.Empty<TextAsset>();
    public IReadOnlyList<TextAsset> CsvFiles => csvFiles;

    public bool TryValidate(out string error)
    {
        if(csvFiles == null || csvFiles.Length ==0)
        {
            error = "Not Exist csv file";
            return false;
        }

        var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        for (int i=0; i < csvFiles.Length; i++)
        {
            TextAsset file = csvFiles[i];

            if(file is null)
            {
                error = $"csv 목록의 {i}번파일이 비어 있음";
                return false;
            }
            if (!names.Add(file.name))
            {
                error = $"CSV 이름이 중복되었습니다: {file.name}";
                return false;
            }
            if(string.IsNullOrWhiteSpace(file.text))
            {
                error = $"CSV 내용이 비어 있습니다: {file.name}";
            }
        }
        error = string.Empty;
        return true;
    }
}
