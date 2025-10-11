using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Resonance.PlayerController
{
    public class OverdriveVisualEffects : MonoBehaviour
    {
        #region Class Variables
        [Header("Post Processing")]
        [SerializeField] private Volume _postProcessVolume;
        
        [Header("Screen Tint Settings")]
        [SerializeField] private Color overdriveTintColor = new Color(0f, 1f, 0.5f, 1f); // Cyan-green
        [SerializeField] private float tintIntensity = 0.3f;
        [SerializeField] private float tintTransitionSpeed = 5f;
        
        private OverdriveAbility _overdriveAbility;
        private ColorAdjustments _colorAdjustments;
        private float _currentTintWeight = 0f;
        #endregion
        
        #region Startup
        private void Awake()
        {
            _overdriveAbility = GetComponent<OverdriveAbility>();

            if (_postProcessVolume != null && _postProcessVolume.profile != null)
            {
                if (!_postProcessVolume.profile.TryGet(out _colorAdjustments))
                {
                    _colorAdjustments = _postProcessVolume.profile.Add<ColorAdjustments>();
                }
            }
            else
            {
                Debug.LogWarning("OverdriveVisualEffects: No Post Process Volume assigned! Screen tint will not work.");
            }
        }

        private void Start()
        {
            if (_colorAdjustments != null)
            {
                _colorAdjustments.colorFilter.overrideState = true;
                _colorAdjustments.colorFilter.value = Color.white;
            }
        }
        #endregion
        
        #region Update Logic
        private void Update()
        {
            UpdateScreenTint();
        }

        private void UpdateScreenTint()
        {
            if (_colorAdjustments == null || _overdriveAbility == null) return;

            float targetWeight = _overdriveAbility.IsInOverdrive ? 1f : 0f;
            
            _currentTintWeight = Mathf.Lerp(_currentTintWeight, targetWeight, tintTransitionSpeed * Time.deltaTime);
            
            Color targetColor = Color.Lerp(Color.white, overdriveTintColor, _currentTintWeight * tintIntensity);
            _colorAdjustments.colorFilter.value = targetColor;
        }
        #endregion
    }
}
