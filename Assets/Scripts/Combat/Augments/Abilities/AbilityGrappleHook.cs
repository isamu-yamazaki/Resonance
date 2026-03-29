using PurrNet;
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
        [SerializeField] private GameObject ropeRendererPrefab;

        private PlayerLocomotionInput playerLocomotionInput;
        private PlayerState playerState;
        private PlayerController.PlayerController playerController;
        private CharacterController characterController;
        private LineRenderer lineRenderer;
        private Camera playerCamera;

        private SyncVar<Vector3> hookPoint = new SyncVar<Vector3>();
        private SyncVar<bool> isGrappling = new SyncVar<bool>();

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
        public bool AbilityReady => currentCooldown <= 0f && !isGrappling.value;

        public void ActivateAbility()
        {
            if (!AbilityReady)
            {
                return;
            }

            Ray ray = new Ray(playerCamera.transform.position, playerCamera.transform.forward);

            if (!Physics.Raycast(ray, out RaycastHit hit, maxRange, grappleLayerMask))
            {
                return;
            }

            hookPoint.value = hit.point;
            isGrappling.value = true;
            currentReelTime = 0f;

            playerState.SetPlayerMovementState(PlayerMovementState.Grappling);
        }

        private void Awake()
        {
            playerLocomotionInput = GetComponent<PlayerLocomotionInput>();
            playerState = GetComponent<PlayerState>();
            playerController = GetComponent<PlayerController.PlayerController>();
            characterController = GetComponent<CharacterController>();

            if (ropeRendererPrefab != null)
            {
                GameObject ropeInstance = Instantiate(ropeRendererPrefab, transform);
                lineRenderer = ropeInstance.GetComponent<LineRenderer>();
                lineRenderer.positionCount = 2;
                lineRenderer.enabled = false;
            }
        }

        protected override void OnSpawned()
        {
            base.OnSpawned();

            if (isOwner)
            {
                playerCamera = Camera.main;
            }
        }

        private void Update()
        {
            if (lineRenderer != null)
            {
                lineRenderer.enabled = isGrappling.value;

                if (isGrappling.value)
                {
                    lineRenderer.SetPosition(0, transform.position);
                    lineRenderer.SetPosition(1, hookPoint.value);
                }
            }

            if (!isOwner)
            {
                return;
            }

            if (currentCooldown > 0f)
            {
                currentCooldown -= Time.deltaTime;
            }

            if (!isGrappling.value)
            {
                return;
            }

            currentReelTime += Time.deltaTime;

            Vector3 directionToHook = hookPoint.value - transform.position;
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
            if (isGrappling.value && isOwner)
            {
                ExitGrapple(earlyExit: false);
            }
        }

        private void ExitGrapple(bool earlyExit)
        {
            isGrappling.value = false;
            currentCooldown = cooldown;

            Vector3 pullDirection = (hookPoint.value - transform.position).normalized;
            Vector3 exitDirection = Vector3.Lerp(pullDirection, Vector3.up, upwardBias).normalized;

            if (earlyExit)
            {
                playerController.ApplyImpulse(exitDirection * (reelSpeed + exitBoost));
            }

            playerState.SetPlayerMovementState(PlayerMovementState.Falling);
        }
    }
}