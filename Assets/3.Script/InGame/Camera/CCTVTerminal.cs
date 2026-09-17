using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CCTVTerminal : MonoBehaviour
{
    [SerializeField] private CameraManager cameraManager;

    

    //플레이어 상화적용 성공시 호출 
    public void Interact()
    {
        cameraManager.ShowCCTV();
    }

    // 종료 버튼 또는 취소 입력에서 호출
    public void Exit()
    {
        cameraManager.ShowPlayer();
    }
}
