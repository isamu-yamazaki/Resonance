using System.Collections;
using PurrNet.Prediction;
using Resonance.Player;
using Resonance.PlayerController;
using UnityEngine;

namespace Resonance.Combat.Augments
{
    public class AbilitySprintBurst : PredictedIdentity<AbilityStateBurstInput, AbilitySprintBurstState>, IAugmentAbility
    {
        [SerializeField] private float maxBurstSpeed = 2f;
        [SerializeField] private float minBurstSpeed = 1.2f;
        [SerializeField] private float maxMeter = 5f;
        [SerializeField] private float meterRecoverySpeed = 1f;
        [SerializeField] private float timeUntilRecovery = 0.5f;

        private PlayerLocomotionInput playerLocomotionInput;
        private PlayerStats playerStats;

        public string AbilityKey => "ability_sprintBurst";
        public string Name => "Sprint Burst";
        public string Description => "Move with a brief burst of speed.";
        public float MaxCooldown => maxMeter;

        public float CurrentCooldown => currentState.CurrentMeter;

        public bool AbilityReady => false;

        #region Lifecycle
        protected override void LateAwake()
        {
            playerStats = GetComponent<PlayerStats>();
            playerLocomotionInput = PlayerLocomotionInput.Instance;
        }

        protected override AbilitySprintBurstState GetInitialState()
        {
            return new AbilitySprintBurstState()
            {
                CurrentMeter = maxMeter,
                WasSprinting = false,
                TimeSinceLastSprinting = 0f,
            };
        }

        #endregion

        #region Input
        public void ActivateAbilityExternal()
        {
        }

        protected override void UpdateInput(ref AbilityStateBurstInput input)
        {
            if (!isOwner) return;
            input.SprintToggledOn |= playerLocomotionInput.SprintToggledOn;
        }

        #endregion

        #region Simulation

        [SimulationOnly]
        public void SimulateActivateAbility()
        {
        }

        protected override void Simulate(AbilityStateBurstInput input, ref AbilitySprintBurstState state, float delta)
        {
            if (!enabled) return;

            if (state.WasSprinting && !input.SprintToggledOn)
            {
                JustStoppedSprinting(ref state);
            }
            else if (input.SprintToggledOn)
            {
                Sprinting(ref state, delta);
            }
            else
            {
                NotSprinting(ref state, delta);
            }
        }
        #endregion

        private void OnDisable()
        {
            RemovePreviousModifier(ref currentState);
        }

        private void JustStoppedSprinting(ref AbilitySprintBurstState state)
        {
            RemovePreviousModifier(ref state);
            state.TimeSinceLastSprinting = 0f;
            state.WasSprinting = false;
        }

        private void Sprinting(ref AbilitySprintBurstState state, float delta)
        {
            state.CurrentMeter = Mathf.Clamp(state.CurrentMeter - delta, 0f, maxMeter);
            float boostToApply = Mathf.Lerp(minBurstSpeed, maxBurstSpeed, state.CurrentMeter / maxMeter);
            Debug.Log($"[AbilitySprintBurst] {boostToApply}");

            if (state.WasSprinting)
            {
                RemovePreviousModifier(ref state);
            }

            playerStats.SimulateAddSpeedModifier(boostToApply);
            state.LastAppliedSpeedMod = boostToApply;

            state.WasSprinting = true;
        }

        private void NotSprinting(ref AbilitySprintBurstState state, float delta)
        {
            state.TimeSinceLastSprinting += delta;

            if (state.TimeSinceLastSprinting >= timeUntilRecovery)
            {
                state.CurrentMeter = Mathf.Clamp(state.CurrentMeter + (delta * meterRecoverySpeed), 0f, maxMeter);
            }

            state.WasSprinting = false;
        }

        private void RemovePreviousModifier(ref AbilitySprintBurstState state)
        {
            if (state.LastAppliedSpeedMod > 0)
            {
                playerStats.SimulateRemoveSpeedModifier(state.LastAppliedSpeedMod);
                state.LastAppliedSpeedMod = 0;
            }
        }
    }

    public struct AbilitySprintBurstState : IPredictedData<AbilitySprintBurstState>
    {
        public void Dispose()
        {
        }

        public bool IsEquipped;
        public float TimeSinceLastSprinting;
        public float CurrentMeter;
        public bool WasSprinting;
        public float LastAppliedSpeedMod;
    }

    public struct AbilityStateBurstInput : IPredictedData
    {
        public void Dispose()
        {
        }

        public bool SprintToggledOn;
    }
}