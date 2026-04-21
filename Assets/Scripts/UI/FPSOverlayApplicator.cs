using Resonance.LobbySystem.DataProviders;
using UnityEngine;

namespace Resonance.UI
{
    public class FPSOverlayApplicator : MonoBehaviour
    {
        private FPSCounterDisplaySetting _setting;

        private void Start()
        {
            _setting = FPSCounterDisplaySetting.Instance;
            if (_setting == null) { return; }

            ApplyState(_setting.IsEnabled);
            _setting.OnIsEnabledChanged.AddListener(ApplyState);
        }

        private void OnDestroy()
        {
            if (_setting != null)
            {
                _setting.OnIsEnabledChanged.RemoveListener(ApplyState);
            }
        }

        private void ApplyState(bool enabled)
        {
            if (InGameViewRouterBridge.Instance == null) { return; }

            if (enabled)
            {
                InGameViewRouterBridge.Instance.ShowFPSOverlay();
            }
            else
            {
                InGameViewRouterBridge.Instance.HideFPSOverlay();
            }
        }
    }
}
