using System;
using Resonance.Combat.Weapons.Enums;
using UnityEngine;

namespace Resonance.PlayerController
{
    public class PlayerState : MonoBehaviour
    {
        [field: SerializeField]
        public PlayerMovementState CurrentPlayerMovementState { get; private set; } = PlayerMovementState.Idling;
        public WeaponClass CurrentWeaponClass { get; private set; }
        public bool WeaponClassInitialized { get; private set; }

        public bool IsReloading { get; private set; }
        public bool IsAttacking { get; private set; }

        public void SetReloading(bool value) => IsReloading = value;
        public void SetAttacking(bool value) => IsAttacking = value;

        public void SetPlayerMovementState(PlayerMovementState playerMovementState)
        {
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

        public bool IsInShop()
        {
            return CurrentPlayerMovementState == PlayerMovementState.InShop;
        }

        public bool IsZiplining()
        {
            return CurrentPlayerMovementState == PlayerMovementState.Ziplining;
        }

        public bool IsMatchFrozen()
        {
            return CurrentPlayerMovementState == PlayerMovementState.MatchFrozen;
        }

        public event Action<WeaponClass> OnWeaponClassChanged;

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
        InShop = 8,
        Ziplining = 9,
        MatchFrozen = 10,
    }
}
