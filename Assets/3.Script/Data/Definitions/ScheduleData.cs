using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ScheduleData : ICSVData
{
    public int ID { get; set; }
    public int Enabled { get; set; } = 1;
    public int RoundID { get; set; }
    public int SlotOrder { get; set; }
    public string SpawnTime { get; set; }
    public int DrawCount { get; set; }
    public int CandidateCount { get; set; }
    public int WeightSum { get; set; }
}
