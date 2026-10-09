using PurrNet.Prediction;
using Resonance.Combat.Mods;
using Resonance.Player;
using Resonance.PlayerController;
using UnityEngine;

namespace Resonance.Combat.Augments
{
    public class AbilityHumanTurret : PredictedIdentity<AbilityHumanTurretInput, AbilityHumanTurretState>, IAugmentAbility, IEquippableAbility
    {
        [SerializeField] private float timeToActivate = 2f;

        [SerializeField] private float damageReduction = 0.25f;
        [SerializeField] private WeaponModProperties turretMod;

        private PlayerLocomotionInput _playerLocomotionInput;
        private PlayerStats _playerStats;
        private WeaponStatManager _weaponStatManager;

        public string AbilityKey => "ability_humanTurret";
        public string Name => "Human Turret";
        public string Description => "Standing still long enough turns you into a turret.";
        public float MaxCooldown => timeToActivate;

        public float CurrentCooldown => currentState.TimeStandingStill;

        public bool AbilityReady => false;

        #region Lifecycle

        protected override void LateAwake()
        {
            _playerStats = GetComponent<PlayerStats>();
            _playerLocomotionInput = PlayerLocomotionInput.Instance;
            _weaponStatManager = GetComponent<WeaponStatManager>();
        }

        protected override AbilityHumanTurretState GetInitialState()
        {
            return new AbilityHumanTurretState()
            {
                TimeStandingStill = 0f,
                IsTurretActive = false
            };
        }

        private void OnDisable()
        {
            DeactivateTurret(ref currentState);
        }

        #endregion

        #region Simulation

        protected override void GetFinalInput(ref AbilityHumanTurretInput input)
        {
            input.IsMoving = _playerLocomotionInput.MovementInput != Vector2.zero;
        }

        protected override void Simulate(AbilityHumanTurretInput input, ref AbilityHumanTurretState state, float delta)
        {
            if (!state.IsEquipped) return;

            if (input.IsMoving)
            {
                StandingStill(ref state, delta);
            }
            else
            {
                Moving(ref state);
            }
        }

        [SimulationOnly]
        public void SimulateActivateAbility()
        {
        }

        [SimulationOnly]
        public void SetEquipped(bool equipped)
        {
            if (currentState.IsEquipped == equipped) return;

            currentState.IsEquipped = equipped;

            // Unequipping must tear down the turret effect; OnDisable used to do this, but the
            // component no longer gets disabled.
            if (!equipped)
                DeactivateTurret(ref currentState);
        }

        [SimulationOnly]
        private void StandingStill(ref AbilityHumanTurretState state, float delta)
        {
            if (state.IsTurretActive)
            {
                return;
            }

            state.TimeStandingStill += delta;

            if (state.TimeStandingStill >= timeToActivate)
            {
                ActivateTurret(ref state);
            }
        }

        [SimulationOnly]
        private void Moving(ref AbilityHumanTurretState state)
        {
            state.TimeStandingStill = 0f;
            DeactivateTurret(ref state);
        }

        [SimulationOnly]
        private void ActivateTurret(ref AbilityHumanTurretState state)
        {
            if (state.IsTurretActive)
            {
                return;
            }

            state.IsTurretActive = true;

            _playerStats.SimulateAddDamageReductionModifier(damageReduction);
            _weaponStatManager.SimulateAddAugmentMod(turretMod);
        }

        private void DeactivateTurret(ref AbilityHumanTurretState state)
        {
            if (!state.IsTurretActive)
            {
                return;
            }

            state.IsTurretActive = false;

            _playerStats.SimulateRemoveDamageReductionModifier(damageReduction);
            _weaponStatManager.SimulateRemoveAugmentMod(turretMod);
        }

        #endregion
    }

    public struct AbilityHumanTurretInput : IPredictedData
    {
        public bool IsMoving;

        public void Dispose()
        {
        }
    }

    public struct AbilityHumanTurretState : IPredictedData<AbilityHumanTurretState>
    {
        public bool IsTurretActive;
        public float TimeStandingStill;
        public bool IsEquipped;

        public void Dispose()
        {
        }
    }
}