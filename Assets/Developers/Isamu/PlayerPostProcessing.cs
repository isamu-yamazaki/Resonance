using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using Resonance.Player;

namespace Resonance.PlayerController
{
    [RequireComponent(typeof(Volume))]
    public class PlayerPostProcessing : MonoBehaviour
    {
        #region Inspector Fields

        [Header("Volume")]
        [SerializeField] private Volume _playerVolume;

        [Header("Overdrive — Bloom")]
        public float overdriveBloomIntensity = 1.4f;
        public float baseBloomIntensity = 0.75f;

        [Header("Overdrive — Chromatic Aberration")]
        public float overdriveChromaticAberrationIntensity = 0.7f;
        public float baseChromaticAberrationIntensity = 0.4f;

        [Header("Overdrive — Lens Distortion")]
        public float overdriveLensDistortionIntensity = -0.4f;
        public float baseLensDistortionIntensity = -0.1f;

        [Header("Overdrive — Screen Tint")]
        public Color overdriveTintColor = new Color(0f, 1f, 0.5f, 1f);
        public float overdriveTintIntensity = 0.3f;

        [Header("Overdrive — Transition")]
        public float overdriveTransitionSpeed = 6f;



        #endregion

        #region Private State

        private OverdriveAbility _overdriveAbility;
        private PlayerStats _playerStats;

        private Bloom _bloom;
        private ChromaticAberration _chromaticAberration;
        private LensDistortion _lensDistortion;
        private ColorAdjustments _colorAdjustments;

        private float _currentTintWeight = 0f;
        private bool _isDead = false;

        #endregion

        #region Startup

        private void Awake()
        {
            _overdriveAbility = GetComponent<OverdriveAbility>();
            _playerStats = GetComponent<PlayerStats>();

            if (_playerVolume == null)
                _playerVolume = GetComponent<Volume>();

            _playerVolume.weight = 1f;

            ResolveOverrides();
        }

        private void Start()
        {
            EnableOverrideStates();
            SetBaseValues();

            if (_playerStats != null)
            {
                _playerStats.OnPlayerDeath += HandlePlayerDeath;
                _playerStats.OnPlayerRespawn += HandlePlayerRespawn;
            }
        }

        private void OnDestroy()
        {
            if (_playerStats != null)
            {
                _playerStats.OnPlayerDeath -= HandlePlayerDeath;
                _playerStats.OnPlayerRespawn -= HandlePlayerRespawn;
            }
        }

        private void ResolveOverrides()
        {
            if (_playerVolume.profile == null)
            {
                Debug.LogWarning("[PlayerPostProcessing] No Volume Profile assigned.");
                return;
            }

            _playerVolume.profile.TryGet(out _bloom);
            _playerVolume.profile.TryGet(out _chromaticAberration);
            _playerVolume.profile.TryGet(out _lensDistortion);
            _playerVolume.profile.TryGet(out _colorAdjustments);
        }

        private void EnableOverrideStates()
        {
            if (_bloom != null)
                _bloom.intensity.overrideState = true;

            if (_chromaticAberration != null)
                _chromaticAberration.intensity.overrideState = true;

            if (_lensDistortion != null)
                _lensDistortion.intensity.overrideState = true;

            if (_colorAdjustments != null)
                _colorAdjustments.colorFilter.overrideState = true;
        }

        private void SetBaseValues()
        {
            if (_bloom != null)
                _bloom.intensity.value = baseBloomIntensity;

            if (_chromaticAberration != null)
                _chromaticAberration.intensity.value = baseChromaticAberrationIntensity;

            if (_lensDistortion != null)
                _lensDistortion.intensity.value = baseLensDistortionIntensity;

            if (_colorAdjustments != null)
                _colorAdjustments.colorFilter.value = Color.white;
        }

        #endregion

        #region Update

        private void Update()
        {
            if (_isDead) return;

            bool isOverdriveActive = _overdriveAbility != null && _overdriveAbility.IsInOverdrive;

            UpdateBloom(isOverdriveActive);
            UpdateChromaticAberration(isOverdriveActive);
            UpdateLensDistortion(isOverdriveActive);
            UpdateScreenTint(isOverdriveActive);
        }

        private void UpdateBloom(bool isOverdriveActive)
        {
            if (_bloom == null) return;

            float target = isOverdriveActive ? overdriveBloomIntensity : baseBloomIntensity;
            _bloom.intensity.value = Mathf.Lerp(_bloom.intensity.value, target, overdriveTransitionSpeed * Time.deltaTime);
        }

        private void UpdateChromaticAberration(bool isOverdriveActive)
        {
            if (_chromaticAberration == null) return;

            float target = isOverdriveActive ? overdriveChromaticAberrationIntensity : baseChromaticAberrationIntensity;
            _chromaticAberration.intensity.value = Mathf.Lerp(
                _chromaticAberration.intensity.value,
                target,
                overdriveTransitionSpeed * Time.deltaTime
            );
        }

        private void UpdateLensDistortion(bool isOverdriveActive)
        {
            if (_lensDistortion == null) return;

            float target = isOverdriveActive ? overdriveLensDistortionIntensity : baseLensDistortionIntensity;
            _lensDistortion.intensity.value = Mathf.Lerp(_lensDistortion.intensity.value, target, overdriveTransitionSpeed * Time.deltaTime);
        }

        private void UpdateScreenTint(bool isOverdriveActive)
        {
            if (_colorAdjustments == null) return;

            float targetWeight = isOverdriveActive ? overdriveTintIntensity : 0f;
            _currentTintWeight = Mathf.Lerp(_currentTintWeight, targetWeight, overdriveTransitionSpeed * Time.deltaTime);

            _colorAdjustments.colorFilter.value = Color.Lerp(Color.white, overdriveTintColor, _currentTintWeight);
        }

        #endregion

        #region Event Handlers

        private void HandlePlayerDeath()
        {
            _isDead = true;
            _currentTintWeight = 0f;
            SetBaseValues();
        }

        private void HandlePlayerRespawn()
        {
            _isDead = false;
            _currentTintWeight = 0f;
            SetBaseValues();
        }

        #endregion
    }
}