using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Resonance.LobbySystem.DataProviders
{
    // may get replaced with a full-on settings script later

    /// <summary>
    /// Attach to any GameObject to experiment with URP render scale.
    /// Drag the sliders in the Inspector while in Play mode to see the effect live.
    /// </summary>
    public class RenderScaleSetter : MonoBehaviour
    {
        public static RenderScaleSetter Instance { get; private set; }

        [Range(0.25f, 1.5f)]
        [SerializeField] private float renderScale = 1f;
        [SerializeField] private UpscalingFilterSelection upscalingFilter = UpscalingFilterSelection.FSR;
        [Range(0f, 1f)]
        [SerializeField] private float fsrSharpness = 0.92f;

        private UniversalRenderPipelineAsset _urpAsset;
        private float _originalRenderScale;
        private UpscalingFilterSelection _originalUpscalingFilter;
        private float _originalFsrSharpness;

        public float RenderScale => renderScale;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(this);

            _urpAsset = GraphicsSettings.currentRenderPipeline as UniversalRenderPipelineAsset;

            if (_urpAsset == null)
            {
                Debug.LogWarning("[RenderScaleSetter] No URP asset found.");
                enabled = false;
                return;
            }

            _originalRenderScale = _urpAsset.renderScale;
            _originalUpscalingFilter = _urpAsset.upscalingFilter;
            _originalFsrSharpness = _urpAsset.fsrSharpness;
        }

        public void ChangeRenderScale(float newRenderScale)
        {
            if (newRenderScale >= 0.25f && newRenderScale <= 1.5f)
            {
                renderScale = newRenderScale;
                Apply();
            }
        }

        private void OnValidate()
        {
            // only called for in-editor changes
            if (!Application.isPlaying)
            {
                return;
            }

            Apply();
        }

        private void OnDisable()
        {
            if (_urpAsset == null)
            {
                return;
            }

            _urpAsset.renderScale = _originalRenderScale;
            _urpAsset.upscalingFilter = _originalUpscalingFilter;
            _urpAsset.fsrSharpness = _originalFsrSharpness;
        }

        private void Apply()
        {
            if (_urpAsset == null)
            {
                return;
            }

            _urpAsset.renderScale = renderScale;
            _urpAsset.upscalingFilter = renderScale < 1f ? upscalingFilter : UpscalingFilterSelection.Auto;
            _urpAsset.fsrSharpness = fsrSharpness;
        }
    }
}
