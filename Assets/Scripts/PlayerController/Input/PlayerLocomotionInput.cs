using Resonance.Assemblies.Player;
using Resonance.Helper;
using Resonance.Match;
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
            if (_cachedPlayerStateReference == null) return;

            // Disable crouch when airborne (jumping or falling)
            bool isAirborne = _cachedPlayerStateReference.CurrentPlayerMovementState == PlayerMovementState.Jumping ||
                              _cachedPlayerStateReference.CurrentPlayerMovementState == PlayerMovementState.Falling;

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
            if (IsBlockedByPlayerOrMatchState())
            {
                MovementInput = Vector2.zero;
                return;
            }

            MovementInput = context.ReadValue<Vector2>();
        }

        public void OnLook(InputAction.CallbackContext context)
        {
            if (IsBlockedByPlayerOrMatchState())
            {
                LookInput = Vector2.zero;
                return;
            }

            LookInput = context.ReadValue<Vector2>();
        }

        public void OnToggleSprint(InputAction.CallbackContext context)
        {
            if (IsBlockedByPlayerOrMatchState()) return;

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
            if (IsBlockedByPlayerOrMatchState()) return;

            if (!context.performed)
            {
                JumpPressed = false;
                return;
            }

            JumpPressed = true;
        }

        public void OnToggleCrouch(InputAction.CallbackContext context)
        {
            if (IsBlockedByPlayerOrMatchState()) return;

            if (!context.performed) return;
            CrouchToggledOn = !CrouchToggledOn;
        }

        #endregion

        #region Input helpers

        private void TryCachePlayerComponentReferences()
        {
            if (_cachedPlayerStateReference != null) return;

            var go = OwnerFinder.FindGameObjectOfOwnedPlayerPredictedController();
            if (go == null) return;

            _cachedPlayerStateReference = go.GetComponent<PlayerState>();
        }

        private bool IsBlockedByPlayerOrMatchState()
        {
            TryCachePlayerComponentReferences();
            var roundManager = MatchLogicNetworkAdapter.Instance?.GetTemporaryActiveRoundManagerReference();
            return _cachedPlayerStateReference == null || roundManager == null ||
                   _cachedPlayerStateReference.IsDead() || !roundManager.IsMatchActive;
        }

        #endregion
    }
}
