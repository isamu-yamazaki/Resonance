using System.Collections;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Resonance.PlayerController
{
    /// <summary>
    /// Singleton MonoBehaviour modeled after DamageIndicatorUI.
    /// Waits for the local player to spawn then grabs their post-process volume.
    /// Flash() is called via Instance from SonarDiscProjectile.
    /// </summary>
    public class ScannedScreenFlash : MonoBehaviour
    {
        public static ScannedScreenFlash Instance { get; private set; }

        [Header("Flash Settings")]
        [SerializeField] private Color flashColor = new Color(1f, 0f, 0.8f, 1f);
        [SerializeField] private float flashIntensity = 0.6f;
        [SerializeField] private float flashDuration = 0.2f;

        private Vignette _vignette;
        private float _flashTimer = 0f;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
        }

        private void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }

        private IEnumerator Start()
        {
            while (PlayerController.LocalPlayer == null)
                yield return null;

            Volume volume = PlayerController.LocalPlayer.GetComponent<Volume>();
            if (volume != null && volume.profile != null)
            {
                volume.profile.TryGet(out _vignette);

                if (_vignette != null)
                {
                    _vignette.intensity.overrideState = true;
                    _vignette.color.overrideState = true;
                    _vignette.color.value = flashColor;
                    _vignette.intensity.value = 0f;
                }
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
