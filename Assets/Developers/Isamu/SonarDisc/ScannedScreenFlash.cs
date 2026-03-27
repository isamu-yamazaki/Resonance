using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Resonance.PlayerController
{
    /// <summary>
    /// Plain MonoBehaviour — modeled after OverdriveScreenTint.
    /// Flash() is called directly on the correct client by SonarDiscProjectile.
    /// </summary>
    public class ScannedScreenFlash : MonoBehaviour
    {
        [Header("Post Processing")]
        [SerializeField] private Volume _postProcessVolume;

        [Header("Flash Settings")]
        [SerializeField] private Color flashColor = new Color(1f, 0f, 0.8f, 1f);
        [SerializeField] private float flashIntensity = 0.6f;
        [SerializeField] private float flashDuration = 0.2f;

        private Vignette _vignette;
        private float _flashTimer = 0f;

        private void Awake()
        {
            if (_postProcessVolume != null && _postProcessVolume.profile != null)
                _postProcessVolume.profile.TryGet(out _vignette);
        }

        private void Start()
        {
            if (_vignette != null)
            {
                _vignette.intensity.overrideState = true;
                _vignette.color.overrideState = true;
                _vignette.color.value = flashColor;
                _vignette.intensity.value = 0f;
            }
        }

        private void Update()
        {
            if (_vignette == null) return;

            if (_flashTimer > 0f)
            {
                _flashTimer -= Time.deltaTime;
                _vignette.intensity.value = Mathf.Sin(Mathf.Clamp01(_flashTimer / flashDuration) * Mathf.PI) * flashIntensity;
            }
            else
            {
                _vignette.intensity.value = 0f;
            }
        }

        public void Flash()
        {
            _flashTimer = flashDuration;
        }
    }
}
