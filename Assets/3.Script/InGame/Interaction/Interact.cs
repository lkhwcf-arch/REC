using UnityEngine;
using UnityEngine.Events;

public enum InteractionInputType
{
    Click,
    Hold
}

public class Interact : MonoBehaviour
{
    public event System.Action HoldCompleted;
    [SerializeField] private bool interactionEnabled = true;
    [Header("입력 방식")]
    [SerializeField] private InteractionInputType inputType = InteractionInputType.Hold;
    [Header("짧게 클릭했을 때")]
    [SerializeField] private UnityEvent onClick = new UnityEvent();

    [Header("1초 이상 유지했을 때")]
    [SerializeField] private UnityEvent onHold = new UnityEvent();
    public InteractionInputType InputType => inputType;
    public bool CanInteract => interactionEnabled && isActiveAndEnabled;

    public void Click()
    {
        if (!CanInteract || inputType != InteractionInputType.Click)
            return;

        Debug.Log($"단발 클릭: {name}", this);
        onClick.Invoke();
    }

    public void Hold()
    {
        if (!CanInteract || inputType != InteractionInputType.Hold)
            return;

        Debug.Log($"유지 클릭: {name}", this);
        onHold.Invoke();
        HoldCompleted?.Invoke();
    }

    public void SetInteractionEnabled(bool enabled)
    {
        interactionEnabled = enabled;
    }
}
