// CSV 경계만 대체합니다. 도메인 테스트는 실제 게임 소스를 그대로 컴파일합니다.
namespace UnityEngine
{
    public class TextAsset { public string text; public string name; }
    public static class Debug { public static void LogWarning(object message) { } }
}
