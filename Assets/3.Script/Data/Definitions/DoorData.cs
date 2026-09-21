// 일반 문도 허용된 데이터 행으로만 연결합니다. 이상현상 TargetID와는 별도 ID 공간입니다.
public class DoorData : ICSVData
{
    public int ID { get; set; }
    public string SceneBinding { get; set; }
    public string InteriorBinding { get; set; }
    public int HingeSide { get; set; }
    public float OpenAngle { get; set; }
    public float AngularSpeed { get; set; }
}
