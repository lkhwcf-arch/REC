using System;
using UnityEngine;

// 데이터로 허용된 일반 문의 열림·닫힘을 제어합니다.
[RequireComponent(typeof(Interact))]
public class DoorInteraction : MonoBehaviour, IRoundResetParticipant
{
    // true: 열기 시작, false: 닫기 시작
    public event Action<bool> MotionStarted;

    // 회차 초기화 또는 동작 취소
    public event Action MotionCancelled;

    public int DoorId { get; private set; }

    private Interact interaction;
    private Quaternion closedRotation;

    private float openAngle;
    private float speed;
    private float angle;
    private float targetAngle;

    private bool moving;
    private bool targetOpen;

    public void Configure(DoorData row, float signedAngle)
    {
        if (interaction != null)
            interaction.ClickCompleted -= Toggle;

        DoorId = row.ID;
        openAngle = signedAngle;
        speed = row.AngularSpeed;
        closedRotation = transform.localRotation;

        interaction = GetComponent<Interact>();
        interaction.Configure(InteractionInputType.Click);
        interaction.ClickCompleted += Toggle;
    }

    private void Toggle()
    {
        if (moving)
            return;

        BeginMotion(!targetOpen);
    }

    // 기존 코드나 Inspector에서 호출할 수 있도록 유지합니다.
    public void Open()
    {
        BeginMotion(true);
    }

    public void Close()
    {
        BeginMotion(false);
    }

    private void BeginMotion(bool open)
    {
        if (!isActiveAndEnabled ||
            interaction == null ||
            moving ||
            targetOpen == open)
        {
            return;
        }

        targetOpen = open;
        targetAngle = open ? openAngle : 0f;
        moving = true;

        // 이동 중에는 연속 클릭을 받지 않습니다.
        interaction.SetInteractionEnabled(false);

        // 실제 동작을 시작한 뒤 결과를 알립니다.
        MotionStarted?.Invoke(open);
    }

    private void Update()
    {
        if (!Application.isFocused || Time.timeScale <= 0f)
            return;

        Tick(Time.deltaTime);
    }

    public void Tick(float deltaSeconds)
    {
        if (!moving || deltaSeconds <= 0f)
            return;

        angle = Mathf.MoveTowards(
            angle,
            targetAngle,
            speed * deltaSeconds);

        transform.localRotation =
            closedRotation * Quaternion.Euler(0f, angle, 0f);

        if (!Mathf.Approximately(angle, targetAngle))
            return;

        moving = false;

        // 열거나 닫는 동작이 끝나면 다시 클릭할 수 있습니다.
        interaction.SetInteractionEnabled(true);
    }

    public void CaptureRoundBaseline()
    {
        closedRotation = transform.localRotation;
    }

    public void ResetForRound()
    {
        moving = false;
        targetOpen = false;
        angle = 0f;
        targetAngle = 0f;

        transform.localRotation = closedRotation;

        if (interaction != null)
            interaction.SetInteractionEnabled(true);

        // 초기화로 문이 닫힐 때는 닫힘 소리를 재생하지 않습니다.
        MotionCancelled?.Invoke();
    }

    private void OnDisable()
    {
        ResetForRound();
    }

    private void OnDestroy()
    {
        if (interaction != null)
            interaction.ClickCompleted -= Toggle;
    }
}