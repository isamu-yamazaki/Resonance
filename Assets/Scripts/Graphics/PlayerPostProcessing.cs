using System.Collections;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using Resonance.Abilities.SonarDisc;
using Resonance.Player;
using PurrNet;

namespace Resonance.PlayerController
{
    [RequireComponent(typeof(Volume))]
    public class PlayerPostProcessing : NetworkBehaviour
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

        [Header("Scanned Flash")]
        public Color scannedFlashColor = new Color(1f, 0f, 0.8f, 1f);
        public float scannedFlashIntensity = 0.6f;
        public float scannedFlashDuration = 0.2f;

        #endregion

        #region Private State

        private OverdriveAbility _overdriveAbility;
        private PlayerStats _playerStats;
        private ScannedHighlight _scannedHighlight;

        private Bloom _bloom;
        private ChromaticAberration _chromaticAberration;
        private LensDistortion _lensDistortion;
        private ColorAdjustments _colorAdjustments;
        private Vignette _vignette;

        private float _currentTintWeight = 0f;
        private bool _isDead = false;
        private bool _wasScanned = false;

        #endregion

        #region Startup

        protected override void OnSpawned()
        {
            base.OnSpawned();
            enabled = isOwner;
        }

        private void Awake()
        {
            _overdriveAbility = GetComponent<OverdriveAbility>();
            _playerStats = GetComponent<PlayerStats>();
            _scannedHighlight = GetComponent<ScannedHighlight>();

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
            _playerVolume.profile.TryGet(out _vignette);
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

            if (_vignette != null)
            {
                _vignette.intensity.overrideState = true;
                _vignette.color.overrideState = true;
            }
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

            if (_vignette != null)
                _vignette.intensity.value = 0f;
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
            UpdateScannedFlash();
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

        private void UpdateScannedFlash()
        {
            if (_scannedHighlight == null || _vignette == null)
            {
                Debug.Log($"[PPP] UpdateScannedFlash skipped — highlight: {_scannedHighlight != null}, vignette: {_vignette != null}");
                return;
            }

            bool isScannedNow = _scannedHighlight.isScanned.value;
            if (isScannedNow && !_wasScanned)
            {
                Debug.Log("[PPP] Triggering ScannedFlashSequence");
                StartCoroutine(ScannedFlashSequence());
            }

            _wasScanned = isScannedNow;
        }

        #endregion

        #region Scanned Flash

        private IEnumerator ScannedFlashSequence()
        {
            Debug.Log($"[PPP] ScannedFlashSequence started, vignette intensity before: {_vignette.intensity.value}");
            _vignette.color.value = scannedFlashColor;

            float elapsed = 0f;
            float halfDuration = scannedFlashDuration * 0.5f;

            while (elapsed < halfDuration)
            {
                elapsed += Time.deltaTime;
                _vignette.intensity.value = Mathf.Lerp(0f, scannedFlashIntensity, elapsed / halfDuration);
                yield return null;
            }

            elapsed = 0f;
            while (elapsed < halfDuration)
            {
                elapsed += Time.deltaTime;
                _vignette.intensity.value = Mathf.Lerp(scannedFlashIntensity, 0f, elapsed / halfDuration);
                yield return null;
            }

            _vignette.intensity.value = 0f;
            Debug.Log("[PPP] ScannedFlashSequence complete");
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
