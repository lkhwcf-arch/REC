
public sealed class DetailData : ICSVData
{
    public int ID { get; set; }
    public int AnomalyID { get; set; }
    public int TargetID { get; set; }
    public string Phenomenon { get; set; }
    public string Resolve { get; set; }
    public int MoveParamID { get; set; }
    public int[] ParameterIDs { get; set; }
}
