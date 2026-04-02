using System;
using Resonance.Helper;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Resonance.LobbySystem.TemporaryUI
{
    public class RenderScaleSlider : MonoBehaviour
    {
        [SerializeField] private Slider slider;
        [SerializeField] private TMP_Text displayText;

        private void Awake()
        {
            slider.onValueChanged.AddListener(SetValue);
            if (RenderScaleSetter.Instance != null)
            {
                SetDisplayValue(RenderScaleSetter.Instance.RenderScale);
                slider.value = RenderScaleSetter.Instance.RenderScale;
            }
        }

        private void SetValue(float newValue)
        {
            if (RenderScaleSetter.Instance != null)
            {
                RenderScaleSetter.Instance.ChangeRenderScale(newValue);
            }
            SetDisplayValue(newValue);
        }

        private void SetDisplayValue(float displayValue)
        {
            displayText.text = $"{Math.Round(displayValue, 3)}";
        }
    }
}
