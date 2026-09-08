using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PannelButton : MonoBehaviour
{
    [SerializeField] private GameObject targetPanel;

    public void Open()
    {
        if (ButtonManager.Instance == null)
        {
            Debug.LogError("씬에 ButtonManager가 없습니다.", this);
            return;
        }

        ButtonManager.Instance.OpenPanel(targetPanel);
    }

    public void Close()
    {
        if (ButtonManager.Instance == null)
        {
            Debug.LogError("씬에 ButtonManager가 없습니다.", this);
            return;
        }

        ButtonManager.Instance.ClosePanel(targetPanel);
    }
}
