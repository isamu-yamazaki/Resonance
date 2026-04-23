using System;
using PurrNet;
using Resonance.Combat.Weapons.Enums;
using UnityEngine;

namespace Resonance.PlayerController
{
    public class PlayerState : NetworkBehaviour
    {
        [field: SerializeField]
        public PlayerMovementState CurrentPlayerMovementState { get; private set; } = PlayerMovementState.Idling;
        public WeaponClass CurrentWeaponClass { get; private set; }
        public bool WeaponClassInitialized { get; private set; }

        public WeaponState CurrentWeaponState { get; private set; } = WeaponState.Idle;

        public bool IsReloading => CurrentWeaponState == WeaponState.Reloading || CurrentWeaponState == WeaponState.EmptyReloading;
        public bool IsAttacking => CurrentWeaponState == WeaponState.Shooting;

        public event Action<WeaponState> OnWeaponStateChanged;
        public event Action<WeaponClass> OnWeaponClassChanged;

        public void SetWeaponState(WeaponState state)
        {
            if (CurrentWeaponState == state) return;
            if (!IsValidTransition(CurrentWeaponState, state)) return;
            
            CurrentWeaponState = state;
            OnWeaponStateChanged?.Invoke(state);
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

        public void SetPlayerMovementState(PlayerMovementState playerMovementState)
        {
            if (!isOwner) return;
            CurrentPlayerMovementState = playerMovementState;
        }

        public bool InGroundedState()
        {
            return IsStateGroundedState(CurrentPlayerMovementState);
        }

        public bool IsStateGroundedState(PlayerMovementState movementState)
        {
            return movementState == PlayerMovementState.Idling ||
                   movementState == PlayerMovementState.Crouching ||
                   movementState == PlayerMovementState.Running ||
                   movementState == PlayerMovementState.Sprinting ||
                   movementState == PlayerMovementState.Sliding;
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
            CurrentWeaponClass = weaponClass;
            WeaponClassInitialized = true;
            OnWeaponClassChanged?.Invoke(weaponClass);
        }
    }

    public enum PlayerMovementState
    {
        Idling = 0,
        Crouching = 1,
        Running = 2,
        Sprinting = 3,
        Jumping = 4,
        Falling = 5,
        Sliding = 6,
        Dead = 7,
        Ziplining = 8,
        PreMatchFrozen = 9,
        MatchEndedFrozen = 10,
        Grappling = 11,
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
