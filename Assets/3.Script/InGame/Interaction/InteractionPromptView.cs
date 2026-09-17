using TMPro;
using UnityEngine;

public sealed class InteractionPromptView : MonoBehaviour
{
    [Header("입력상태 제공")]
    [SerializeField] private PlayerInteractor playerInteractor;

    [Header("화면 연결")]
    [SerializeField] private GameObject content;
    [SerializeField] private TMP_Text actionText;
    [SerializeField] private TMP_Text instructionText;
    [Header("안내 문구")]
    [SerializeField] private string actionLabel = "Resolve";
    [SerializeField] private string holdInstructionFormat = "Press mouse Left Button in {0}Second";

    private bool subscribed;

    private void Awake()
    {
        if (playerInteractor == null || content == null
        || actionText == null || instructionText == null)
        {
            Debug.LogError("[InteractionPromptView] 플레이어와 UI 참조를 연결 할 것", this);
            enabled = false;
            return;
        }
        // 안내 UI가 조준 입력을 가로채지 않도록
        actionText.raycastTarget = false;
        instructionText.raycastTarget = false;

        Hide();
    }

    private void OnEnable()
    {
        if (playerInteractor == null || content == null || actionText == null || instructionText == null)
        {
            return;
        }

        playerInteractor.TargetChanged += OnTargetChanged;
        subscribed = true;
        // UI가 다시 켜졌을 때 현재 상태를 즉시 반영합니다.
        OnTargetChanged(playerInteractor.CurrentTarget);
    }

    private void OnDisable()
    {
        if (subscribed && playerInteractor != null)
        {
            playerInteractor.TargetChanged -= OnTargetChanged;
        }

        subscribed = false;
        Hide();
    }

    private void OnTargetChanged(Interact target)
    {
        if (target == null || !target.CanInteract || target.InputType != InteractionInputType.Hold)
        {
            Hide();
            return;
        }

        actionText.text = actionLabel;

        string seconds = playerInteractor.HoldDuration.ToString("0.##");

        // {0} 부분을 실제 유지 시간으로 바꿉니다.
        instructionText.text = holdInstructionFormat.Replace("{0}", seconds);

        content.SetActive(true);
    }
    private void Hide()
    {
        if (content != null)
            content.SetActive(false);
    }
}
