using UnityEngine;
using UnityEngine.InputSystem;
using PurrNet;
using Resonance.Combat;

namespace Resonance.PlayerController
{
    public class PlayerActionsInput : NetworkBehaviour, PlayerControls.IPlayerActionMapActions
    {
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

        // needed for disabling correctly after PurrNet resets the attribute
        private bool wasPreviouslyOwner;
        #endregion

        #region Startup
        private void Awake()
        {
            _playerLocomotionInput = GetComponent<PlayerLocomotionInput>();
            _overdriveAbility = GetComponent<OverdriveAbility>();
            _playerState = GetComponent<PlayerState>();
            _fpArmsAnimator = GetComponent<FPArmsAnimator>();
        }

        protected override void OnSpawned()
        {
            base.OnSpawned();
            enabled = isOwner;
            wasPreviouslyOwner = isOwner;

            if (isOwner)
            {
                PlayerInputManager.Instance.PlayerControls.PlayerActionMap.Enable();
                PlayerInputManager.Instance.PlayerControls.PlayerActionMap.AddCallbacks(this);
            }
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

            if (wasPreviouslyOwner)
            {
                PlayerInputManager.Instance.PlayerControls.PlayerActionMap.Disable();
                PlayerInputManager.Instance.PlayerControls.PlayerActionMap.RemoveCallbacks(this);
            }
        }
        #endregion

        #region Update

        private void Update()
        {
            // TODO: Implement action cancellation on movement
            if (_playerLocomotionInput.MovementInput != Vector2.zero)
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
            if (_playerState.IsDead() || _playerState.IsMatchFrozen())
                return;

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
            if (!context.performed || _playerState.IsDead() || _playerState.IsMatchFrozen())
                return;

            ReloadPressed = true;
        }

        public void OnInteract(InputAction.CallbackContext context)
        {
            if (!context.performed || _playerState.IsDead() || _playerState.IsMatchFrozen())
                return;

            InteractPressed = true;
        }

        public void OnOverdrive(InputAction.CallbackContext context)
        {
            if (!context.performed || _playerState.IsDead() || _playerState.IsMatchFrozen())
                return;

            _fpArmsAnimator?.RequestOverdriveActivation();
        }

        public void OnSwapSlotOne(InputAction.CallbackContext context)
        {
            if (!context.performed || _playerState.IsDead() || _playerState.IsMatchFrozen())
                return;

            SwapSlotOnePressed = true;
        }

        public void OnSwapSlotTwo(InputAction.CallbackContext context)
        {
            if (!context.performed || _playerState.IsDead() || _playerState.IsMatchFrozen())
                return;

            SwapSlotTwoPressed = true;
        }

        public void OnSwapWeapon(InputAction.CallbackContext context)
        {
            if (_playerState.IsDead() || _playerState.IsMatchFrozen())
                return;

            Vector2 scroll = context.ReadValue<Vector2>();
            if (Mathf.Abs(scroll.y) < 0.01f)
                return;

            SwapWeaponPressed = true;
        }

        public void OnStim(InputAction.CallbackContext context)
        {
            if (!context.performed || _playerState.IsDead() || _playerState.IsMatchFrozen())
                return;

            _fpArmsAnimator?.RequestStimActivation();
        }

        public void OnAbilityUpper(InputAction.CallbackContext context)
        {
            if (!context.performed || _playerState.IsDead() || _playerState.IsMatchFrozen())
                return;

            AbilityUpperPressed = true;
        }

        public void OnAbilityLower(InputAction.CallbackContext context)
        {
            if (!context.performed || _playerState.IsDead() || _playerState.IsMatchFrozen())
                return;

            AbilityLowerPressed = true;
        }

        public void OnShowMatchStats(InputAction.CallbackContext context)
        {
            if (_playerState != null && _playerState.IsDead())
                return;

            if (MatchStatsViewModel.Instance == null)
                return;

            if (context.started)
            {
                MatchStatsViewModel.Instance.Show();
            }
            else if (context.canceled)
            {
                MatchStatsViewModel.Instance.Hide();
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
            if (_playerState.IsDead() || _playerState.IsMatchFrozen())
                return;

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
    }
}
