using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Resonance
{
    public class CinematicCameraPostProcessing : MonoBehaviour
    {
        [Header("Profile")]
        [SerializeField] private VolumeProfile _profile;

        [Header("Lens Distortion")]
        public float lensDistortionIntensity = -0.25f;

        [Header("Chromatic Aberration")]
        public float chromaticAberrationIntensity = 0.65f;

        [Header("Vignette")]
        public Color vignetteColor = Color.black;
        public float vignetteIntensity = 0.5f;
        public float vignetteSmoothness = 0.4f;

        [Header("Film Grain")]
        public float filmGrainIntensity = 0.35f;

        [Header("Color Adjustments")]
        public float postExposure = -0.3f;
        public float saturation = -15f;
        public Color colorFilter = new Color(0.85f, 0.92f, 1f);

        private Volume _volume;

        private LensDistortion _lensDistortion;
        private ChromaticAberration _chromaticAberration;
        private Vignette _vignette;
        private FilmGrain _filmGrain;
        private ColorAdjustments _colorAdjustments;

        #region Startup

        private void Awake()
        {
            CreateVolume();
            ResolveOverrides();
            EnableOverrideStates();
            ApplyValues();
        }

        private void OnDestroy()
        {
            if (_volume != null)
                Destroy(_volume.gameObject);
        }

        private void CreateVolume()
        {
            GameObject volumeObject = new GameObject("CinematicPostProcessingVolume");
            DontDestroyOnLoad(volumeObject);

            _volume = volumeObject.AddComponent<Volume>();
            _volume.isGlobal = true;
            _volume.priority = 2;
            _volume.profile = _profile;
        }

        private void ResolveOverrides()
        {
            if (_profile == null)
            {
                Debug.LogWarning("[CinematicCameraPostProcessing] No Volume Profile assigned.");
                return;
            }

            _profile.TryGet(out _lensDistortion);
            _profile.TryGet(out _chromaticAberration);
            _profile.TryGet(out _vignette);
            _profile.TryGet(out _filmGrain);
            _profile.TryGet(out _colorAdjustments);
        }

        private void EnableOverrideStates()
        {
            if (_lensDistortion != null)
                _lensDistortion.intensity.overrideState = true;

            if (_chromaticAberration != null)
                _chromaticAberration.intensity.overrideState = true;

            if (_vignette != null)
            {
                _vignette.color.overrideState = true;
                _vignette.intensity.overrideState = true;
                _vignette.smoothness.overrideState = true;
            }

            if (_filmGrain != null)
            {
                _filmGrain.intensity.overrideState = true;
                _filmGrain.type.overrideState = true;
            }

            if (_colorAdjustments != null)
            {
                _colorAdjustments.postExposure.overrideState = true;
                _colorAdjustments.saturation.overrideState = true;
                _colorAdjustments.colorFilter.overrideState = true;
            }
        }

        private void ApplyValues()
        {
            if (_lensDistortion != null)
                _lensDistortion.intensity.value = lensDistortionIntensity;

            if (_chromaticAberration != null)
                _chromaticAberration.intensity.value = chromaticAberrationIntensity;

            if (_vignette != null)
            {
                _vignette.color.value = vignetteColor;
                _vignette.intensity.value = vignetteIntensity;
                _vignette.smoothness.value = vignetteSmoothness;
            }

            if (_filmGrain != null)
            {
                _filmGrain.type.value = FilmGrainLookup.Thin1;
                _filmGrain.intensity.value = filmGrainIntensity;
            }

            if (_colorAdjustments != null)
            {
                _colorAdjustments.postExposure.value = postExposure;
                _colorAdjustments.saturation.value = saturation;
                _colorAdjustments.colorFilter.value = colorFilter;
            }
        }

        #endregion

        #region Public Methods

        public void OnCinematicEnd()
        {
            if (_volume != null)
                _volume.gameObject.SetActive(false);
        }

        #endregion
    }
}
