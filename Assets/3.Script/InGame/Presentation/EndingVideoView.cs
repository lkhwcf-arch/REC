using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;

// 실제 클리어 권한은 GameSession에 남겨 두고 영상 완료 결과만 전달합니다.
public sealed class EndingVideoView : MonoBehaviour
{
    public event Action Completed;
    private GameObject canvasRoot;
    private RawImage image;
    private Text placeholder;
    private VideoPlayer video;
    private RenderTexture texture;
    private Font font;
    private bool running, prepared, temporary, paused;
    private float elapsed, temporarySeconds, prepareTimeout;
    public void Configure()
    {
        canvasRoot = new GameObject("EndingCanvas", typeof(RectTransform), typeof(Canvas), typeof(GraphicRaycaster));
        canvasRoot.transform.SetParent(transform, false);
        var canvas = canvasRoot.GetComponent<Canvas>(); canvas.renderMode = RenderMode.ScreenSpaceOverlay; canvas.sortingOrder = 500;
        var panel = new GameObject("Video", typeof(RectTransform), typeof(RawImage)); panel.transform.SetParent(canvasRoot.transform, false);
        image = panel.GetComponent<RawImage>(); image.color = Color.black; image.raycastTarget = true;
        Stretch(image.rectTransform);
        var label = new GameObject("TemporaryEnding", typeof(RectTransform), typeof(Text)); label.transform.SetParent(canvasRoot.transform, false);
        placeholder = label.GetComponent<Text>(); Stretch(placeholder.rectTransform);
        font = Font.CreateDynamicFontFromOSFont(new[] { "Malgun Gothic", "Arial" }, 28);
        placeholder.font = font; placeholder.fontSize = 28; placeholder.alignment = TextAnchor.MiddleCenter; placeholder.raycastTarget = false;
        placeholder.text = "임시 엔딩\n영상 준비 후 교체됩니다.";
        video = gameObject.AddComponent<VideoPlayer>(); video.playOnAwake = false; video.isLooping = false;
        video.renderMode = VideoRenderMode.RenderTexture; video.aspectRatio = VideoAspectRatio.FitInside;
        var audio = gameObject.AddComponent<AudioSource>(); audio.playOnAwake = false; audio.spatialBlend = 0;
        video.audioOutputMode = VideoAudioOutputMode.AudioSource; video.controlledAudioTrackCount = 1; video.SetTargetAudioSource(0, audio);
        video.prepareCompleted += OnPrepared; video.loopPointReached += OnEnded; video.errorReceived += OnError;
        canvasRoot.SetActive(false);
    }
    private static void Stretch(RectTransform rect) { rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one; rect.offsetMin = rect.offsetMax = Vector2.zero; }
    public void Play(VideoClip clip, float fallbackDuration, float timeout)
    {
        Stop();
        running = true; elapsed = 0; temporarySeconds = Mathf.Max(0.1f, fallbackDuration); prepareTimeout = Mathf.Max(1, timeout);
        canvasRoot.SetActive(true); placeholder.enabled = clip == null; image.color = Color.black;
        temporary = clip == null;
        if (temporary) return;
        texture = new RenderTexture(1920, 1080, 0); texture.Create(); image.texture = texture;
        video.clip = clip; video.targetTexture = texture; video.Prepare();
    }
    private void OnPrepared(VideoPlayer source)
    {
        if (!running || temporary) return;
        prepared = true; image.color = Color.white;
        if (Application.isFocused && Time.timeScale > 0) source.Play();
    }
    private void OnEnded(VideoPlayer _) => Finish();
    private void OnError(VideoPlayer _, string message)
    {
        if (!running) return;
        Debug.LogWarning("[엔딩] 영상 재생 실패, 임시 화면으로 전환: " + message, this);
        video.Stop(); temporary = true; prepared = false; elapsed = 0;
        placeholder.enabled = true; image.color = Color.black;
    }
    private void Update()
    {
        if (!running) return;
        bool pause = Time.timeScale <= 0 || !Application.isFocused;
        if (paused != pause)
        {
            paused = pause;
            if (prepared && !temporary) { if (pause) video.Pause(); else video.Play(); }
        }
        if (pause) return;
        elapsed += Time.unscaledDeltaTime;
        if (temporary && elapsed >= temporarySeconds) Finish();
        else if (!temporary && !prepared && elapsed >= prepareTimeout) OnError(video, "준비 시간 초과");
    }
    private void Finish()
    {
        if (!running) return;
        Stop(); Completed?.Invoke();
    }
    public void Stop()
    {
        running = prepared = temporary = paused = false;
        if (video != null) { video.Stop(); video.targetTexture = null; }
        if (image != null) image.texture = null;
        if (texture != null) { texture.Release(); Destroy(texture); texture = null; }
        if (canvasRoot != null) canvasRoot.SetActive(false);
    }
    private void OnDisable() => Stop();
    private void OnDestroy()
    {
        Stop();
        if (video != null) { video.prepareCompleted -= OnPrepared; video.loopPointReached -= OnEnded; video.errorReceived -= OnError; }
        if (font != null) Destroy(font);
    }
}
