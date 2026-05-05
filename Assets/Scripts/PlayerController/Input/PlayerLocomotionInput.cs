using Resonance.Assemblies.Player;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Resonance.PlayerController
{
    [DefaultExecutionOrder(-2)]
    public class PlayerLocomotionInput : MonoBehaviour, PlayerControls.IPlayerLocomotionMapActions
    {
        public static PlayerLocomotionInput Instance { get; private set; }

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
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);

            _playerState = FindFirstObjectByType<PlayerState>();

            PlayerInputManager.Instance.PlayerControls.PlayerLocomotionMap.Enable();
            PlayerInputManager.Instance.PlayerControls.PlayerLocomotionMap.AddCallbacks(this);
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

            PlayerInputManager.Instance.PlayerControls.PlayerLocomotionMap.Disable();
            PlayerInputManager.Instance.PlayerControls.PlayerLocomotionMap.RemoveCallbacks(this);
        }
        #endregion

        #region Late Update Logic
        private void LateUpdate()
        {
            JumpPressed = false;

            if (_playerState == null) return;

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
            if (_playerState != null && (_playerState.IsDead() || _playerState.IsMatchFrozen()))
            {
                MovementInput = Vector2.zero;
                return;
            }

            MovementInput = context.ReadValue<Vector2>();
        }

        public void OnLook(InputAction.CallbackContext context)
        {
            if (_playerState != null && (_playerState.IsDead() || _playerState.IsMatchFrozen()))
            {
                LookInput = Vector2.zero;
                return;
            }

            LookInput = context.ReadValue<Vector2>();
        }

        public void OnToggleSprint(InputAction.CallbackContext context)
        {
            if (_playerState != null && (_playerState.IsDead() || _playerState.IsMatchFrozen())) return;

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
            if (!context.performed) return;
            if (_playerState != null && (_playerState.IsDead() || _playerState.IsMatchFrozen())) return;

            JumpPressed = true;
        }

        public void OnToggleCrouch(InputAction.CallbackContext context)
        {
            if (!context.performed) return;
            if (_playerState != null && (_playerState.IsDead() || _playerState.IsMatchFrozen())) return;

            CrouchToggledOn = !CrouchToggledOn;
        }

        #endregion
    }
}
