using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public sealed class QuestData : ICSVData
{
    public int ID { get; set; }
    public int RoundID { get; set; }
    public int ScheduleID { get; set; }
    public long RealTime { get; set; }
    public int SpawnType { get; set; }
    public int Weight { get; set; }
    public int AnomalyID { get; set; }
    public string Spot { get; set; }
    public int DirectionGroupID { get; set; }
}
