using System.Linq;
using Resonance.Assemblies.UISystem;
using Resonance.DebugTools;
using Resonance.Shop;
using UnityEngine;
using UnityEngine.InputSystem;
using PlayerInputManager = Resonance.PlayerController.PlayerInputManager;

namespace Resonance.UI
{
    public class InGameViewRouterBridge : MonoBehaviour
    {
        public static InGameViewRouterBridge Instance { get; private set; }

        public ViewRouter viewRouter { get; private set; }

        public bool IsShopOpen => viewRouter.ActiveOverlayKeys.Contains(ShopOverlayView.Key);
        public bool IsEscOpen => viewRouter.ActiveOverlayKeys.Contains(EscOverlayView.Key);
        public bool IsDebugMenuOpen => viewRouter.ActiveOverlayKeys.Contains(DebugOverlayView.Key);
        public bool IsPerformanceStatsOpen => viewRouter.ActiveOverlayKeys.Contains(PerformanceStatsOverlay.Key);
        public bool IsPlayerFacingFPSOverlayOpen => viewRouter.ActiveOverlayKeys.Contains(PlayerFacingFPSOverlayView.Key);
        public bool IsMatchEndOpen => viewRouter.ActiveOverlayKeys.Contains(MatchEndOverlayView.Key);

        private void Awake()
        {
            if (Instance != null)
            {
                Destroy(this);
            }
            Instance = this;

            viewRouter = new ViewRouter();
            RegisterOverlays();
        }

        private void RegisterOverlays()
        {
            var shopView = GetComponentInChildren<ShopOverlayView>(includeInactive: true);
            if (shopView == null)
            {
                Debug.LogError("[InGameViewRouterBridge] ShopOverlayView not found in children.");
                return;
            }

            var escView = GetComponentInChildren<EscOverlayView>(includeInactive: true);
            if (escView == null)
            {
                Debug.LogError("[InGameViewRouterBridge] EscOverlayView not found in children.");
                return;
            }

            var debugView = GetComponentInChildren<DebugOverlayView>(includeInactive: true);
            if (debugView == null)
            {
                Debug.LogError("[InGameViewRouterBridge] DebugMenuManager not found in children.");
                return;
            }

            var perfStatsView = GetComponentInChildren<PerformanceStatsOverlay>(includeInactive: true);
            if (perfStatsView == null)
            {
                Debug.LogError("[InGameViewRouterBridge] PerformanceStatsOverlay not found in children.");
                return;
            }

            var fpsView = GetComponentInChildren<PlayerFacingFPSOverlayView>(includeInactive: true);
            if (fpsView == null)
            {
                Debug.LogError("[InGameViewRouterBridge] PlayerFacingFPSOverlayView not found in children.");
                return;
            }

            var matchEndView = GetComponentInChildren<MatchEndOverlayView>(includeInactive: true);
            if (matchEndView == null)
            {
                Debug.LogError("[InGameViewRouterBridge] MatchEndOverlayView not found in children.");
                return;
            }

            var playerLocomotionAndActions = new InputActionMap[]
            {
                PlayerInputManager.Instance.PlayerControls.PlayerLocomotionMap,
                PlayerInputManager.Instance.PlayerControls.PlayerActionMap,
            };

            viewRouter.RegisterOverlay(new OverlayOptions
            {
                view = shopView,
                inputMapsToDisableWhenShown = playerLocomotionAndActions,
                unlockCursorWhenShown = true
            });
            viewRouter.RegisterOverlay(new OverlayOptions
            {
                view = escView,
                inputMapsToDisableWhenShown = playerLocomotionAndActions,
                unlockCursorWhenShown = true
            });
            viewRouter.RegisterOverlay(new OverlayOptions
            {
                view = debugView,
                unlockCursorWhenShown = true
            });
            viewRouter.RegisterOverlay(new OverlayOptions
            {
                view = perfStatsView,
                // intentionally: no inputMapsToDisableWhenShown, unlockCursorWhenShown = false
            });
            viewRouter.RegisterOverlay(new OverlayOptions
            {
                view = fpsView,
                // intentionally: no inputMapsToDisableWhenShown, unlockCursorWhenShown = false
            });
            viewRouter.RegisterOverlay(new OverlayOptions
            {
                view = matchEndView,
                inputMapsToDisableWhenShown = playerLocomotionAndActions,
                unlockCursorWhenShown = true
            });
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }

        public void ToggleShop()
        {
            viewRouter.ToggleOverlay(ShopOverlayView.Key);
        }

        public void ToggleEsc()
        {
            viewRouter.ToggleOverlay(EscOverlayView.Key);
        }

        public void ToggleDebugMenu()
        {
            viewRouter.ToggleOverlay(DebugOverlayView.Key);
        }

        public void TogglePerformanceStats()
        {
            viewRouter.ToggleOverlay(PerformanceStatsOverlay.Key);
        }

        public void ShowPlayerFacingFPSOverlay()
        {
            if (!IsPlayerFacingFPSOverlayOpen)
            {
                viewRouter.ToggleOverlay(PlayerFacingFPSOverlayView.Key);
            }
        }

        public void HidePlayerFacingFPSOverlay()
        {
            if (IsPlayerFacingFPSOverlayOpen)
            {
                viewRouter.ToggleOverlay(PlayerFacingFPSOverlayView.Key);
            }
        }

        public void ShowMatchEnd()
        {
            if (!IsMatchEndOpen)
            {
                viewRouter.ToggleOverlay(MatchEndOverlayView.Key);
            }
        }

        public void HideMatchEnd()
        {
            if (IsMatchEndOpen)
            {
                viewRouter.ToggleOverlay(MatchEndOverlayView.Key);
            }
        }
    }
}
