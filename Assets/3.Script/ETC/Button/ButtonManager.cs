using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;


enum ButtonType
{
    StartButton,
    ExplaneButton,
    OutButton,
    ExitButton
}

public class ButtonManager : MonoBehaviour
{
    public static ButtonManager Instance { get; private set; }


    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

     public void OpenPanel(GameObject panel)
    {
        if (panel != null)
            panel.SetActive(true);
    }

    public void ClosePanel(GameObject panel)
    {
        if (panel != null)
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
        transform.Find("ExplanePannel").gameObject.SetActive(true);
    }

    private void StartGame()
    {
        // 게임 시작 로직 구현
        Debug.Log("게임 시작");
        SceneManager.LoadScene("InGame"); // 예시로 "InGame"으로 씬 전환
    }
    private void OutPannel()
    {
        transform.Find("ExplanePannel").gameObject.SetActive(false);
    }
    private void ExitGame()
    {
        // 게임 종료 로직 구현
        Debug.Log("게임 종료");
        
#if UNITY_EDITOR
    UnityEditor.EditorApplication.isPlaying = false;
#else
    Application.Quit();
#endif
    }
}
