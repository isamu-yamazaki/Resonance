using PurrNet.Prediction;
using Resonance.Assemblies.AbilitySimulation.SprintBurst;
using Resonance.Player;
using Resonance.PlayerController;
using UnityEngine;

namespace Resonance.Combat.Augments
{
    public class AbilitySprintBurst : PredictedIdentity<AbilitySprintBurstInput, AbilitySprintBurstState>,
        IAugmentAbility, IEquippableAbility
    {
        [Header("Config")] [SerializeField] private SprintBurstConfig config;

        private PlayerLocomotionInput playerLocomotionInput;
        private PlayerStats playerStats;

        public string AbilityKey => "ability_sprintBurst";
        public string Name => "Sprint Burst";
        public string Description => "Move with a brief burst of speed.";
        public float MaxCooldown => config.maxMeter;

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
            return new AbilitySprintBurstState
            {
                CurrentMeter = config.maxMeter,
                WasSprinting = false,
                TimeSinceLastSprinting = 0f,
            };
        }

        #endregion

        #region Input

        protected override void UpdateInput(ref AbilitySprintBurstInput input)
        {
            if (!isOwner) return;
            input.Sprinting |= playerLocomotionInput.SprintToggledOn;
        }

        protected override void GetFinalInput(ref AbilitySprintBurstInput input)
        {
            input.Sprinting = playerLocomotionInput.SprintToggledOn;
        }

        #endregion

        #region Simulation

        [SimulationOnly]
        public void SimulateActivateAbility()
        {
        }

        // Equipped-ness lives in predicted state, not a Unity `enabled` flag, so it stays in sync with
        // the simulation and survives rollback. See IEquippableAbility.
        [SimulationOnly]
        public void SetEquipped(bool equipped)
        {
            if (currentState.IsEquipped == equipped) return;

            currentState.IsEquipped = equipped;

            // Releasing the augment must drop any speed modifier this ability applied mid-sprint,
            // mirroring the cleanup that previously lived in OnDisable.
            if (!equipped)
                RemovePreviousModifier(ref currentState);
        }

        protected override void Simulate(AbilitySprintBurstInput input, ref AbilitySprintBurstState state, float delta)
        {
            if (!state.IsEquipped) return;

            // The pure meter/boost math lives in SprintBurstSimulation.Step, which writes the desired
            // speed modifier into state.LastAppliedSpeedMod. The continuous PlayerStats modifier is this
            // ability's only side effect, so reconcile it here against the value Step produced: remove
            // the previous modifier and add the new one whenever it changes (first sprint tick adds
            // only; ramping ticks remove+add; the stop tick removes only).
            float previousSpeedMod = state.LastAppliedSpeedMod;

            SprintBurstSimulation.Step(new SprintBurstSimulationContext(input, config, delta), ref state);

            ReconcileSpeedModifier(previousSpeedMod, state.LastAppliedSpeedMod);
        }
        #endregion

        private void ReconcileSpeedModifier(float previous, float current)
        {
            if (Mathf.Approximately(previous, current)) return;

            if (previous > 0f)
                playerStats.SimulateRemoveSpeedModifier(previous);
            if (current > 0f)
                playerStats.SimulateAddSpeedModifier(current);
        }

        private void RemovePreviousModifier(ref AbilitySprintBurstState state)
        {
            if (!(state.LastAppliedSpeedMod > 0)) return;
            playerStats.SimulateRemoveSpeedModifier(state.LastAppliedSpeedMod);
            state.LastAppliedSpeedMod = 0;
        }
    }
}
