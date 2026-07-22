using PurrNet.Prediction;
using Resonance.Assemblies.AbilitySimulation.GrappleHook;
using Resonance.Audio;
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

#if !UNITY_SERVER
        [Header("Wwise Events")] [SerializeField] private AK.Wwise.Event shootEvent;
        [SerializeField] private AK.Wwise.Event travelLoopEvent;
        [SerializeField] private AK.Wwise.Event stopTravelEvent;
        [SerializeField] private AK.Wwise.Event releaseEvent;
#endif

        private PlayerLocomotionInput playerLocomotionInput;
        private Camera playerCamera;
        private GrappleRopeRenderer ropeRenderer;
        private FPArmsAnimator fpArmsAnimator;

        // Owner-only view bookkeeping for detecting the grapple-end transition.
        private bool _wasGrappling;

        // Previous verified state, so the one-shot broadcast flags can be edge-detected instead of
        // re-firing every render frame that resamples the same verified tick.
        private AbilityGrappleHookState? _previousVerifiedState;

        #region IAugmentAbility

        public string AbilityKey => "ability_grappleHook";
        public string Name => "Grapple Hook";
        public string Description => "Fire a hook to pull yourself to a target point.";
        public float MaxCooldown => config.cooldown;

        public float CurrentCooldown => currentState.Cooldown;

        public bool AbilityReady => CurrentCooldown <= 0f && !currentState.IsGrappling;

        [SimulationOnly]
        public void SimulateActivateAbility()
        {
            // Request activation for this system's own next Simulate call, rather than mutating
            // currentState directly here. Keep the ordering consistent.
            currentState.GrappleNextTick = true;
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

            if (isOwner)
                playerCamera = Camera.main;
        }

        #endregion

        #region Simulation loop

        protected override void GetFinalInput(ref AbilityGrappleHookInput input)
        {
            if (!isOwner) return;

            input.JumpPressed = playerLocomotionInput != null && playerLocomotionInput.JumpPressed;

            if (playerCamera != null)
            {
                input.CameraPosition = playerCamera.transform.position;
                input.CameraForward = playerCamera.transform.forward;
            }

            input.LocalTransformPosition = transform.position;
        }

        protected override void Simulate(AbilityGrappleHookInput input, ref AbilityGrappleHookState state, float delta)
        {
            var ctx = new GrappleHookSimulationContext(
                input, config, delta);
            GrappleHookSimulation.Step(ctx, ref state);
        }
        #endregion

        #region Local view updates
        protected override void UpdateView(AbilityGrappleHookState viewState, AbilityGrappleHookState? verified)
        {
            if (!verified.HasValue) return;
            var v = verified.Value;

            var previous = _previousVerifiedState;

#if !UNITY_SERVER
            if (v.BroadcastShootAndTravel && !(previous?.BroadcastShootAndTravel ?? false))
            {
                if (shootEvent != null && shootEvent.IsValid())
                    shootEvent.Post(gameObject);

                if (travelLoopEvent != null && travelLoopEvent.IsValid())
                    travelLoopEvent.Post(gameObject);
            }

            if (v.BroadcastGrappleRegistration && !(previous?.BroadcastGrappleRegistration ?? false))
            {
                if (AudioSourceTracker.Instance != null)
                    AudioSourceTracker.Instance.RegisterSound(v.GrappleRegistrationPosition, 1f);
            }

            if (v.BroadcastStopTravel && !(previous?.BroadcastStopTravel ?? false))
            {
                if (stopTravelEvent != null && stopTravelEvent.IsValid())
                    stopTravelEvent.Post(gameObject);
            }

            if (v.BroadcastRelease && !(previous?.BroadcastRelease ?? false))
            {
                if (releaseEvent != null && releaseEvent.IsValid())
                    releaseEvent.Post(gameObject);
            }
#endif

            _wasGrappling = v.IsGrappling;
            _previousVerifiedState = v;
            if (!isOwner) return;

            // Drive the rope renderer's owner-authority SyncVars so the rope replicates to all clients.
            if (ropeRenderer != null)
            {
                ropeRenderer.IsGrappling.value = v.IsGrappling;
                if (v.IsGrappling)
                    ropeRenderer.HookPoint.value = v.HookPoint;
            }

            // Detect the grapple-end transition to fire end-of-grapple arm animation feedback.
            if (_wasGrappling && !v.IsGrappling)
                fpArmsAnimator?.TriggerGrappleEnd();

        }
        #endregion

    }



}
