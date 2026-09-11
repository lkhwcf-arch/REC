using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class StartMenuView : MonoBehaviour
{
    [SerializeField] private GameObject explanationPanel;

    public bool IsConfigured => explanationPanel != null;

    public bool IsExplanationPanelOpen => explanationPanel != null && explanationPanel.activeSelf;

    public bool IsExplanationPanel(GameObject panel)
    {
        return explanationPanel != null && panel == explanationPanel;
    }

    public void RenderExplanation(bool isOpen)
    {
        if (explanationPanel == null)
        {
            Debug.LogWarning("[StartMenuView] 설명 패널이 할당되지 않았습니다.");
            return;
        }

        explanationPanel.SetActive(isOpen);
    }
}
