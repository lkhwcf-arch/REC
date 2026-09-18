public sealed class MovementParameterData : ICSVData
{
    public int ID { get; set; }
    public int TargetID { get; set; }
    public string MoveType { get; set; }
    public string Axis { get; set; }
    public float? Value { get; set; }
    public string Unit { get; set; }
}