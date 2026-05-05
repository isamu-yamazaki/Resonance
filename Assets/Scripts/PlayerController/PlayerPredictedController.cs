using PurrNet.Prediction;
using Resonance.Assemblies.Player;
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
    ///
    /// This is scaffolding for the migration off the legacy PlayerController. Several
    /// pieces of legacy behavior (movement-state machine, camera rotation, slide init,
    /// rotate-to-target) are not yet ported and are flagged with TODOs below.
    /// </summary>
    [DefaultExecutionOrder(-1)]
    [RequireComponent(typeof(CharacterController))]
    public class PlayerPredictedController : PredictedIdentity<PlayerInputData, PlayerMovementDataState>
    {
        #region Inspector

        [Header("Components")]
        [SerializeField] private CharacterController _characterController;
        [SerializeField] private CinemachineCamera _virtualCamera;

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
        private float _stepOffset;

        #endregion

        #region Public API (mirrors PlayerController)

        public static PlayerPredictedController LocalPlayer { get; private set; }

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

            _trainPassengerPhysics?.ClearInertia();
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

            _stepOffset = _characterController != null ? _characterController.stepOffset : 0f;

            if (_virtualCamera != null)
                _virtualCamera.Lens.FieldOfView = _config.baseFOV;

            if (isOwner)
                LocalPlayer = this;

            if (_virtualCamera != null)
                _virtualCamera.gameObject.SetActive(isOwner);
        }

        #endregion

        #region Prediction overrides

        protected override PlayerMovementDataState GetInitialState()
        {
            return new PlayerMovementDataState
            {
                Position = transform.position,
                Velocity = Vector3.zero,
                CameraYaw = transform.eulerAngles.y,
                GrappleImpulse = Vector3.zero,
                JumpedLastSimulatedFrame = false,
                LastSimulatedMovementState = PlayerMovementState.Falling,
                SlideTimer = 0f,
            };
        }

        protected override void GetUnityState(ref PlayerMovementDataState state)
        {
            state.Position = transform.position;
        }

        protected override void SetUnityState(PlayerMovementDataState state)
        {
            // CharacterController patch: disable around teleport so it doesn't snap-back.
            bool wasEnabled = _characterController.enabled;
            _characterController.enabled = false;
            transform.position = state.Position;
            _characterController.enabled = wasEnabled;
        }

        protected override void UpdateInput(ref PlayerInputData input)
        {
            input.JumpPressed = _playerLocomotionInput.JumpPressed;
            input.SprintToggledOn = _playerLocomotionInput.SprintToggledOn;
            input.CrouchToggledOn = _playerLocomotionInput.CrouchToggledOn;
        }

        protected override void GetFinalInput(ref PlayerInputData input)
        {
            input.MovementInput = _playerLocomotionInput.MovementInput;
            input.LookInput = _playerLocomotionInput.LookInput;
        }


        protected override void Simulate(PlayerInputData input, ref PlayerMovementDataState state, float delta)
        {
            // Bail-outs ported from PlayerController.Update.
            if (IsPlayerDead) return;
            if (_playerState.IsDead()) return;
            if (_playerState.IsMatchFrozen()) return;
            if (_playerState.IsZiplining() || _playerState.IsGrappling()) return;
            if (!_characterController.enabled) return;

            // PlayerState/PlayerStats are NetworkBehaviours backed by ValidatedSyncVar.
            // They aren't part of the predicted state, so during rollback/replay they
            // hold the currently-synced value — not the value at that historical tick.
            // TODO: push this snapshot into the predicted state, or run the legacy
            // UpdateMovementState transitions inside Simulate so the state machine is
            // also predicted.
            var deps = new PlayerDependencyData
            {
                MovementSpeedMultiplier = _playerStats.PlayerSpeed,
                CurrentPlayerMovementState = _playerState.CurrentPlayerMovementState,
                trainVelocityOffset = _trainPassengerPhysics != null
                    ? _trainPassengerPhysics.GetFrameVelocityOffset()
                    : Vector3.zero,
                trainKnockbackVertical = _trainPassengerPhysics != null
                    ? _trainPassengerPhysics.GetKnockbackVertical()
                    : 0f,
                groundLayers = _groundLayers,
                OverdriveSpeedMultiplier = _overdriveAbility != null ? _overdriveAbility.SpeedMultiplier : 1f,
                IsInOverdrive = _overdriveAbility != null && _overdriveAbility.IsInOverdrive,
            };

            var ctx = new PlayerSimulationContext(
                input,
                deps,
                _config,
                _characterController,
                delta);

            PlayerSimulation.Step(ctx, ref state);
        }

        protected override PlayerMovementDataState Interpolate(
            PlayerMovementDataState from,
            PlayerMovementDataState to,
            float t)
        {
            return new PlayerMovementDataState
            {
                Position = Vector3.Lerp(from.Position, to.Position, t),
                Velocity = Vector3.Lerp(from.Velocity, to.Velocity, t),
                CameraYaw = Mathf.LerpAngle(from.CameraYaw, to.CameraYaw, t),
                GrappleImpulse = Vector3.Lerp(from.GrappleImpulse, to.GrappleImpulse, t),
                // Discrete fields snap to `to`.
                JumpedLastSimulatedFrame = to.JumpedLastSimulatedFrame,
                LastSimulatedMovementState = to.LastSimulatedMovementState,
                SlideTimer = Mathf.Lerp(from.SlideTimer, to.SlideTimer, t),
            };
        }

        protected override void UpdateView(PlayerMovementDataState viewState, PlayerMovementDataState? verified)
        {
            // TODO: port PlayerController.LateUpdate logic — camera rotation from CameraYaw,
            // FOV lerp (sprint/overdrive), rotate-player-to-target, RotationMismatch tracking.
            // Transform position is already kept in sync by CharacterController.Move during
            // Simulate; SetUnityState handles rollback restoration.
        }

        #endregion
    }
}
