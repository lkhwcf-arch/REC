using System;
using UnityEngine;

enum ButtonType
{
    StartButton,
    ExplaneButton,
    OutButton,
    ExitButton
}

public class ButtonManager : MonoBehaviour
{
    private StartMenuView menuView;
    private MenuController menuController;

    public void Initialize(StartMenuView view, MenuController controller)
    {
        if (controller == null)
        {
            throw new ArgumentNullException(nameof(controller), "MenuController가 null입니다.");
        }
        if (view == null || !view.IsConfigured)
        {
            throw new ArgumentException("StartMenuView가 초기화되지 않았거나 null입니다.", nameof(view));
        }
        menuController = controller;
        menuView = view;
    }

    public void OpenPanel(GameObject panel)
    {
        if (panel == null)
            return;

        if (menuView != null && menuView.IsExplanationPanel(panel))
        {
            ShowExplanation();
            return;
        }
        panel.SetActive(true);
    }

    public void ClosePanel(GameObject panel)
    {
        if (panel == null)
            return;
        if (menuView != null && menuView.IsExplanationPanel(panel))
        {
            OutPannel();
            return;
        }
        panel.SetActive(false);
    }

    public void OnClickButton(string buttonName)
    {
        if (System.Enum.TryParse(buttonName, out ButtonType buttonType))
        {
            switch (buttonType)
            {
                case ButtonType.StartButton:
                    StartGame();
                    break;
                case ButtonType.ExplaneButton:
                    ShowExplanation();
                    break;
                case ButtonType.ExitButton:
                    ExitGame();
                    break;
                case ButtonType.OutButton:
                    OutPannel();
                    break;
                default:
                    Debug.LogWarning($"[ButtonManager] 알 수 없는 버튼 타입: {buttonName}");
                    break;
            }
        }
        else
        {
            Debug.LogWarning($"[ButtonManager] 버튼 이름을 ButtonType으로 변환할 수 없습니다: {buttonName}");
        }
    }

    private void ShowExplanation()
    {
        if (menuController == null)
        {
            Debug.LogWarning("[ButtonManager] MenuController가 초기화되지 않았습니다.");
            return;
        }
        menuController.OpenExplanation();
    }

    private void StartGame()
    {
        // 게임 시작 로직 구현
        Debug.Log("게임 시작");
        if (menuController == null)
        {
            Debug.LogWarning("[ButtonManager] MenuController가 초기화되지 않았습니다.");
            return;
        }
        menuController.StartGame();
    }
    private void OutPannel()
    {
        if (menuController == null)
        {
            Debug.LogWarning("[ButtonManager] 설명 패널이 할당되지 않았습니다.");
            return;
        }
        menuController.CloseExplanation();
    }
    private void ExitGame()
    {
        if (menuController == null)
        {
            Debug.LogWarning("[ButtonManager] MenuController가 초기화되지 않았습니다.");
            return;
        }
        menuController.ExitGame();
    }
}
