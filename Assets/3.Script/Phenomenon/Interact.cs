using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
public class Interact : MonoBehaviour
{
    [SerializeField] private bool interactionEnabled = true;

    [Header("짧게 클릭했을 때")]
    [SerializeField] private UnityEvent onClick = new UnityEvent();

    [Header("1초 이상 유지했을 때")]
    [SerializeField] private UnityEvent onHold = new UnityEvent();

    public bool CanInteract =>
        interactionEnabled && isActiveAndEnabled;

    public void Click()
    {
        if (!CanInteract)
            return;

        Debug.Log($"단발 클릭: {name}", this);
        onClick.Invoke();
    }

    public void Hold()
    {
        if (!CanInteract)
            return;

        Debug.Log($"유지 클릭: {name}", this);
        onHold.Invoke();
    }

    public void SetInteractionEnabled(bool enabled)
    {
        interactionEnabled = enabled;
    }
}
