using PurrNet;
using Resonance.Assemblies.Player;
using Resonance.Audio;
using Resonance.Player;
using Resonance.PlayerController;
using UnityEngine;

namespace Resonance.Combat.Augments
{
    public class AbilityGrappleHook : NetworkBehaviour, IAugmentAbility
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

#if !UNITY_SERVER
        [Header("Wwise Events")]
        [SerializeField] private AK.Wwise.Event shootEvent;
        [SerializeField] private AK.Wwise.Event travelLoopEvent;
        [SerializeField] private AK.Wwise.Event stopTravelEvent;
        [SerializeField] private AK.Wwise.Event releaseEvent;
#endif

        private PlayerLocomotionInput playerLocomotionInput;
        private PlayerState playerState;
        private PlayerController.PlayerController playerController;
        private CharacterController characterController;
        private Camera playerCamera;
        private GrappleRopeRenderer ropeRenderer;
        private FPArmsAnimator fpArmsAnimator;

        private float currentReelTime;
        private float currentCooldown;

        public string AbilityKey => "ability_grappleHook";
        public string Name => "Grapple Hook";
        public string Description => "Fire a hook to pull yourself to a target point.";
        public float MaxCooldown => cooldown;
        public float CurrentCooldown
        {
            get => currentCooldown;
            set => currentCooldown = Mathf.Clamp(value, 0f, cooldown);
        }
        public bool AbilityReady => currentCooldown <= 0f && !ropeRenderer.IsGrappling.value;

        public void ActivateAbility()
        {
            if (!isOwner) return;
            if (!AbilityReady) return;

            Ray ray = new Ray(playerCamera.transform.position, playerCamera.transform.forward);

            if (!Physics.Raycast(ray, out RaycastHit hit, maxRange, grappleLayerMask))
            {
                fpArmsAnimator?.TriggerGrappleEnd();
                return;
            }

            ropeRenderer.HookPoint.value = hit.point;
            ropeRenderer.IsGrappling.value = true;
            currentReelTime = 0f;

            playerState.SetPlayerMovementState(PlayerMovementState.Grappling);

            BroadcastShootAndTravelRpc();
            RequestGrappleRegistrationOnServer(transform.position);
        }

        private void Awake()
        {
            playerLocomotionInput = GetComponent<PlayerLocomotionInput>();
            playerState = GetComponent<PlayerState>();
            playerController = GetComponent<PlayerController.PlayerController>();
            characterController = GetComponent<CharacterController>();
            ropeRenderer = GetComponent<GrappleRopeRenderer>();
            fpArmsAnimator = GetComponent<FPArmsAnimator>();
        }

        protected override void OnSpawned()
        {
            base.OnSpawned();

            if (isOwner)
                playerCamera = Camera.main;
        }

        private void Update()
        {
            if (!isOwner)
                return;

            if (currentCooldown > 0f)
                currentCooldown -= Time.deltaTime;

            if (!ropeRenderer.IsGrappling.value)
                return;

            currentReelTime += Time.deltaTime;

            Vector3 directionToHook = ropeRenderer.HookPoint.value - transform.position;
            float distanceToHook = directionToHook.magnitude;

            if (playerLocomotionInput.JumpPressed)
            {
                ExitGrapple(earlyExit: true);
                return;
            }

            if (currentReelTime >= maxReelTime)
            {
                ExitGrapple(earlyExit: false);
                return;
            }

            if (distanceToHook < 0.5f)
            {
                ExitGrapple(earlyExit: false);
                return;
            }

            Vector3 moveDirection = directionToHook.normalized * reelSpeed * Time.deltaTime;
            characterController.Move(moveDirection);
        }

        private void OnDisable()
        {
            if (ropeRenderer.IsGrappling.value && isOwner)
                ExitGrapple(earlyExit: false);
        }

        private void ExitGrapple(bool earlyExit)
        {
            ropeRenderer.IsGrappling.value = false;
            currentCooldown = cooldown;

            Vector3 pullDirection = (ropeRenderer.HookPoint.value - transform.position).normalized;
            Vector3 exitDirection = Vector3.Lerp(pullDirection, Vector3.up, upwardBias).normalized;

            if (earlyExit)
                playerController.ApplyImpulse(exitDirection * (reelSpeed + exitBoost));

            playerState.SetPlayerMovementState(PlayerMovementState.Falling);

            fpArmsAnimator?.TriggerGrappleEnd();
            BroadcastStopTravelRpc();
            BroadcastReleaseRpc();
        }
        
        public bool CanGrapple()
        {
            if (playerCamera == null) return false;
            Ray ray = new Ray(playerCamera.transform.position, playerCamera.transform.forward);
            return Physics.Raycast(ray, maxRange, grappleLayerMask);
        }

        #region Audio RPCs

        [ObserversRpc(runLocally: true)]
        private void BroadcastShootAndTravelRpc()
        {
#if !UNITY_SERVER
            if (shootEvent != null && shootEvent.IsValid())
                shootEvent.Post(gameObject);

            if (travelLoopEvent != null && travelLoopEvent.IsValid())
                travelLoopEvent.Post(gameObject);
#endif
        }

        [ServerRpc]
        private void RequestGrappleRegistrationOnServer(Vector3 position)
        {
            BroadcastGrappleRegistration(position);
        }

        [ObserversRpc(runLocally: true)]
        private void BroadcastGrappleRegistration(Vector3 position)
        {
#if !UNITY_SERVER
            if (AudioSourceTracker.Instance != null)
                AudioSourceTracker.Instance.RegisterSound(position, 1f);
#endif
        }

        [ObserversRpc(runLocally: true)]
        private void BroadcastStopTravelRpc()
        {
#if !UNITY_SERVER
            if (stopTravelEvent != null && stopTravelEvent.IsValid())
                stopTravelEvent.Post(gameObject);
#endif
        }

        [ObserversRpc(runLocally: true)]
        private void BroadcastReleaseRpc()
        {
#if !UNITY_SERVER
            if (releaseEvent != null && releaseEvent.IsValid())
                releaseEvent.Post(gameObject);
#endif
        }

        #endregion
    }
}
