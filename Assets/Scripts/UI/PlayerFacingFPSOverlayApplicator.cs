using Resonance.LobbySystem.DataProviders;
using UnityEngine;

namespace Resonance.UI
{
    public class PlayerFacingFPSOverlayApplicator : MonoBehaviour
    {
        private PlayerFacingFPSCounterDisplaySetting _setting;

        private void Start()
        {
            _setting = PlayerFacingFPSCounterDisplaySetting.Instance;
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
                InGameViewRouterBridge.Instance.ShowPlayerFacingFPSOverlay();
            }
            else
            {
                InGameViewRouterBridge.Instance.HidePlayerFacingFPSOverlay();
            }
        }
    }
}
