using PurrNet.Prediction;
using Resonance.Assemblies.Player;
using Resonance.Combat.Augments;
using Resonance.Player;
using Resonance.Train;
using Unity.Cinemachine;
using UnityEngine;

namespace Resonance.PlayerController
{
    /// <summary>
    /// Predicted equivalent of PlayerController. Mirrors its public API surface and
    /// component dependencies, but runs the movement tick inside PurrNet's prediction
    /// loop via PlayerSimulation.Step.
    /// </summary>
    [DefaultExecutionOrder(-1)]
    [RequireComponent(typeof(CharacterController))]
    public class PlayerPredictedController : PredictedIdentity<PlayerInputData, PlayerMovementDataState>
    {
        #region Inspector

        [Header("Components")]
        [SerializeField] private CharacterController _characterController;
        [SerializeField] private CinemachineCamera _virtualCamera;

        [Tooltip("Shared parent of the first-person camera and FP arms. Driven from the " +
                 "interpolated PredictedTransform.graphics so the local view feels as smooth " +
                 "as the third-person skin, instead of stepping with the raw simulated root.")]
        [SerializeField] private Transform _firstPersonViewRoot;

        [Header("Config")]
        [SerializeField] private PlayerConfig _config;

        [Header("Environment Details")]
        [SerializeField] private LayerMask _groundLayers;

        #endregion

        #region Component dependencies

        private PlayerLocomotionInput _playerLocomotionInput;
        private PlayerState _playerState;
        private OverdriveAbility _overdriveAbility;
        private PlayerStats _playerStats;
        private TrainPassengerPhysics _trainPassengerPhysics;
        private AbilityGrappleHook _grapple;
        private float _stepOffset;

        // Both yaw and pitch are fully local for better responsiveness
        private float _cameraPitch;
        private float _cameraYaw;

        /// <summary>
        /// The first-person view root's offset relative to the raw simulated root, captured once
        /// in root-local space. Reapplied each frame on top of the interpolated graphics position
        /// so the eye height/offset is preserved without compounding.
        /// </summary>
        private Vector3 _firstPersonViewRootOffsetFromRoot;
        private bool _hasFirstPersonViewRoot;

        #endregion

        #region Public API (mirrors PlayerController)

        public static PlayerPredictedController LocalPlayer { get; private set; }

        /// <summary>
        /// The sibling PredictedTransform that owns this player's networked position+rotation and
        /// interpolation. This controller drives CharacterController.Move each tick and
        /// PredictedTransform captures the resulting transform; repositioning is funneled through
        /// this controller's simulation methods (see <see cref="SimulatePlaceAtRespawnPoint"/>).
        /// </summary>
        private PredictedTransform _predictedTransform;

        /// <summary>
        /// The interpolated transform that player visuals (e.g. the third-person body) should
        /// parent under so they follow the smooth transform rather than the raw simulated root.
        /// Falls back to the simulated root until PredictedTransform/_graphics is wired.
        /// </summary>
        public Transform GraphicsRoot =>
            _predictedTransform != null && _predictedTransform.graphics != null
                ? _predictedTransform.graphics
                : transform;

        public float RotationMismatch { get; private set; }
        public bool IsRotatingToTarget { get; private set; }
        public bool IsPlayerDead { get; set; }

        public void ApplyJumpVelocity(float velocity)
        {
            currentState.Velocity.y = velocity;
        }

        public void ApplyImpulse(Vector3 impulse)
        {
            currentState.Velocity.y = impulse.y;
            currentState.GrappleImpulse = new Vector3(impulse.x, 0f, impulse.z);
        }

        public override void ResetState()
        {
            currentState.Velocity = Vector3.zero;
            currentState.GrappleImpulse = Vector3.zero;
            currentState.JumpedLastSimulatedFrame = false;
            currentState.SlideTimer = 0f;
            IsRotatingToTarget = false;

            if (_characterController != null)
                _characterController.stepOffset = _stepOffset;

            _trainPassengerPhysics?.SimulateClearInertia();
        }

        #endregion

        #region Lifecycle

        protected override void LateAwake()
        {
            _playerLocomotionInput = PlayerLocomotionInput.Instance;
            _playerState = GetComponent<PlayerState>();
            _overdriveAbility = GetComponent<OverdriveAbility>();
            _playerStats = GetComponent<PlayerStats>();
            _trainPassengerPhysics = GetComponent<TrainPassengerPhysics>();
            _grapple = GetComponent<AbilityGrappleHook>();
            _predictedTransform = GetComponent<PredictedTransform>();

            _stepOffset = _characterController != null ? _characterController.stepOffset : 0f;

            if (_virtualCamera != null)
                _virtualCamera.Lens.FieldOfView = _config.baseFOV;

            if (isOwner)
                LocalPlayer = this;

            if (_virtualCamera != null)
                _virtualCamera.gameObject.SetActive(isOwner);

            _hasFirstPersonViewRoot = _firstPersonViewRoot != null;
            if (_hasFirstPersonViewRoot)
            {
                // Capture the view root's current offset from the simulated root in root-local
                // space (authored eye height/offset). Reapplied every frame against the smooth
                // graphics position; computing in root-local space keeps it correct under body yaw.
                _firstPersonViewRootOffsetFromRoot =
                    Quaternion.Inverse(transform.rotation) * (_firstPersonViewRoot.position - transform.position);
            }
        }

        #endregion

        #region Prediction overrides

        protected override PlayerMovementDataState GetInitialState()
        {
            return new PlayerMovementDataState
            {
                Velocity = Vector3.zero,
                CameraYaw = transform.eulerAngles.y,
                GrappleImpulse = Vector3.zero,
                JumpedLastSimulatedFrame = false,
                WasGroundedLastTick = true,
                SimulatedMovementStateResult = PlayerMovementState.Falling,
                SlideTimer = 0f,
            };
        }

        protected override void UpdateInput(ref PlayerInputData input)
        {
            if (!isOwner) return;
            input.JumpPressed |= _playerLocomotionInput.JumpPressed;
            input.SprintToggledOn |= _playerLocomotionInput.SprintToggledOn;
            input.CrouchToggledOn |= _playerLocomotionInput.CrouchToggledOn;

            _cameraYaw += _playerLocomotionInput.LookInput.x * _config.lookSensitivityH;
            _cameraPitch = Mathf.Clamp(
                _cameraPitch - _config.lookSensitivityV * _playerLocomotionInput.LookInput.y,
                -_config.lookLimitV,
                _config.lookLimitV);
        }

        protected override void GetFinalInput(ref PlayerInputData input)
        {
            if (!isOwner) return;
            input.MovementInput = _playerLocomotionInput.MovementInput;
            input.CameraYaw = _cameraYaw;
        }


        protected override void Simulate(PlayerInputData input, ref PlayerMovementDataState state, float delta)
        {
            // Bail-outs ported from PlayerController.Update.
            if (IsPlayerDead) return;
            if (_playerState.IsDead()) return;
            if (_playerState.IsMatchFrozen()) return;
            if (_playerState.IsZiplining()) return;
            if (!_characterController.enabled) return;
            
            // Player movement state:
            // The current player movement state is read from _playerState.
            // A resulting state is calculated in PlayerSimulation, then
            // PlayerPredictedController feeds it back to PlayerState in the
            // simulation loop.

            var deps = new PlayerDependencyData
            {
                MovementSpeedMultiplier = _playerStats.PlayerSpeed,
                CurrentPlayerMovementState = _playerState.CurrentPlayerMovementState,
                TrainVelocityOffset = _trainPassengerPhysics != null
                    ? _trainPassengerPhysics.GetTickVelocityOffset()
                    : Vector3.zero,
                TrainKnockbackVertical = _trainPassengerPhysics != null
                    ? _trainPassengerPhysics.GetKnockbackVertical()
                    : 0f,
                GroundLayers = _groundLayers,
                OverdriveSpeedMultiplier = _overdriveAbility != null ? _overdriveAbility.SpeedMultiplier : 1f,
                IsInOverdrive = _overdriveAbility != null && _overdriveAbility.IsInOverdrive,
                IsGrappling = _grapple != null && _grapple.IsGrappling,
                GrappleVelocity = _grapple != null ? _grapple.ReelVelocity : Vector3.zero,
                GrappleExitImpulse = _grapple != null ? _grapple.ExitImpulse : Vector3.zero,
            };
            // preserve old behavior formerly in GetKnockbackVertical
            _trainPassengerPhysics.SimulateClearKnockbackVertical();

            var ctx = new PlayerSimulationContext(
                input,
                deps,
                _config,
                _characterController,
                delta);

            PlayerSimulation.Step(ctx, ref state);

            // Body yaw is owned by PredictedTransform: set it here in simulation so it is
            // captured (GetUnityState), replicated, and interpolated for remote players.
            transform.rotation = Quaternion.Euler(0f, state.CameraYaw, 0f);

            _playerState.SetSimulatedPlayerMovementState(state.SimulatedMovementStateResult);
        }

        /// <summary>
        /// Repositions the player during simulation. Must run inside the prediction loop so the
        /// sibling PredictedTransform (which owns position+rotation) captures the new transform.
        /// </summary>
        [SimulationOnly]
        public void SimulatePlaceAtRespawnPoint(Vector3 position, Quaternion rotation)
        {
            // CameraYaw is the source of truth for the owner camera, movement direction, and
            // the body yaw written in Simulate, so seed it from the spawn rotation's yaw.
            currentState.CameraYaw = rotation.eulerAngles.y;

            // Position+rotation are owned by PredictedTransform. Reposition the transform with
            // the CharacterController disabled so it doesn't fight the teleport; PredictedTransform
            // captures the new transform after simulation this tick.
            bool wasEnabled = _characterController.enabled;
            _characterController.enabled = false;
            transform.SetPositionAndRotation(position, rotation);
            _characterController.enabled = wasEnabled;

            // Snap the interpolation so the large respawn jump isn't smeared from the old location.
            _predictedTransform?.ResetInterpolation();
        }

        protected override PlayerMovementDataState Interpolate(
            PlayerMovementDataState from,
            PlayerMovementDataState to,
            float t)
        {
            return new PlayerMovementDataState
            {
                Velocity = Vector3.Lerp(from.Velocity, to.Velocity, t),
                CameraYaw = Mathf.LerpAngle(from.CameraYaw, to.CameraYaw, t),
                GrappleImpulse = Vector3.Lerp(from.GrappleImpulse, to.GrappleImpulse, t),
                // Discrete fields snap to `to`.
                JumpedLastSimulatedFrame = to.JumpedLastSimulatedFrame,
                WasGroundedLastTick = to.WasGroundedLastTick,
                SimulatedMovementStateResult = to.SimulatedMovementStateResult,
                SlideTimer = Mathf.Lerp(from.SlideTimer, to.SlideTimer, t),
            };
        }

        protected override void UpdateView(PlayerMovementDataState viewState, PlayerMovementDataState? verified)
        {
            if (!isOwner || IsPlayerDead) return;
            if (_virtualCamera == null || _playerLocomotionInput == null) return;

            _virtualCamera.transform.rotation = Quaternion.Euler(_cameraPitch, _cameraYaw, 0f);

            // Drive the first-person view root (camera + FP arms) from the interpolated graphics
            // position instead of the raw simulated root, so local movement reads as smooth. The
            // captured offset is reapplied fresh each frame so it never compounds.
            if (_hasFirstPersonViewRoot && _predictedTransform != null && _predictedTransform.graphics != null)
            {
                _firstPersonViewRoot.position =
                    _predictedTransform.graphics.position + transform.rotation * _firstPersonViewRootOffsetFromRoot;
            }

            // Body yaw is driven in Simulate and owned by PredictedTransform; UpdateView only
            // handles the owner camera/FOV (kept crisp on the simulated root).
            Vector3 camForwardXZ = new Vector3(_virtualCamera.transform.forward.x, 0f, _virtualCamera.transform.forward.z).normalized;
            Vector3 cross = Vector3.Cross(transform.forward, camForwardXZ);
            RotationMismatch = Mathf.Sign(Vector3.Dot(cross, transform.up)) * Vector3.Angle(transform.forward, camForwardXZ);

            float targetFOV = _config.baseFOV;
            if (_overdriveAbility != null && _overdriveAbility.IsInOverdrive)
                targetFOV = _config.overdriveFOV;
            else if (viewState.SimulatedMovementStateResult == PlayerMovementState.Sprinting)
                targetFOV = _config.sprintFOV;
            _virtualCamera.Lens.FieldOfView = Mathf.Lerp(_virtualCamera.Lens.FieldOfView, targetFOV, _config.fovTransitionSpeed * Time.deltaTime);
        }

        #endregion
    }
}
