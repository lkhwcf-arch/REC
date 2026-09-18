using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public sealed class AnomalyData : ICSVData
{
    public int ID { get; set; }

    public int MainDetailIDs { get; set; }

    public string BeginMethod { get; set; }

    public string ResolveMethod { get; set; }

    public int DirectionGroupID { get; set; }
}
