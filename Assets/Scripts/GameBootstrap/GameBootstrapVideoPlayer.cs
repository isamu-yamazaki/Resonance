using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;

namespace Resonance.GameBootstrap
{
    /// <summary>
    /// Plays a video into a RawImage on the bootstrap canvas.
    /// Configure aspect behavior on the RawImage's AspectRatioFitter directly.
    /// </summary>
    [RequireComponent(typeof(RawImage))]
    public class GameBootstrapVideoPlayer : MonoBehaviour
    {
        [SerializeField] private VideoClip clip;
        [SerializeField] private VideoPlayer videoPlayer;

        [Tooltip("Shown immediately on scene load while the video prepares. Should be the first frame of the clip.")]
        [SerializeField] private Texture posterFrame;

        [Tooltip("If true, the video starts playing as soon as it's prepared. If false, call Play() to start it.")]
        [SerializeField] private bool playAutomatically = true;

        [Tooltip("If true, the video restarts when it reaches the end.")]
        [SerializeField] private bool loop = true;

        private RawImage rawImage;
        private RenderTexture renderTexture;
        private bool isPrepared;
        private bool playRequested;

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
            videoPlayer.isLooping = loop;
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

        /// <summary>
        /// Starts the video. Safe to call before Prepare() finishes —
        /// playback will start as soon as the first frame is ready.
        /// </summary>
        public void Play()
        {
            if (videoPlayer == null)
            {
                return;
            }

            if (isPrepared)
            {
                StartPlayback();
            }
            else
            {
                playRequested = true;
            }
        }

        private void OnPrepared(VideoPlayer source)
        {
            isPrepared = true;

            if (playAutomatically || playRequested)
            {
                StartPlayback();
            }
        }

        private void StartPlayback()
        {
            rawImage.texture = renderTexture;
            rawImage.enabled = true;
            videoPlayer.Play();
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
