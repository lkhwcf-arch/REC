using System;
using UnityEngine;
using UnityEngine.SceneManagement;

public sealed class ResultNavigation : IResultNavigation
{
   private readonly string titleSceneName;
   public ResultNavigation(string titleSceneName)
    {
        if (string.IsNullOrWhiteSpace(titleSceneName))
        {
            throw new ArgumentException("타이틀 씬 이름이 비었다.", nameof(titleSceneName));
        }
        this.titleSceneName = titleSceneName;
    }

    public void ReturnToTitle()
    {
        // 인게임에서 일시정지해도  타이틀은 정상적인 시간과 커서상태로 시작
        Time.timeScale =1f;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        SceneManager.LoadScene(titleSceneName);
    }
}
