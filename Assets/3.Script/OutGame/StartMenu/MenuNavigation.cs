using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class MenuNavigation : IMenuNavigation
{
    private readonly string gameSceneName;

    public MenuNavigation(string gameSceneName)
    {
        if (string.IsNullOrEmpty(gameSceneName))
        {
            throw new System.ArgumentException("게임 씬 이름이 유효하지 않습니다.", nameof(gameSceneName));
        }
        this.gameSceneName = gameSceneName;
    }

    public void StartGame()
    {
        Debug.Log($"게임 씬으로 이동: {gameSceneName}");
        SceneManager.LoadScene(gameSceneName);
    }

    public void ExitGame()
    {
        Debug.Log("게임 종료");

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}
