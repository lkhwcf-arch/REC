using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PannelButton : MonoBehaviour
{
    [SerializeField] private ButtonManager buttonManager;
    [SerializeField] GameObject targetPanel;

    public void Open()
    {
        if (buttonManager == null)
        {
            Debug.LogError("[PannelButton] ButtonManager를 연결 하세요", this);
            return;
        }
        buttonManager.OpenPanel(targetPanel);
    }
    public void Close()
    {
        if (buttonManager == null)
        {
            Debug.LogError("[PannelButton] ButtonManager를 연결 하세요", this);
            return;
        }
        buttonManager.ClosePanel(targetPanel);
    }
}
