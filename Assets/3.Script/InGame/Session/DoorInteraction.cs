using UnityEngine;

// 데이터로 허용된 문 한 개를 제어합니다. 각도 진행은 Update, 회차 초기화는 명시적인 계약으로 처리합니다.
[RequireComponent(typeof(Interact))]
public class DoorInteraction : MonoBehaviour, IRoundResetParticipant
{
    private Interact interaction;
    private Quaternion closedRotation;
    private float openAngle, speed, angle;
    private bool opening;
    public int DoorId { get; private set; }
    public void Configure(DoorData row, float signedAngle)
    {
        DoorId = row.ID; openAngle = signedAngle; speed = row.AngularSpeed; closedRotation = transform.localRotation;
        interaction = GetComponent<Interact>(); interaction.Configure(InteractionInputType.Click);
        interaction.ClickCompleted += Open;
    }
    public void Open() { opening = true; interaction.SetInteractionEnabled(false); }
    private void Update()
    {
        if (!Application.isFocused || Time.timeScale <= 0) return;
        Tick(Time.deltaTime);
    }
    public void Tick(float deltaSeconds)
    {
        if (!opening || deltaSeconds <= 0) return;
        angle = Mathf.MoveTowards(angle, openAngle, speed * deltaSeconds);
        transform.localRotation = closedRotation * Quaternion.Euler(0, angle, 0);
        if (Mathf.Approximately(angle, openAngle)) opening = false;
    }
    public void CaptureRoundBaseline() => closedRotation = transform.localRotation;
    public void ResetForRound()
    {
        opening = false; angle = 0; transform.localRotation = closedRotation; interaction.SetInteractionEnabled(true);
    }
    private void OnDestroy() { if (interaction != null) interaction.ClickCompleted -= Open; }
}
