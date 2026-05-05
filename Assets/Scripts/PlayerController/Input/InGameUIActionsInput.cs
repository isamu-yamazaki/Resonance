using Resonance.UI;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Resonance.PlayerController
{
    public class InGameUIActionsInput : MonoBehaviour, PlayerControls.IInGameUIActionMapActions
    {
        public static InGameUIActionsInput Instance { get; private set; }

        #region Startup
        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);

            PlayerInputManager.Instance.PlayerControls.InGameUIActionMap.Enable();
            PlayerInputManager.Instance.PlayerControls.InGameUIActionMap.AddCallbacks(this);
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        private void OnDisable()
        {
            if (PlayerInputManager.Instance?.PlayerControls == null)
            {
                Debug.LogError("Player controls is not initialized - cannot disable");
                return;
            }

            PlayerInputManager.Instance.PlayerControls.InGameUIActionMap.Disable();
            PlayerInputManager.Instance.PlayerControls.InGameUIActionMap.RemoveCallbacks(this);
        }

        #endregion


        #region Input Callbacks
        public void OnEscape(InputAction.CallbackContext context)
        {
            if (!context.performed)
                return;

            if (InGameViewRouterBridge.Instance == null)
                return;

            if (InGameViewRouterBridge.Instance.IsShopOpen)
            {
                InGameViewRouterBridge.Instance.ToggleShop();
                return;
            }

            InGameViewRouterBridge.Instance.ToggleEsc();
        }

        public void OnToggleShop(InputAction.CallbackContext context)
        {
            if (!context.performed)
            {
                return;
            }

            if (InGameViewRouterBridge.Instance == null)
            {
                return;
            }

            InGameViewRouterBridge.Instance.ToggleShop();
        }

        public void OnToggleDebugMenu(InputAction.CallbackContext context)
        {
            if (!context.performed)
            {
                return;
            }

            if (InGameViewRouterBridge.Instance == null)
            {
                return;
            }

            InGameViewRouterBridge.Instance.ToggleDebugMenu();
        }

        public void OnToggleStatsOverlay(InputAction.CallbackContext context)
        {
            if (!context.performed)
            {
                return;
            }

            if (InGameViewRouterBridge.Instance == null)
            {
                return;
            }

            InGameViewRouterBridge.Instance.TogglePerformanceStats();
        }
    }

    #endregion
}
