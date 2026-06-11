using PurrNet.Prediction;
using Resonance.Assemblies.AbilitySimulation.GrappleHook;
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
        [Header("Config")] [SerializeField] private GrappleHookConfig config;
        private LayerMask grappleLayerMask => config.grappleLayerMask;

        private PlayerLocomotionInput playerLocomotionInput;
        private Camera playerCamera;
        private GrappleRopeRenderer ropeRenderer;
        private FPArmsAnimator fpArmsAnimator;
        private AbilityGrappleHookAudioBroadcast _audioBroadcast;

        // Owner-only input accumulators, flushed in GetFinalInput.
        private bool _pendingActivate;
        private Vector3 _pendingHookPoint;

        // Owner-only view bookkeeping for detecting the grapple-end transition.
        private bool _wasGrappling;

        #region IAugmentAbility

        public string AbilityKey => "ability_grappleHook";
        public string Name => "Grapple Hook";
        public string Description => "Fire a hook to pull yourself to a target point.";
        public float MaxCooldown => config.cooldown;

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

            if (!Physics.Raycast(ray, out RaycastHit hit, config.maxRange, grappleLayerMask))
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
            if (!Physics.Raycast(ray, out RaycastHit hit, config.maxRange, grappleLayerMask))
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
            return Physics.Raycast(ray, config.maxRange, grappleLayerMask);
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

            input.LocalTransformPosition = transform.position;

            _pendingActivate = false;
        }

        protected override void Simulate(AbilityGrappleHookInput input, ref AbilityGrappleHookState state, float delta)
        {
            var ctx = new GrappleHookSimulationContext(
                input, config, transform.position, delta);
            GrappleHookSimulation.Step(ctx, ref state);
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



}
