using System;
using Resonance.Assemblies.UISystem;
using Resonance.LobbySystem.DataProviders;
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

        [Header("FPS counter")]
        [SerializeField] private Toggle fpsCounterToggle;

#if !UNITY_SERVER
        [Header("Wwise Events")]
        [SerializeField] private AK.Wwise.Event buttonClickEvent;
#endif

        public readonly static string Key = nameof(LobbySettingsOverlayView);
        string IOverlayView.Key => Key;

        private Action dismiss;

        public void OnHide()
        {
            gameObject.SetActive(false);
            slider.onValueChanged.RemoveListener(SetValue);
            fpsCounterToggle.onValueChanged.RemoveListener(HandleFpsToggleChanged);

            doneButton.onClick.RemoveListener(HandleDoneClicked);
            dismiss = null;
        }

        public void OnShow(OverlayViewActions viewActions)
        {
            gameObject.SetActive(true);
            dismiss = viewActions.Dismiss;

            slider.onValueChanged.AddListener(SetValue);
            UpdateRenderScaleDisplayValue();

            if (PlayerFacingFPSCounterDisplaySetting.Instance != null)
            {
                fpsCounterToggle.isOn = PlayerFacingFPSCounterDisplaySetting.Instance.IsEnabled;
            }
            fpsCounterToggle.onValueChanged.AddListener(HandleFpsToggleChanged);

            doneButton.onClick.AddListener(HandleDoneClicked);
        }

        private void HandleFpsToggleChanged(bool value)
        {
            if (PlayerFacingFPSCounterDisplaySetting.Instance != null)
            {
                PlayerFacingFPSCounterDisplaySetting.Instance.SetEnabled(value);
            }
        }

#if !UNITY_SERVER
        private void PostClick(AK.Wwise.Event wwiseEvent)
        {
            if (wwiseEvent != null && wwiseEvent.IsValid())
                wwiseEvent.Post(gameObject);
        }
#endif

        private void HandleDoneClicked()
        {
#if !UNITY_SERVER
            PostClick(buttonClickEvent);
#endif
            dismiss?.Invoke();
        }

        private void SetValue(float newValue)
        {
            if (RenderScaleSetter.Instance != null)
                RenderScaleSetter.Instance.ChangeRenderScale(newValue);
            SetDisplayValue(newValue);
        }

        private void UpdateRenderScaleDisplayValue()
        {
            if (RenderScaleSetter.Instance != null)
                SetDisplayValue(RenderScaleSetter.Instance.RenderScale);
        }

        private void SetDisplayValue(float displayValue)
        {
            displayText.text = $"{Math.Round(displayValue, 3)}";
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (eventData.pointerPressRaycast.gameObject != gameObject) return;
            dismiss?.Invoke();
        }
    }
}
