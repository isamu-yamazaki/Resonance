using UnityEngine;
using UnityEngine.InputSystem;

namespace Resonance.PlayerController
{
    [DefaultExecutionOrder(-2)]
    public class PlayerLocomotionInput : MonoBehaviour, PlayerControls.IPlayerLocomotionMapActions

    {
        #region Class Variables
        [SerializeField] private bool holdToSprint = true;
        public Vector2 MovementInput { get; private set; }
        public Vector2 LookInput { get; private set; }
        public bool JumpPressed { get; private set; }
        public bool SprintToggledOn { get; private set; }
        public bool CrouchToggledOn { get; private set; }

        private PlayerState _playerState;
        #endregion
        
        #region Startup

        private void Awake()
        {
            _playerState = GetComponent<PlayerState>();
        }
        
        private void OnEnable()
        {
            if (PlayerInputManager.Instance?.PlayerControls == null)
            {
                Debug.LogError("Player controls is not initialized - cannot enable");
                return;
            }
            
            PlayerInputManager.Instance.PlayerControls.PlayerLocomotionMap.Enable();
            PlayerInputManager.Instance.PlayerControls.PlayerLocomotionMap.SetCallbacks(this);
        }

        private void OnDisable()
        {
            if (PlayerInputManager.Instance?.PlayerControls == null)
            {
                Debug.LogError("Player controls is not initialized - cannot disable");
                return;
            }
            
            PlayerInputManager.Instance.PlayerControls.PlayerLocomotionMap.Disable();
            PlayerInputManager.Instance.PlayerControls.PlayerLocomotionMap.RemoveCallbacks(this);
        }
        #endregion

        #region Late Update Logic
        private void LateUpdate()
        {
            JumpPressed = false;
            
            // Disable crouch when airborne (jumping or falling)
            bool isAirborne = _playerState.CurrentPlayerMovementState == PlayerMovementState.Jumping ||
                              _playerState.CurrentPlayerMovementState == PlayerMovementState.Falling;

            if (isAirborne && CrouchToggledOn)
            {
                CrouchToggledOn = false;
            }
        }
        #endregion
        
        #region Public Methods
        public void DisableCrouch()
        {
            CrouchToggledOn = false;
        }
        #endregion
        
        #region Input Callbacks
        public void OnMovement(InputAction.CallbackContext context)
        {
            MovementInput = context.ReadValue<Vector2>();
        }

        public void OnLook(InputAction.CallbackContext context)
        {
            LookInput = context.ReadValue<Vector2>();
        }

        public void OnToggleSprint(InputAction.CallbackContext context)
        {
            if (context.performed)
            {
                SprintToggledOn = holdToSprint || !SprintToggledOn;
            }
            else if (context.canceled)
            {
                SprintToggledOn = !holdToSprint && SprintToggledOn;
            }
        }

        public void OnJump(InputAction.CallbackContext context)
        {
            if (!context.performed)
                return;

            JumpPressed = true;
        }

        public void OnToggleCrouch(InputAction.CallbackContext context)
        {
            if (!context.performed)
                return;

            CrouchToggledOn = !CrouchToggledOn;
        }

        #endregion
    }
}

