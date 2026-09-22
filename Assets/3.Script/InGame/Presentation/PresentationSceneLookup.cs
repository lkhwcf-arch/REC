using System;
using UnityEngine;

internal static class PresentationSceneLookup
{
    public static Transform Named(Transform root, string name)
    {
        Transform found = null;
        foreach (var node in root.GetComponentsInChildren<Transform>(true))
        {
            if (node.name != name) continue;
            if (found != null) throw new InvalidOperationException("연출 기준 이름이 중복되었습니다: " + name);
            found = node;
        }
        if (found == null) throw new InvalidOperationException("연출 기준 사물이 없습니다: " + name);
        return found;
    }
    public static Vector3 Floor(Vector3 near)
    {
        int mask = ~LayerMask.GetMask("Player", "UI", "InteractionArea", "Ignore Raycast");
        return Physics.Raycast(near + Vector3.up * 0.5f, Vector3.down, out var hit, 4f, mask, QueryTriggerInteraction.Ignore)
            ? hit.point + Vector3.up * 0.03f : near;
    }
    public static AudioSource Audio(Transform owner, string name, AudioClip clip, bool loop, float spatial = 0)
    {
        var node = new GameObject(name); node.transform.SetParent(owner, false);
        var source = node.AddComponent<AudioSource>(); source.playOnAwake = false; source.loop = loop;
        source.spatialBlend = spatial; source.dopplerLevel = 0; source.clip = clip; return source;
    }
}
