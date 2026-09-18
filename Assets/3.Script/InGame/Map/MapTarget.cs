using UnityEngine;

[DisallowMultipleComponent]
public class MapTarget : MonoBehaviour
{
    [Header("데이터 연결")]
    [SerializeField, Min(1)] private int targetId = 1;

    [Header("실제 사물")]
    [SerializeField] private GameObject visual;

    public int TargetId => targetId;
    public GameObject Visual => visual;

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
}