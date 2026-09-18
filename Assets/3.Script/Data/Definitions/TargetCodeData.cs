using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public sealed class TargetCodeData : ICSVData
{
    public int ID { get; set; }
    public string TargetCode { get; set; }
    public string Spot { get; set; }
    public string SceneBinding { get; set; }
    public string InteractionArea { get; set; }
    public string PrefabAssetPath { get; set; }
}
