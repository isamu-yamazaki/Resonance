using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Resonance.PlayerController
{
    public class PlayerActionsInput : MonoBehaviour, PlayerControls.IPlayerActionMapActions
    {
        #region Class Variables
        public bool AttackPressed  { get; private set; }
        public bool ReloadPressed { get; private set; }
        
        private PlayerLocomotionInput _playerLocomotionInput;
        #endregion
        
        #region Startup
        private void Awake()
        {
            _playerLocomotionInput = GetComponent<PlayerLocomotionInput>();
        }

        private void OnEnable()
        {
            if (PlayerInputManager.Instance?.PlayerControls == null)
            {
                Debug.LogError("Player controls is not initialized - cannot enable");
                return;
            }
            
            PlayerInputManager.Instance.PlayerControls.PlayerActionMap.Enable();
            PlayerInputManager.Instance.PlayerControls.PlayerActionMap.SetCallbacks(this);
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
        #endregion
        
        #region Input Callbacks
        public void OnAttack(InputAction.CallbackContext context)
        {
            if (!context.performed)
                return;

            AttackPressed = true;
        }
        
        public void OnReload(InputAction.CallbackContext context)
        {
            if (!context.performed)
                return;

            ReloadPressed = true;
        }
        #endregion
    }
}
