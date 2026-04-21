using System;
using Resonance.Assemblies.UISystem;
using Resonance.Helper;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Resonance.LobbySystem.NewUI
{
    public class LobbySettingsOverlayView : MonoBehaviour, IOverlayView, IPointerClickHandler
    {
        [SerializeField] private Button doneButton;

        [Header("Render scale slider")]
        [SerializeField] private Slider slider;
        [SerializeField] private TMP_Text displayText;

        public readonly static string Key = nameof(LobbySettingsOverlayView);
        string IOverlayView.Key => Key;

        private Action dismiss;

        public void OnHide()
        {
            gameObject.SetActive(false);
            slider.onValueChanged.RemoveListener(SetValue);

            dismiss = null;
        }

        public void OnShow(OverlayViewActions viewActions)
        {
            gameObject.SetActive(true);
            dismiss = viewActions.Dismiss;

            slider.onValueChanged.AddListener(SetValue);
            UpdateRenderScaleDisplayValue();

            doneButton.onClick.AddListener(HandleDoneClicked);
        }

        private void HandleDoneClicked()
        {
            dismiss?.Invoke();
        }

        private void SetValue(float newValue)
        {
            if (RenderScaleSetter.Instance != null)
            {
                RenderScaleSetter.Instance.ChangeRenderScale(newValue);
            }
            SetDisplayValue(newValue);
        }

        private void UpdateRenderScaleDisplayValue()
        {
            if (RenderScaleSetter.Instance != null)
            {
                SetDisplayValue(RenderScaleSetter.Instance.RenderScale);
            }
        }

        private void SetDisplayValue(float displayValue)
        {
            displayText.text = $"{Math.Round(displayValue, 3)}";
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (eventData.pointerPressRaycast.gameObject != gameObject)
            {
                return;
            }
            dismiss?.Invoke();
        }
    }
}
