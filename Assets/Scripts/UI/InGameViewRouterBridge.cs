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

        private void Awake()
        {
            if (Instance != null)
            {
                Destroy(this);
            }
            Instance = this;

            viewRouter = new ViewRouter();

            var shopOverlayOptions = new OverlayOptions
            {
                view = GetComponentInChildren<ShopOverlayView>(),
                inputMapsToDisableWhenShown = new InputActionMap[]
                    {
                        // TODO: make a dedicated input map for shop/esc so we can disable player actions
                        PlayerInputManager.Instance.PlayerControls.PlayerLocomotionMap,
                    },
                unlockCursorWhenShown = true
            };
            shopOverlayId = viewRouter.RegisterOverlay(shopOverlayOptions);

            var escOverlayOptions = new OverlayOptions
            {
                view = GetComponentInChildren<EscOverlayView>(),
                inputMapsToDisableWhenShown = new InputActionMap[]
                    {
                        PlayerInputManager.Instance.PlayerControls.PlayerLocomotionMap,
                    },
                unlockCursorWhenShown = true
            };
            escOverlayId = viewRouter.RegisterOverlay(escOverlayOptions);
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
            if (viewRouter.ActiveOverlayIds.Contains(shopOverlayId))
            {
                viewRouter.HideOverlay(shopOverlayId);
            }
            else
            {
                viewRouter.ShowOverlay(shopOverlayId);
            }
        }

        public void ToggleEsc()
        {
            if (viewRouter.ActiveOverlayIds.Contains(escOverlayId))
            {
                viewRouter.HideOverlay(escOverlayId);
            }
            else
            {
                viewRouter.ShowOverlay(escOverlayId);
            }
        }
    }
}
