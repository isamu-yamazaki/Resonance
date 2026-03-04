using UnityEngine;
using UnityEngine.InputSystem;

namespace Resonance.PlayerController
{
    public class PlayerActionsInput : MonoBehaviour, PlayerControls.IPlayerActionMapActions
    {
        #region Class Variables
        public bool AttackPressed  { get; private set; }
        public bool AttackHeld { get; private set; }
        public bool ReloadPressed { get; private set; }
        public bool InteractPressed { get; private set; }
        public bool SwapSlotOnePressed  { get; private set; }
        public bool SwapSlotTwoPressed  { get; private set; }
        public bool SwapWeaponPressed  { get; private set; }
        
        public bool ShowStatsHeld { get; private set; }
        
        private PlayerLocomotionInput _playerLocomotionInput;
        private OverdriveAbility _overdriveAbility;
        private PlayerState _playerState;
        #endregion
        
        #region Startup
        private void Awake()
        {
            _playerLocomotionInput = GetComponent<PlayerLocomotionInput>();
            _overdriveAbility =  GetComponent<OverdriveAbility>();
            _playerState = GetComponent<PlayerState>();
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

        #endregion
        
        #region Input Callbacks
        public void OnAttack(InputAction.CallbackContext context)
        {
            if (_playerState.IsDead())
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
                AttackPressed = true;
            }
        }

        
        public void OnReload(InputAction.CallbackContext context)
        {
            if (!context.performed || _playerState.IsDead())
                return;

            ReloadPressed = true;
        }
        
        public void OnInteract(InputAction.CallbackContext context)
        {
            if (!context.performed || _playerState.IsDead())
                return;

            InteractPressed = true;
        }

        public void OnOverdrive(InputAction.CallbackContext context)
        {
            if (!context.performed || _playerState.IsDead())
                return;

            if (_overdriveAbility != null)
            {
                _overdriveAbility.TryActivateOverdrive();
            }
        }
        
        public void OnSwapSlotOne(InputAction.CallbackContext context)
        {
            if (!context.performed || _playerState.IsDead())
                return;

            SwapSlotOnePressed = true;
        }
        
        public void OnSwapSlotTwo(InputAction.CallbackContext context)
        {
            if (!context.performed || _playerState.IsDead())
                return;

            SwapSlotTwoPressed = true;
        }

        public void OnSwapWeapon(InputAction.CallbackContext context)
        {
            if (_playerState.IsDead())
                return;

            Vector2 scroll = context.ReadValue<Vector2>();
            if (Mathf.Abs(scroll.y) < 0.01f)
                return;

            SwapWeaponPressed = true;
        }



        public void OnEscape(InputAction.CallbackContext context)
        {
            if (!context.performed)
                return;

            if (Cursor.lockState == CursorLockMode.Locked)
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }
            else
            {
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }
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
        #endregion
        public void RequestReload()
        {
            if (_playerState != null && _playerState.IsDead())
                return;

            ReloadPressed = true;
        }
    }
}
