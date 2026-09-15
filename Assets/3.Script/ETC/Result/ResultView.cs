using UnityEngine;
using TMPro;

public sealed class ResultView : MonoBehaviour
{
    [Header("결과화면 문구")]
    [SerializeField] private TMP_Text resultMessage;
    [SerializeField] private TMP_Text continueMessage;

    public bool IsConfigured => resultMessage != null && continueMessage != null;

    public void HideAll()
    {
        if (!ValidateReferences())
            return;

        resultMessage.gameObject.SetActive(false);
        continueMessage.gameObject.SetActive(false);
    }
    public void ShowResult()
    {
        if (!ValidateReferences())
            return;

        resultMessage.gameObject.SetActive(true);
        continueMessage.gameObject.SetActive(false);
    }

    private bool ValidateReferences()
    {
        if (IsConfigured)
            return true;

        Debug.LogError("[ResultView] Result Message와 Continue Message를 연결하세요", this);
        return false;
    }

    public void ShowContinuePrompt()
    {
        if (!ValidateReferences())
            return;

        resultMessage.gameObject.SetActive(true);
        continueMessage.gameObject.SetActive(true);
    }
}
