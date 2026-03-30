using PurrNet;
using Resonance.PlayerController;
using UnityEngine;

namespace Resonance.Combat.Augments
{
    public class AbilityActiveCamo : NetworkBehaviour, IAugmentAbility
    {
        [Header("Camo Settings")]
        [SerializeField] private float maxMeter = 45f;
        [SerializeField] private float meterRecoveryRate = 1f;
        [SerializeField] private float meterDrainPerSpeed = 0.5f;
        [SerializeField] private float fireDrainAmount = 5f;
        [SerializeField] private float maxDuration = 20f;
        [SerializeField] private float rechargeLockout = 3f;
        
        private float minMeterToActivate;

        private PlayerLocomotionInput playerLocomotionInput;
        private PlayerActionsInput playerActionsInput;
        private CharacterController characterController;
        private PlayerCamoRenderer camoRenderer;
        private PlayerShooter playerShooter;

        private SyncVar<bool> isCamoActive = new SyncVar<bool>(default, 0f, ownerAuth: true);

        private float currentMeter;
        private float currentDuration;
        private float rechargeLockoutTimer;
        private bool isInLockout;

        public string AbilityKey => "ability_activeCamo";
        public string Name => "Active Camo";
        public string Description => "Blend into your surroundings.";
        public float MaxCooldown => maxMeter;
        public float CurrentCooldown
        {
            get => currentMeter;
            set => currentMeter = Mathf.Clamp(value, 0f, maxMeter);
        }
        public bool AbilityReady => !isCamoActive.value && !isInLockout && currentMeter >= minMeterToActivate;

        public void ActivateAbility()
        {
            if (!AbilityReady)
            {
                return;
            }

            isCamoActive.value = true;
            currentDuration = 0f;
        }

        private void Awake()
        {
            playerLocomotionInput = GetComponent<PlayerLocomotionInput>();
            playerActionsInput = GetComponent<PlayerActionsInput>();
            characterController = GetComponent<CharacterController>();
            camoRenderer = GetComponent<PlayerCamoRenderer>();
            playerShooter = GetComponent<PlayerShooter>();

            playerShooter.OnShotFired += OnShotFired;
            currentMeter = maxMeter;
            isInLockout = false;
            rechargeLockoutTimer = 0f;
            minMeterToActivate = maxMeter / 4;
        }

        protected override void OnDestroy()
        {
            playerShooter.OnShotFired -= OnShotFired;
        }

        protected override void OnSpawned()
        {
            base.OnSpawned();
            isCamoActive.onChanged += OnCamoStateChanged;
        }

        protected override void OnDespawned()
        {
            base.OnDespawned();
            isCamoActive.onChanged -= OnCamoStateChanged;
        }

        private void OnCamoStateChanged(bool active)
        {
            camoRenderer.CamoMaterialActive.value = active;
        }

        private void Update()
        {
            if (!isOwner)
            {
                return;
            }

            if (isInLockout)
            {
                rechargeLockoutTimer -= Time.deltaTime;
                if (rechargeLockoutTimer <= 0f)
                {
                    isInLockout = false;
                }
            }

            if (isCamoActive.value)
            {
                UpdateActiveCamo();
            }
            else
            {
                RecoverMeter();
            }
        }

        private void UpdateActiveCamo()
        {
            currentDuration += Time.deltaTime;

            float speed = new Vector3(characterController.velocity.x, 0f, characterController.velocity.z).magnitude;
            float drainThisFrame = speed * meterDrainPerSpeed * Time.deltaTime;
            currentMeter = Mathf.Clamp(currentMeter - drainThisFrame, 0f, maxMeter);

            if (currentMeter <= 0f || currentDuration >= maxDuration)
            {
                DeactivateCamo();
                return;
            }

            if (playerActionsInput.AbilityLowerPressed)
            {
                playerActionsInput.SetAbilityLowerPressedFalse();
                DeactivateCamo();
            }
        }

        private void RecoverMeter()
        {
            currentMeter = Mathf.Clamp(currentMeter + meterRecoveryRate * Time.deltaTime, 0f, maxMeter);
        }

        private void DeactivateCamo()
        {
            isCamoActive.value = false;
            isInLockout = true;
            rechargeLockoutTimer = rechargeLockout;
        }

        private void OnDisable()
        {
            if (isCamoActive.value && isOwner)
            {
                DeactivateCamo();
            }
        }

        private void OnShotFired()
        {
            if (!isCamoActive.value) return;
            currentMeter = Mathf.Clamp(currentMeter - fireDrainAmount, 0f, maxMeter);
        }
    }
}