using System.Linq;
using Resonance.Assemblies.UISystem;
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

        private int shopOverlayId;
        private int escOverlayId;

        public bool IsShopOpen => viewRouter.ActiveOverlayIds.Contains(shopOverlayId);
        public bool IsEscOpen  => viewRouter.ActiveOverlayIds.Contains(escOverlayId);

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
            var shopView = GetComponentInChildren<ShopOverlayView>();
            if (shopView == null) { Debug.LogError("[InGameViewRouterBridge] ShopOverlayView not found in children."); }

            var escView = GetComponentInChildren<EscOverlayView>();
            if (escView == null) { Debug.LogError("[InGameViewRouterBridge] EscOverlayView not found in children."); }

            var playerLocomotionAndActions = new InputActionMap[]
            {
                PlayerInputManager.Instance.PlayerControls.PlayerLocomotionMap,
                PlayerInputManager.Instance.PlayerControls.PlayerActionMap,
            };

            shopOverlayId = viewRouter.RegisterOverlay(new OverlayOptions
            {
                view = shopView,
                inputMapsToDisableWhenShown = playerLocomotionAndActions,
                unlockCursorWhenShown = true
            });
            escOverlayId = viewRouter.RegisterOverlay(new OverlayOptions
            {
                view = escView,
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
            viewRouter.ToggleOverlay(shopOverlayId);
        }

        public void ToggleEsc()
        {
            viewRouter.ToggleOverlay(escOverlayId);
        }
    }
}
