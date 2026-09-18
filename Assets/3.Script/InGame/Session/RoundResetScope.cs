using System;
using System.Collections.Generic;
using UnityEngine;

// 문 잠금, 인벤토리 등 Transform 이외의 상태를 가진 컴포넌트가 구현합니다.
public interface IRoundResetParticipant
{
    void CaptureRoundBaseline();
    void ResetForRound();
}

public class RoundResetScope : MonoBehaviour
{
    [Tooltip("회차마다 초기화할 맵 루트. 관리 서비스는 이 루트 밖에 두세요.")]
    [SerializeField] private Transform mapRoot;
    private TransformState[] transforms;
    private Rigidbody[] bodies;
    private IRoundResetParticipant[] participants;
    private struct TransformState
    {
        public Transform node;
        public Vector3 position, scale;
        public Quaternion rotation;
        public bool active;
    }
    public void Configure(Transform root) => mapRoot = root;
    public void Capture()
    {
        if (mapRoot == null) throw new InvalidOperationException("회차 초기화 Map Root를 연결하세요.");
        Transform[] nodes = mapRoot.GetComponentsInChildren<Transform>(true);
        transforms = new TransformState[nodes.Length];
        for (int i = 0; i < nodes.Length; i++)
            transforms[i] = new TransformState { node = nodes[i], position = nodes[i].localPosition, rotation = nodes[i].localRotation, scale = nodes[i].localScale, active = nodes[i].gameObject.activeSelf };
        bodies = mapRoot.GetComponentsInChildren<Rigidbody>(true);
        List<IRoundResetParticipant> resets = new();
        foreach (var component in mapRoot.GetComponentsInChildren<MonoBehaviour>(true))
            if (component is IRoundResetParticipant participant) { participant.CaptureRoundBaseline(); resets.Add(participant); }
        participants = resets.ToArray();
    }
    public void ResetMap()
    {
        if (transforms == null) return;
        foreach (var state in transforms)
        {
            if (state.node == null) throw new InvalidOperationException("초기 맵 사물이 Destroy되었습니다. 회차 재사용 사물은 비활성화하거나 풀로 반환하세요.");
            state.node.localPosition = state.position; state.node.localRotation = state.rotation; state.node.localScale = state.scale;
            state.node.gameObject.SetActive(state.active);
        }
        foreach (var body in bodies)
        {
            if (body == null) continue;
            body.position = body.transform.position; body.rotation = body.transform.rotation;
            if (!body.isKinematic) { body.linearVelocity = Vector3.zero; body.angularVelocity = Vector3.zero; body.Sleep(); }
        }
        foreach (var participant in participants) participant.ResetForRound();
        Physics.SyncTransforms();
    }
}
