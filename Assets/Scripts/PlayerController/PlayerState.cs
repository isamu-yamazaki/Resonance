using System;
using PurrNet.Prediction;
using Resonance.Assemblies.Player;
using Resonance.Combat.Weapons.Enums;

namespace Resonance.PlayerController
{
    public class PlayerState : PredictedIdentity<PlayerStateInput, PlayerStateData>
    {
        public PlayerMovementState CurrentPlayerMovementState => currentState.MovementState;
        public WeaponClass CurrentWeaponClass => currentState.WeaponClass;
        public bool WeaponClassInitialized => currentState.WeaponClassInitialized;

        public WeaponState CurrentWeaponState => currentState.WeaponState;

        public bool IsReloading => CurrentWeaponState == WeaponState.Reloading || CurrentWeaponState == WeaponState.EmptyReloading;
        public bool IsAttacking => CurrentWeaponState == WeaponState.Shooting;

        public event Action<WeaponState> OnWeaponStateChanged;
        public event Action<WeaponClass> OnWeaponClassChanged;

        #region External input accumulators
        private WeaponState _pendingWeaponState;
        private PlayerMovementState _pendingMovementState;
        private bool _requestWeaponStateUpdate;
        private bool _requestMovementStateUpdate;
        private PlayerStateData? _previousVerifiedState;
        #endregion

        public void SetExternalWeaponState(WeaponState state)
        {
            if (CurrentWeaponState == state) return;
            if (!IsValidTransition(CurrentWeaponState, state)) return;
            
            _pendingWeaponState = state;
            _requestWeaponStateUpdate = true;
        }

        public void SetExternalPlayerMovementState(PlayerMovementState playerMovementState)
        {
            _pendingMovementState = playerMovementState;
            _requestMovementStateUpdate = true;
        }

        [SimulationOnly]
        public void SetSimulatedPlayerMovementState(PlayerMovementState playerMovementState)
        {
            currentState.MovementState = playerMovementState;
        }

        [SimulationOnly]
        public void SetSimulatedWeaponClass(WeaponClass weaponClass)
        {
            currentState.WeaponClass = weaponClass;
        }

        private bool IsValidTransition(WeaponState from, WeaponState to)
        {
            switch (from)
            {
                case WeaponState.Idle:
                    return true;
                case WeaponState.Shooting:
                    return to == WeaponState.Idle || 
                           to == WeaponState.Reloading || 
                           to == WeaponState.EmptyReloading || 
                           to == WeaponState.Holstering;
                case WeaponState.Holstering:
                    return to == WeaponState.Casting || to == WeaponState.Stimming || to == WeaponState.Grappling || to == WeaponState.Drawing || to == WeaponState.Idle;
                case WeaponState.Drawing:
                    return to == WeaponState.Idle;
                case WeaponState.Reloading:
                case WeaponState.EmptyReloading:
                    return to == WeaponState.Idle;
                case WeaponState.Casting:
                case WeaponState.Stimming:
                case WeaponState.Grappling:
                    return to == WeaponState.Drawing;
                default:
                    return true;
            }
        }


        public bool InGroundedState()
        {
            return PlayerMovementStateUtils.IsStateGroundedState(CurrentPlayerMovementState);
        }

        public bool IsDead()
        {
            return CurrentPlayerMovementState == PlayerMovementState.Dead;
        }

        public bool IsZiplining()
        {
            return CurrentPlayerMovementState == PlayerMovementState.Ziplining;
        }

        public bool IsGrappling()
        {
            return CurrentPlayerMovementState == PlayerMovementState.Grappling;
        }

        public bool IsMatchFrozen()
        {
            return CurrentPlayerMovementState == PlayerMovementState.PreMatchFrozen ||
                   CurrentPlayerMovementState == PlayerMovementState.MatchEndedFrozen;
        }


        #region Server-auth methods

        protected override void GetFinalInput(ref PlayerStateInput input)
        {
            input.RequestExternalPlayerMovementStateUpdate = _requestMovementStateUpdate;
            input.RequestExternalWeaponStateUpdate = _requestWeaponStateUpdate;
            input.RequestedPlayerMovementState = _pendingMovementState;
            input.RequestedWeaponState = _pendingWeaponState;

            _requestMovementStateUpdate = false;
            _requestWeaponStateUpdate = false;
        }

        protected override void Simulate(PlayerStateInput input, ref PlayerStateData state, float delta)
        {
            if (input.RequestExternalPlayerMovementStateUpdate)
            {
                state.MovementState = input.RequestedPlayerMovementState;
            }
            if (input.RequestExternalWeaponStateUpdate)
            {
                state.WeaponState = input.RequestedWeaponState;
            }
        }

        protected override void UpdateView(PlayerStateData viewState, PlayerStateData? verified)
        {
            if (!verified.HasValue) return;
            var v = verified.Value;

            if (!_previousVerifiedState.HasValue || (_previousVerifiedState.Value.WeaponClass != v.WeaponClass))
                OnWeaponClassChanged?.Invoke(v.WeaponClass);
            
            if (!_previousVerifiedState.HasValue || (_previousVerifiedState.Value.WeaponState != v.WeaponState))
                OnWeaponStateChanged?.Invoke(v.WeaponState);

            _previousVerifiedState = v;
        }

        #endregion
        
    }

    public enum WeaponState
    {
        Idle = 0,
        Drawing = 1,
        Holstering = 2,
        Shooting = 3,
        Reloading = 4,
        EmptyReloading = 5,
        Casting = 6,
        Stimming = 7,
        Grappling = 8,
    }
}
