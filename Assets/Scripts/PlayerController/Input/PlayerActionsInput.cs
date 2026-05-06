using UnityEngine;
using UnityEngine.InputSystem;
using Resonance.Combat;
using Resonance.Helper;

namespace Resonance.PlayerController
{
    public class PlayerActionsInput : MonoBehaviour, PlayerControls.IPlayerActionMapActions
    {
        public static PlayerActionsInput Instance { get; private set; }

        #region Class Variables
        public bool AttackPressed { get; private set; }
        public bool AttackHeld { get; private set; }
        public bool ReloadPressed { get; private set; }
        public bool InteractPressed { get; private set; }
        public bool SwapSlotOnePressed { get; private set; }
        public bool SwapSlotTwoPressed { get; private set; }
        public bool SwapWeaponPressed { get; private set; }
        public bool HealPressed { get; private set; }

        public bool AbilityUpperPressed { get; private set; }

        public bool AbilityLowerPressed { get; private set; }

        public bool ShowStatsHeld { get; private set; }

        public bool ToggleShopPressed { get; private set; }

        public bool AdsHeld { get; private set; }
        public bool OverdrivePressed { get; private set; }
        public bool StimPressed { get; private set; }

        private PlayerLocomotionInput _playerLocomotionInput;
        private OverdriveAbility _cachedOverdriveAbilityReference;
        private PlayerState _cachedPlayerStateReference;
        #endregion

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

            _playerLocomotionInput = PlayerLocomotionInput.Instance;
            
            PlayerInputManager.Instance.PlayerControls.PlayerActionMap.Enable();
            PlayerInputManager.Instance.PlayerControls.PlayerActionMap.AddCallbacks(this);
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        private void OnEnable()
        {
            if (PlayerInputManager.Instance?.PlayerControls == null)
            {
                Debug.LogError("Player controls is not initialized - cannot enable");
                return;
            }
        }

        private void OnDisable()
        {
            if (PlayerInputManager.Instance?.PlayerControls == null)
            {
                Debug.LogError("Player controls is not initialized - cannot disable");
                return;
            }

            PlayerInputManager.Instance.PlayerControls.PlayerActionMap.Disable();
            PlayerInputManager.Instance.PlayerControls.PlayerActionMap.RemoveCallbacks(this);
        }

        // sometime after the player has spawned, try to get component references
        private void TryCachePlayerComponentReferences()
        {
            if (_cachedOverdriveAbilityReference != null && _cachedPlayerStateReference != null) return;

            var gameObject = OwnerFinder.FindGameObjectOfOwnedPlayerPredictedController();
            if (gameObject == null) return;

            _cachedOverdriveAbilityReference = gameObject.GetComponent<OverdriveAbility>();
            _cachedPlayerStateReference = gameObject.GetComponent<PlayerState>();
        }
        #endregion

        #region Update

        private void Update()
        {
            // TODO: Implement action cancellation on movement
            if (_playerLocomotionInput != null && _playerLocomotionInput.MovementInput != Vector2.zero)
            {
                // Cancels interruptible animations while moving
                // AttackPressed = false;
            }
        }

        public void SetAttackPressedFalse()
        {
            AttackPressed = false;
        }

        public void SetReloadPressedFalse()
        {
            ReloadPressed = false;
        }

        public void SetInteractPressedFalse()
        {
            InteractPressed = false;
        }
        public void SetSlotOnePressedFalse()
        {
            SwapSlotOnePressed = false;
        }

        public void SetSlotTwoPressedFalse()
        {
            SwapSlotTwoPressed = false;
        }

        public void SetSwapWeaponPressedFalse()
        {
            SwapWeaponPressed = false;
        }

        public void SetHealPressedFalse()
        {
            HealPressed = false;
        }

        public void SetAbilityUpperPressedFalse()
        {
            AbilityUpperPressed = false;
        }

        public void SetAbilityLowerPressedFalse()
        {
            AbilityLowerPressed = false;
        }

        public void SetOverdrivePressedFales()
        {
            OverdrivePressed = false;
        }

        public void SetStimPressedFalse()
        {
            StimPressed = false;
        }

        #endregion

        #region Input Callbacks
        public void OnAttack(InputAction.CallbackContext context)
        {
            if (IsBlockedByPlayerState()) return;

            if (context.started)
            {
                AttackHeld = true;
                AttackPressed = true;
            }
            else if (context.canceled)
            {
                AttackHeld = false;
            }
            else if (context.performed)
            {
                AttackHeld = true;
            }
        }

        public void OnReload(InputAction.CallbackContext context)
        {
            if (!context.performed || IsBlockedByPlayerState()) return;

            ReloadPressed = true;
        }

        public void OnInteract(InputAction.CallbackContext context)
        {
            if (!context.performed || IsBlockedByPlayerState()) return;

            InteractPressed = true;
        }

        public void OnOverdrive(InputAction.CallbackContext context)
        {
            if (!context.performed || IsBlockedByPlayerState()) return;

            OverdrivePressed = true;
        }

        public void OnSwapSlotOne(InputAction.CallbackContext context)
        {
            if (!context.performed || IsBlockedByPlayerState()) return;

            SwapSlotOnePressed = true;
        }

        public void OnSwapSlotTwo(InputAction.CallbackContext context)
        {
            if (!context.performed || IsBlockedByPlayerState()) return;

            SwapSlotTwoPressed = true;
        }

        public void OnSwapWeapon(InputAction.CallbackContext context)
        {
            if (IsBlockedByPlayerState()) return;

            Vector2 scroll = context.ReadValue<Vector2>();
            if (Mathf.Abs(scroll.y) < 0.01f)
                return;

            SwapWeaponPressed = true;
        }

        public void OnStim(InputAction.CallbackContext context)
        {
            if (!context.performed || IsBlockedByPlayerState()) return;

            StimPressed = true;
        }

        public void OnAbilityUpper(InputAction.CallbackContext context)
        {
            if (!context.performed || IsBlockedByPlayerState()) return;

            AbilityUpperPressed = true;
        }

        public void OnAbilityLower(InputAction.CallbackContext context)
        {
            if (!context.performed || IsBlockedByPlayerState()) return;

            AbilityLowerPressed = true;
        }

        public void OnShowMatchStats(InputAction.CallbackContext context)
        {
            TryCachePlayerComponentReferences();
            if (_cachedPlayerStateReference != null && _cachedPlayerStateReference.IsDead())
                return;

            var bridge = UI.InGameViewRouterBridge.Instance;
            if (bridge == null)
                return;

            if (context.started)
            {
                bridge.ShowMatchStats();
            }
            else if (context.canceled)
            {
                bridge.HideMatchStats();
            }
        }

        public void OnADS(InputAction.CallbackContext context)
        {
            if (context.started)
                AdsHeld = true;
            else if (context.canceled)
                AdsHeld = false;
        }

        #endregion
        public void RequestReload()
        {
            if (IsBlockedByPlayerState()) return;

            ReloadPressed = true;
        }

        public void ResetAllInputs()
        {
            AttackPressed = false;
            AttackHeld = false;
            ReloadPressed = false;
            SwapWeaponPressed = false;
            SwapSlotOnePressed = false;
            SwapSlotTwoPressed = false;
            HealPressed = false;
            AbilityUpperPressed = false;
            AbilityLowerPressed = false;
            AdsHeld = false;
        }

        private bool IsBlockedByPlayerState()
        {
            TryCachePlayerComponentReferences();
            return _cachedPlayerStateReference != null && (_cachedPlayerStateReference.IsDead() || _cachedPlayerStateReference.IsMatchFrozen());
        }
    }
}
