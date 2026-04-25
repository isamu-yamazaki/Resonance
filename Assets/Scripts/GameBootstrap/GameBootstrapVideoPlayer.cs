using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;

namespace Resonance.GameBootstrap
{
    /// <summary>
    /// Plays a looping video into a RawImage on the bootstrap canvas.
    /// Configure aspect behavior on the RawImage's AspectRatioFitter directly.
    /// </summary>
    [RequireComponent(typeof(RawImage))]
    public class GameBootstrapVideoPlayer : MonoBehaviour
    {
        [SerializeField] private VideoClip clip;
        [SerializeField] private VideoPlayer videoPlayer;

        [Tooltip("Shown immediately on scene load while the video prepares. Should be the first frame of the clip.")]
        [SerializeField] private Texture posterFrame;

        private RawImage rawImage;
        private RenderTexture renderTexture;

        private void Awake()
        {
            rawImage = GetComponent<RawImage>();

            if (clip == null)
            {
                Debug.LogError($"[{GetType()}] No {nameof(VideoClip)} assigned.");
                return;
            }

            if (videoPlayer == null)
            {
                Debug.LogError($"[{GetType()}] No {nameof(VideoPlayer)} assigned.");
                return;
            }

            renderTexture = new RenderTexture((int)clip.width, (int)clip.height, 0);
            renderTexture.Create();

            videoPlayer.clip = clip;
            videoPlayer.renderMode = VideoRenderMode.RenderTexture;
            videoPlayer.targetTexture = renderTexture;
            videoPlayer.isLooping = true;
            videoPlayer.playOnAwake = false;
            videoPlayer.waitForFirstFrame = true;

            if (posterFrame != null)
            {
                rawImage.texture = posterFrame;
                rawImage.enabled = true;
            }
            else
            {
                rawImage.texture = renderTexture;
                rawImage.enabled = false;
            }

            videoPlayer.prepareCompleted += OnPrepared;
            videoPlayer.Prepare();
        }

        private void OnPrepared(VideoPlayer source)
        {
            rawImage.texture = renderTexture;
            rawImage.enabled = true;
            source.Play();
        }

        private void OnDestroy()
        {
            if (videoPlayer != null)
            {
                videoPlayer.prepareCompleted -= OnPrepared;
                videoPlayer.Stop();
            }

            if (renderTexture != null)
            {
                renderTexture.Release();
                Destroy(renderTexture);
            }
        }
    }
}
