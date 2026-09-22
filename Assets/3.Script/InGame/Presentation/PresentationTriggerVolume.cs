using System;
using UnityEngine;

[RequireComponent(typeof(BoxCollider))]
public sealed class PresentationTriggerVolume : MonoBehaviour
{
    public event Action Entered;
    private PlayerController player;
    private bool armed;
    public void Configure(PlayerController target, Vector3 size)
    {
        player = target;
        var volume = GetComponent<BoxCollider>(); volume.isTrigger = true; volume.size = size;
        var body = gameObject.AddComponent<Rigidbody>(); body.isKinematic = true; body.useGravity = false;
        gameObject.layer = LayerMask.NameToLayer("Ignore Raycast");
    }
    public void Arm(bool value) => armed = value;
    private void OnTriggerEnter(Collider other) => TryEnter(other);
    private void OnTriggerStay(Collider other) => TryEnter(other);
    private void TryEnter(Collider other)
    {
        if (!armed || player == null || !player.CanInteractNow || Time.timeScale <= 0 || !Application.isFocused) return;
        if (other.transform != player.transform && !other.transform.IsChildOf(player.transform)) return;
        armed = false;
        Entered?.Invoke();
    }
    public static PresentationTriggerVolume Create(Transform owner, string name, Transform anchor, Vector3 position, Vector3 size, PlayerController player)
    {
        var node = new GameObject(name); node.transform.SetParent(owner, false);
        node.transform.SetPositionAndRotation(anchor != null ? anchor.position : position, anchor != null ? anchor.rotation : Quaternion.identity);
        var trigger = node.AddComponent<PresentationTriggerVolume>(); trigger.Configure(player, size); return trigger;
    }
}
