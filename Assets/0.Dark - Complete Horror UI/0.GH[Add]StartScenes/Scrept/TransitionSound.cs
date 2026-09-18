using UnityEngine;

public class PlayDissolveSound : MonoBehaviour
{
    public AudioSource dissolveAudioSource;

    // 디졸브 효과를 실행할 때 이 함수를 호출합니다.
    public void TriggerDissolveWithSound()
    {
        if (dissolveAudioSource != null)
        {
            dissolveAudioSource.Play();
        }

        // 기존 디졸브 연출 로직 실행 (예: Coroutine이나 Tween으로 Location 값 변경)
    }
}