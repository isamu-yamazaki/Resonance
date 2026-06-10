using PurrNet.Prediction;
using Resonance.PlayerController;
using UnityEngine;

namespace Resonance.Combat.Augments
{
    /// <summary>
    /// Predicted grapple hook ability. The reel motion is computed deterministically each tick in
    /// <see cref="Simulate"/> and exposed via <see cref="ReelVelocity"/> / <see cref="ExitImpulse"/>,
    /// which PlayerPredictedController reads into PlayerDependencyData so the actual movement is applied
    /// inside PlayerSimulation (on both server and owner client). This component no longer touches the
    /// CharacterController directly.
    /// </summary>
    public class AbilityGrappleHook
        : PredictedIdentity<AbilityGrappleHookInput, AbilityGrappleHookState>, IAugmentAbility
    {
        [Header("Grapple Settings")]
        [SerializeField] private float maxRange = 30f;
        [SerializeField] private float reelSpeed = 20f;
        [SerializeField] private float maxReelTime = 3f;
        [SerializeField] private float exitBoost = 5f;
        [SerializeField] private float upwardBias = 0.25f;
        [SerializeField] private float cooldown = 10f;

        [Header("References")]
        [SerializeField] private LayerMask grappleLayerMask;

        private PlayerLocomotionInput playerLocomotionInput;
        private Camera playerCamera;
        private GrappleRopeRenderer ropeRenderer;
        private FPArmsAnimator fpArmsAnimator;
        private AbilityGrappleHookAudioBroadcast _audioBroadcast;

        private float currentCooldown;

        // Owner-only input accumulators, flushed in GetFinalInput.
        private bool _pendingActivate;
        private Vector3 _pendingHookPoint;

        // Owner-only view bookkeeping for detecting the grapple-end transition.
        private bool _wasGrappling;

        #region IAugmentAbility

        public string AbilityKey => "ability_grappleHook";
        public string Name => "Grapple Hook";
        public string Description => "Fire a hook to pull yourself to a target point.";
        public float MaxCooldown => cooldown;

        public float CurrentCooldown => currentState.Cooldown;

        public bool AbilityReady => CurrentCooldown <= 0f && !currentState.IsGrappling;

        /// <summary>
        /// Invoked externally by FPArmsAnimator.OnGrappleFireHook once the holster animation reaches
        /// the fire-hook event. Performs the camera raycast and, on a hit, queues a predicted activation
        /// request.
        /// </summary>
        public void ActivateAbilityExternal()
        {
            if (!AbilityReady) return;
            if (playerCamera == null) return;

            Ray ray = new Ray(playerCamera.transform.position, playerCamera.transform.forward);

            if (!Physics.Raycast(ray, out RaycastHit hit, maxRange, grappleLayerMask))
            {
                fpArmsAnimator?.TriggerGrappleEnd();
                return;
            }

            _pendingHookPoint = hit.point;
            _pendingActivate = true;

            _audioBroadcast.RequestExternalBroadcastShootAndTravel();
            _audioBroadcast.RequestExternalBroadcastGrappleRegistration(transform.position);
        }

        [SimulationOnly]
        public void SimulateActivateAbility()
        {
            // modify the current state in-place instead of going through the input cycle
            
            if (!AbilityReady) return;
            Ray ray = new Ray(currentState.CameraPosition, currentState.CameraForward);
            if (!Physics.Raycast(ray, out RaycastHit hit, maxRange, grappleLayerMask))
            {
                fpArmsAnimator?.TriggerGrappleEnd();
                return;
            }

            currentState.IsGrappling = true;
            currentState.HookPoint = hit.point;
            currentState.ReelTime = 0f;
        }

        public bool CanGrapple()
        {
            if (playerCamera == null) return false;
            Ray ray = new Ray(playerCamera.transform.position, playerCamera.transform.forward);
            return Physics.Raycast(ray, maxRange, grappleLayerMask);
        }

        #endregion

        #region Exposed state

        public bool IsGrappling => currentState.IsGrappling;
        public Vector3 ReelVelocity => currentState.ReelVelocity;
        public Vector3 ExitImpulse => currentState.ExitImpulse;

        #endregion

        #region Lifecycle

        protected override void LateAwake()
        {
            playerLocomotionInput = PlayerLocomotionInput.Instance;
            ropeRenderer = GetComponent<GrappleRopeRenderer>();
            fpArmsAnimator = GetComponent<FPArmsAnimator>();
            _audioBroadcast = GetComponent<AbilityGrappleHookAudioBroadcast>();
            
            if (isOwner)
                playerCamera = Camera.main;
        }

        #endregion

        #region Simulation loop

        protected override void GetFinalInput(ref AbilityGrappleHookInput input)
        {
            if (!isOwner) return;

            input.ActivatePressed = _pendingActivate;
            input.HookPoint = _pendingHookPoint;
            input.JumpPressed = playerLocomotionInput != null && playerLocomotionInput.JumpPressed;

            if (playerCamera != null)
            {
                input.CameraPosition = playerCamera.transform.position;
                input.CameraForward = playerCamera.transform.forward;
            }

            _pendingActivate = false;
        }

        protected override void Simulate(AbilityGrappleHookInput input, ref AbilityGrappleHookState state, float delta)
        {
            if (state.Cooldown > 0)
                state.Cooldown -= delta;
            
            // Per-tick outputs are consumed each tick, never accumulated.
            state.ReelVelocity = Vector3.zero;
            state.ExitImpulse = Vector3.zero;

            // Mirror the owner camera pose into state so SimulationOnly code (SimulateActivateAbility)
            // can read it outside the input frame.
            state.CameraPosition = input.CameraPosition;
            state.CameraForward = input.CameraForward;

            if (input.ActivatePressed && !state.IsGrappling)
            {
                state.IsGrappling = true;
                state.HookPoint = input.HookPoint;
                state.ReelTime = 0f;
            }

            if (!state.IsGrappling)
                return;

            state.ReelTime += delta;

            Vector3 directionToHook = state.HookPoint - transform.position;
            float distanceToHook = directionToHook.magnitude;
            

            // Start the cooldown after the grappling hook ends
            if (input.JumpPressed)
            {
                ExitGrapple(ref state, directionToHook, earlyExit: true);
                state.Cooldown = cooldown;
                return;
            }

            if (state.ReelTime >= maxReelTime || distanceToHook < 0.5f)
            {
                ExitGrapple(ref state, directionToHook, earlyExit: false);
                state.Cooldown = cooldown;
                return;
            }

            state.ReelVelocity = directionToHook.normalized * reelSpeed;

        }

        private void ExitGrapple(ref AbilityGrappleHookState state, Vector3 directionToHook, bool earlyExit)
        {
            state.IsGrappling = false;

            if (earlyExit)
            {
                Vector3 pullDirection = directionToHook.normalized;
                Vector3 exitDirection = Vector3.Lerp(pullDirection, Vector3.up, upwardBias).normalized;
                state.ExitImpulse = exitDirection * (reelSpeed + exitBoost);
            }
        }

        protected override void UpdateView(AbilityGrappleHookState viewState, AbilityGrappleHookState? verified)
        {
            if (!isOwner) return;

            if (!verified.HasValue) return;
            var v = verified.Value;

            // Drive the rope renderer's owner-authority SyncVars so the rope replicates to all clients.
            if (ropeRenderer != null)
            {
                ropeRenderer.IsGrappling.value = v.IsGrappling;
                if (v.IsGrappling)
                    ropeRenderer.HookPoint.value = v.HookPoint;
            }

            // Detect the grapple-end transition to start the cooldown and fire end-of-grapple feedback.
            if (_wasGrappling && !v.IsGrappling)
            {
                fpArmsAnimator?.TriggerGrappleEnd();
                _audioBroadcast.RequestExternalBroadcastStopTravel();
                _audioBroadcast.RequestExternalBroadcastRelease();
            }

            _wasGrappling = v.IsGrappling;
        }

        #endregion
    }

    public struct AbilityGrappleHookInput : IPredictedData
    {
        public bool ActivatePressed;
        public Vector3 HookPoint;
        public bool JumpPressed;

        /// <summary>Owner camera pose this tick, forwarded into state so SimulationOnly code can read it.</summary>
        public Vector3 CameraPosition;
        public Vector3 CameraForward;

        public void Dispose() { }
    }

    public struct AbilityGrappleHookState : IPredictedData<AbilityGrappleHookState>
    {
        public bool IsGrappling;
        public Vector3 HookPoint;
        public float ReelTime;

        /// <summary>Reel velocity computed this tick (zero when not grappling).</summary>
        public Vector3 ReelVelocity;

        /// <summary>Exit boost emitted on the tick an early exit occurs (zero otherwise).</summary>
        public Vector3 ExitImpulse;

        /// <summary>Latest owner camera pose, mirrored from input each tick so SimulationOnly code can read it.</summary>
        public Vector3 CameraPosition;
        public Vector3 CameraForward;

        public float Cooldown;

        public void Dispose() { }
    }
}
