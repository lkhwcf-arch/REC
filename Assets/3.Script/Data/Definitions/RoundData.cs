using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public sealed class RoundData : ICSVData
{
    public int ID { get; set; }
    public int NextRoundID { get; set; }
    public string StartTime { get; set; }
    public string ReviewTime { get; set; }
    public int ScheduleCount { get; set; }
    public int FixedCount { get; set; }
    public int RandomCount { get; set; }
}
