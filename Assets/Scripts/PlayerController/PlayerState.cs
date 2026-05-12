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
        private WeaponClass pendingWeaponClass;
        private WeaponState pendingWeaponState;
        private PlayerMovementState pendingMovementState;
        private bool requestWeaponClassUpdate;
        private bool requestWeaponStateUpdate;
        private bool requestMovementStateUpdate;
        private PlayerStateData? previousVerifiedState;
        #endregion

        public void SetExternalWeaponState(WeaponState state)
        {
            if (CurrentWeaponState == state) return;
            if (!IsValidTransition(CurrentWeaponState, state)) return;
            
            pendingWeaponState = state;
            requestWeaponStateUpdate = true;
        }

        public void SetExternalPlayerMovementState(PlayerMovementState playerMovementState)
        {
            pendingMovementState = playerMovementState;
            requestMovementStateUpdate = true;
        }

        [SimulationOnly]
        public void SetSimulatedPlayerMovementState(PlayerMovementState playerMovementState)
        {
            currentState.MovementState = playerMovementState;
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

        public void SetWeaponClass(WeaponClass weaponClass)
        {
            pendingWeaponClass = weaponClass;
            requestWeaponClassUpdate = true;
        }

        #region Server-auth methods

        protected override void GetFinalInput(ref PlayerStateInput input)
        {
            input.RequestExternalPlayerMovementStateUpdate = requestMovementStateUpdate;
            input.RequestExternalWeaponClassUpdate = requestWeaponClassUpdate;
            input.RequestExternalWeaponStateUpdate = requestWeaponStateUpdate;
            input.RequestedPlayerMovementState = pendingMovementState;
            input.RequestedWeaponClass = pendingWeaponClass;
            input.RequestedWeaponState = pendingWeaponState;
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
            if (input.RequestExternalWeaponClassUpdate)
            {
                state.WeaponClass = input.RequestedWeaponClass;
                state.WeaponClassInitialized = true;
            }
        }

        protected override void UpdateView(PlayerStateData viewState, PlayerStateData? verified)
        {
            if (!verified.HasValue) return;
            var v = verified.Value;

            if (!previousVerifiedState.HasValue || (previousVerifiedState.Value.WeaponClass != v.WeaponClass))
                OnWeaponClassChanged?.Invoke(v.WeaponClass);
            
            if (!previousVerifiedState.HasValue || (previousVerifiedState.Value.WeaponState != v.WeaponState))
                OnWeaponStateChanged?.Invoke(v.WeaponState);

            previousVerifiedState = v;
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
