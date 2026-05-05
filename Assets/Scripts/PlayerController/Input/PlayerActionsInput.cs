using UnityEngine;
using UnityEngine.InputSystem;
using Resonance.Combat;

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

        private PlayerLocomotionInput _playerLocomotionInput;
        private OverdriveAbility _overdriveAbility;
        private PlayerState _playerState;
        private FPArmsAnimator _fpArmsAnimator;
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
            _overdriveAbility = FindFirstObjectByType<OverdriveAbility>();
            _playerState = FindFirstObjectByType<PlayerState>();
            _fpArmsAnimator = FindFirstObjectByType<FPArmsAnimator>();

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

            _fpArmsAnimator?.RequestOverdriveActivation();
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

            _fpArmsAnimator?.RequestStimActivation();
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
            if (_playerState != null && _playerState.IsDead())
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
            return _playerState != null && (_playerState.IsDead() || _playerState.IsMatchFrozen());
        }
    }
}
