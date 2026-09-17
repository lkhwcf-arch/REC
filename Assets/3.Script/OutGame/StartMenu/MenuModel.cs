using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MenuModel
{
    // 현재 설명창이 열려있는지 확인
    public bool IsExplanationPanelOpen { get; private set; }
    // 설명창 상태가 바뀌었을때 알림
    public event Action<bool> ExplanationChanged;
    // 기존 패널의 상태를 받아서 시작
    public MenuModel(bool initiallyOpen)
    {
        IsExplanationPanelOpen = initiallyOpen;
    }
    // 설명창 상태를 변경하는 입구
    public void SetExplanationOpen(bool isOpen)
    {
        if (IsExplanationPanelOpen == isOpen)
            return;

        IsExplanationPanelOpen = isOpen;
        ExplanationChanged?.Invoke(isOpen);
    }
}
