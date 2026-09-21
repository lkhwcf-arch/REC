using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(AudioSource))]
public class ScreenNoiseEffect : PresentationEffect
{
    [SerializeField] private RawImage overlay;
    [SerializeField] private AudioClip noiseClip;
    [SerializeField, Range(0f, 1f)] private float opacity = 0.35f;
    [SerializeField, Range(0f, 1f)] private float volume = 0.7f;
    [SerializeField, Min(1f)] private float visualRate = 20f;

    private AudioSource source;
    private Texture2D noiseTexture;
    private Texture previousTexture;
    private Color previousColor;
    private Rect previousUvRect;
    private int lastFrame = -1;

    private void Awake()
    {
        source = GetComponent<AudioSource>();
        source.playOnAwake = false;
        source.loop = true;
        source.spatialBlend = 0f;
        source.dopplerLevel = 0f;
        source.Stop();

        if (overlay == null)
            return;

        previousTexture = overlay.texture;
        previousColor = overlay.color;
        previousUvRect = overlay.uvRect;

        noiseTexture = new Texture2D(64, 64, TextureFormat.RGBA32, false);
        noiseTexture.name = "RuntimeScreenNoise";
        noiseTexture.wrapMode = TextureWrapMode.Repeat;
        noiseTexture.filterMode = FilterMode.Point;

        var random = new System.Random(7391);
        var pixels = new Color32[64 * 64];

        for (int i = 0; i < pixels.Length; i++)
        {
            byte value = (byte)random.Next(256);
            pixels[i] = new Color32(value, value, value, 255);
        }

        noiseTexture.SetPixels32(pixels);
        noiseTexture.Apply(false, true);

        overlay.texture = noiseTexture;
        overlay.raycastTarget = false;
        overlay.enabled = false;
    }

    protected override void OnStarted()
    {
        lastFrame = -1;

        if (overlay != null)
        {
            overlay.color = new Color(1f, 1f, 1f, opacity);
            overlay.enabled = true;
            OnTick(0f);
        }

        if (noiseClip != null)
        {
            source.clip = noiseClip;
            source.volume = volume;
            source.Play();
        }
    }

    protected override void OnTick(float elapsed)
    {
        if (overlay == null)
            return;

        int frame = Mathf.FloorToInt(elapsed * Mathf.Max(1f, visualRate));

        if (frame == lastFrame)
            return;

        lastFrame = frame;

        float x = Mathf.Repeat(frame * 0.371f, 1f);
        float y = Mathf.Repeat(frame * 0.619f, 1f);
        overlay.uvRect = new Rect(x, y, 8f, 4.5f);
    }

    protected override void OnPaused(bool paused)
    {
        if (paused)
            source.Pause();
        else
            source.UnPause();
    }

    protected override void OnStopped()
    {
        if (source != null)
        {
            source.Stop();
            source.clip = null;
        }

        if (overlay != null)
            overlay.enabled = false;
    }

    private void OnDestroy()
    {
        StopImmediately();

        if (overlay != null)
        {
            overlay.texture = previousTexture;
            overlay.color = previousColor;
            overlay.uvRect = previousUvRect;
        }

        if (noiseTexture != null)
            Destroy(noiseTexture);
    }
}