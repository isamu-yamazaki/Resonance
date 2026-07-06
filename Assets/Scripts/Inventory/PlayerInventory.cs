using System;
using PurrNet.Prediction;
using Resonance.Combat.Augments;
using Resonance.Combat.Weapons;
using Resonance.Combat.Weapons.Enums;
using UnityEngine;

namespace Resonance.Inventory
{
    public class PlayerInventory : PredictedIdentity<PlayerInventoryInputData, PlayerInventoryDataState>
    {
        public event Action OnInventoryChanged;

        // Resolved from the simulated state so reads reflect currentState on both server and client,
        // not just the locally-applied verified view.
        public WeaponProperties[] WeaponInventory
        {
            get
            {
                return new[]
                {
                    WeaponResolver.ResolveWeapon(currentState.WeaponPrimaryIdentity),
                    WeaponResolver.ResolveWeapon(currentState.WeaponSecondaryIdentity),
                };
            }
        }

        public AugmentProperties[] AugmentInventory => new[]
        {
            // unlike weapons, augments can be looked up solely by key
            FindAugmentByKey(currentState.AugmentKeyUpper),
            FindAugmentByKey(currentState.AugmentKeyLower),
        };

        private PlayerInventoryDataState? _previousVerifiedState;

        [SerializeField] WeaponProperties startingWeapon;

        private AugmentProperties[] _augments;

        // Pending operations queued by the public mutator API; drained by UpdateInput each frame.
        private WeaponIdentity? _pendingWeaponIdentitySet;
        private WeaponIdentity? _pendingWeaponIdentityRemove;
        private string _pendingAugmentKeyAdd;
        private string _pendingAugmentKeyRemove;

        protected override void LateAwake()
        {
            _augments = Resources.LoadAll<AugmentProperties>("Content/Augments");
        }

        protected override PlayerInventoryDataState GetInitialState()
        {
            var state = new PlayerInventoryDataState();

            if (startingWeapon != null)
            {
                var startingWeaponWithId = startingWeapon.Clone();
                var weaponIdentity = WeaponIdentity.FromWeaponProperties(startingWeaponWithId);
                switch (startingWeaponWithId.Slot)
                {
                    case WeaponSlot.Primary:
                        state.WeaponPrimaryIdentity = weaponIdentity;
                        break;
                    case WeaponSlot.Secondary:
                        state.WeaponSecondaryIdentity = weaponIdentity;
                        break;
                }
            }

            return state;
        }

        public void SetWeaponExternal(WeaponProperties weaponToSet)
        {
            if (weaponToSet == null) return;
            _pendingWeaponIdentitySet = WeaponIdentity.FromWeaponProperties(weaponToSet);
        }

        public void RemoveWeaponExternal(WeaponProperties weaponToRemove)
        {
            if (weaponToRemove == null) return;
            _pendingWeaponIdentityRemove = WeaponIdentity.FromWeaponProperties(weaponToRemove);
        }

        // [SimulationOnly]
        // public void RemoveWeapon(WeaponSlot slot)
        // {
        //     switch (slot)
        //     {
        //         case WeaponSlot.Primary:
        //             currentState.WeaponPrimaryIdentity = null;
        //             break;
        //         case WeaponSlot.Secondary:
        //             currentState.WeaponSecondaryIdentity = null;
        //             break;
        //     }
        // }

        public void AddAugment(AugmentProperties augmentToAdd)
        {
            if (augmentToAdd == null) return;
            _pendingAugmentKeyAdd = augmentToAdd.Key;
        }

        public void RemoveAugmentExternal(AugmentProperties augmentToRemove)
        {
            if (augmentToRemove == null) return;
            _pendingAugmentKeyRemove = augmentToRemove.Key;
        }

        protected override void GetFinalInput(ref PlayerInventoryInputData input)
        {
            if (_pendingWeaponIdentitySet != null)
            {
                input.WeaponIdentityToSet = _pendingWeaponIdentitySet;
                _pendingWeaponIdentitySet = null;
            }

            if (_pendingAugmentKeyAdd != null)
            {
                input.AugmentKeyToAdd = _pendingAugmentKeyAdd;
                _pendingAugmentKeyAdd = null;
            }

            if (_pendingAugmentKeyRemove != null)
            {
                input.AugmentKeyToRemove = _pendingAugmentKeyRemove;
                _pendingAugmentKeyRemove = null;
            }

            if (_pendingWeaponIdentityRemove != null)
            {
                input.WeaponIdentityToRemove = _pendingWeaponIdentityRemove;
                _pendingWeaponIdentityRemove = null;
            }
        }

        protected override void Simulate(PlayerInventoryInputData input, ref PlayerInventoryDataState state,
            float delta)
        {
            if (input.WeaponIdentityToSet.HasValue)
            {
                var weaponIdentity = input.WeaponIdentityToSet.Value;
                var baseWeapon = FindWeaponByKey(weaponIdentity.Key);
                switch (baseWeapon.Slot)
                {
                    case WeaponSlot.Primary:
                        state.WeaponPrimaryIdentity = weaponIdentity;
                        break;
                    case WeaponSlot.Secondary:
                        state.WeaponSecondaryIdentity = weaponIdentity;
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }

            if (input.WeaponIdentityToRemove.HasValue)
            {
                var weaponIdentity = input.WeaponIdentityToRemove.Value;
                var baseWeapon = FindWeaponByKey(weaponIdentity.Key);

                switch (baseWeapon.Slot)
                {
                    case WeaponSlot.Primary:
                        if (state.WeaponPrimaryIdentity == weaponIdentity)
                            state.WeaponPrimaryIdentity = null;
                        break;
                    case WeaponSlot.Secondary:
                        if (state.WeaponSecondaryIdentity == weaponIdentity)
                            state.WeaponSecondaryIdentity = null;
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }

            if (input.AugmentKeyToAdd != null)
            {
                var augment = FindAugmentByKey(input.AugmentKeyToAdd);
                if (augment != null)
                {
                    switch (augment.Slot)
                    {
                        case AugmentSlot.Upper:
                            state.AugmentKeyUpper = input.AugmentKeyToAdd;
                            break;
                        case AugmentSlot.Lower:
                            state.AugmentKeyLower = input.AugmentKeyToAdd;
                            break;
                        default:
                            throw new ArgumentOutOfRangeException();
                    }
                }
                else
                {
                    Debug.Log(
                        $"[PlayerInventory] Attempted to assign augment key {input.AugmentKeyToAdd}, but unable to find corresponding augment");
                }
            }

            if (input.AugmentKeyToRemove != null)
            {
                var augment = FindAugmentByKey(input.AugmentKeyToRemove);
                if (augment != null)
                {
                    // Clear only if the slot still holds this exact augment, so a stale removal
                    // can't clobber an augment equipped after the sell was queued.
                    switch (augment.Slot)
                    {
                        case AugmentSlot.Upper:
                            if (state.AugmentKeyUpper == input.AugmentKeyToRemove)
                                state.AugmentKeyUpper = null;
                            break;
                        case AugmentSlot.Lower:
                            if (state.AugmentKeyLower == input.AugmentKeyToRemove)
                                state.AugmentKeyLower = null;
                            break;
                        default:
                            throw new ArgumentOutOfRangeException();
                    }
                }
            }
        }

        protected override PlayerInventoryDataState Interpolate(
            PlayerInventoryDataState from,
            PlayerInventoryDataState to,
            float t)
        {
            return to;
        }

        protected override void UpdateView(PlayerInventoryDataState viewState, PlayerInventoryDataState? verified)
        {
            if (!verified.HasValue) return;
            var v = verified.Value;

            if (!_previousVerifiedState.HasValue
                || _previousVerifiedState.Value.WeaponPrimaryIdentity != v.WeaponPrimaryIdentity
                || _previousVerifiedState.Value.WeaponSecondaryIdentity != v.WeaponSecondaryIdentity
                || _previousVerifiedState.Value.AugmentKeyUpper != v.AugmentKeyUpper
                || _previousVerifiedState.Value.AugmentKeyLower != v.AugmentKeyLower)
            {
                OnInventoryChanged?.Invoke();
            }

            _previousVerifiedState = v;
        }

        private static WeaponProperties FindWeaponByKey(string key)
        {
            return WeaponResolver.FindBaseWeaponByKey(key);
        }

        public AugmentProperties FindAugmentByKey(string key)
        {
            if (string.IsNullOrEmpty(key) || _augments == null) return null;
            return Array.Find(_augments, a => a.Key == key);
        }
    }
}