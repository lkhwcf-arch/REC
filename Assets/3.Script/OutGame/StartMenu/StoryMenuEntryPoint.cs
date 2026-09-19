using System;
using NUnit.Framework;
using UnityEngine;

public class StoryMenuEntryPoint : MonoBehaviour
{
    [SerializeField] private string gameSceneName = "InGame";
    private MenuController controller;
    private bool isStarting;

    void Awake()
    {
        if(string.IsNullOrWhiteSpace(gameSceneName))
        {
             Debug.LogError("[스토리 시작] 게임 씬 이름을 입력하세요.", this);
            enabled = false;
            return;
        }
        controller = new MenuController(new MenuModel(false), new MenuNavigation(gameSceneName));
    }

    public void StartStory()
    {
        if(!isActiveAndEnabled || controller == null || isStarting)
            return;
        
        isStarting = true;

        try
        {
            controller.StartGame();
        } 
        catch (Exception exception)
        {
            isStarting = false;
            Debug.LogException(exception, this);
        }

    }
}
