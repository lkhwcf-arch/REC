using UnityEngine;

[DisallowMultipleComponent]
public class MapTarget : MonoBehaviour, IAnomalyTarget
{
    [Header("데이터 연결")]
    [SerializeField, Min(1)] private int targetId = 1;

    [Header("실제 사물")]
    [SerializeField] private GameObject visual;

    public bool IsVisible => visual != null && visual.activeSelf;
    public int TargetId => targetId;
    public GameObject Visual => visual;

    public void Configure(int id, GameObject visualObject)
    {
        targetId = id;
        visual = visualObject;
    }

    public bool TryValidate(out string error)
    {
        if (targetId <= 0)
        {
            error = $"'{name}'의 Target ID는 양수여야 합니다.";
            return false;
        }

        if (visual == null)
        {
            error = $"'{name}'의 Visual을 연결하세요.";
            return false;
        }

        if (visual == gameObject || !visual.transform.IsChildOf(transform))
        {
            error = $"'{name}'의 Visual은 이 오브젝트 아래의 자식이어야 합니다.";
            return false;
        }

        error = string.Empty;
        return true;
    }

    public void SetVisible(bool visible)
    {
        if (visual == null)
            throw new System.InvalidOperationException($"TargetID={targetId}의 Visual이 연결되지 않았습니다.");

        visual.SetActive(visible);
    }
}
