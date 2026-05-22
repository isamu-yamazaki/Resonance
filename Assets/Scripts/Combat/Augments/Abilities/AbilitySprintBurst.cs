using System;
using Resonance.Player;
using Resonance.PlayerController;
using UnityEngine;

namespace Resonance.Combat.Augments
{
    public class AbilitySprintBurst : MonoBehaviour, IAugmentAbility
    {
        [SerializeField] private float maxBurstSpeed = 2f;
        [SerializeField] private float minBurstSpeed = 1.2f;
        [SerializeField] private float maxMeter = 5f;
        [SerializeField] private float meterRecoverySpeed = 1f;
        [SerializeField] private float timeUntilRecovery = 0.5f;

        private PlayerLocomotionInput playerLocomotionInput;
        private PlayerStats playerStats;

        private float currentMeter;
        private float lastAppliedSpeedMod;
        private float timeSinceLastSprinting;
        private bool wasSprinting;


        public string AbilityKey => "ability_sprintBurst";
        public string Name => "Sprint Burst";
        public string Description => "Move with a brief burst of speed.";
        public float MaxCooldown => maxMeter;
        public float CurrentCooldown
        {
            get => currentMeter;
            set => currentMeter = Mathf.Clamp(value, 0f, maxMeter);
        }
        public bool AbilityReady => false;

        public void ActivateAbilityExternal() { }
        public void SimulateActivateAbility()
        {
            throw new NotImplementedException();
        }

        private void Awake()
        {
            playerStats = GetComponent<PlayerStats>();
            playerLocomotionInput = PlayerLocomotionInput.Instance;
            currentMeter = maxMeter;
            wasSprinting = false;
            timeSinceLastSprinting = 0f;
        }

        private void Update()
        {
            if (wasSprinting && !playerLocomotionInput.SprintToggledOn)
            {
                JustStoppedSprinting();
            } else if (playerLocomotionInput.SprintToggledOn)
            {
                Sprinting();
            } else
            {
                NotSprinting();
            }
        }

        private void OnDisable()
        {
            RemovePreviousModifier();
        }

        private void JustStoppedSprinting()
        {
            RemovePreviousModifier();
            timeSinceLastSprinting = 0f;
            wasSprinting = false;
        }

        private void Sprinting()
        {
            currentMeter = Mathf.Clamp(currentMeter - Time.deltaTime, 0f, maxMeter);
            float boostToApply = Mathf.Lerp(minBurstSpeed, maxBurstSpeed, currentMeter / maxMeter);

            if (wasSprinting)
            {
                RemovePreviousModifier();
            }

            playerStats.AddSpeedModifier(boostToApply);
            lastAppliedSpeedMod = boostToApply;

            wasSprinting = true;
        }

        private void NotSprinting()
        {
            timeSinceLastSprinting += Time.deltaTime;

            if (timeSinceLastSprinting >= timeUntilRecovery)
            {
                currentMeter = Mathf.Clamp(currentMeter + (Time.deltaTime * meterRecoverySpeed), 0f, maxMeter);
            }

            wasSprinting = false;
        }
        private void RemovePreviousModifier()
        {
            if (lastAppliedSpeedMod > 0)
            {
                playerStats.RemoveSpeedModifier(lastAppliedSpeedMod);
                lastAppliedSpeedMod = 0;
            }
        }

    }
}
