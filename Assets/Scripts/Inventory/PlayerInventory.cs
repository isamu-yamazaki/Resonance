using PurrNet.Prediction;
using Resonance.Combat.Augments;
using Resonance.Combat.Weapons;
using Resonance.Combat.Weapons.Enums;
using UnityEngine;

namespace Resonance.Inventory
{
    public class PlayerInventory : PredictedIdentity<PlayerInventoryInputData, PlayerInventoryDataState>
    {
        public event System.Action OnInventoryChanged;

        // Resolved from the simulated state so reads reflect currentState on both server and client,
        // not just the locally-applied verified view.
        public WeaponProperties[] weaponInventory => new[]
        {
            FindWeaponByKey(currentState.WeaponPrimaryKey),
            FindWeaponByKey(currentState.WeaponSecondaryKey),
        };

        public AugmentProperties[] augmentInventory => new[]
        {
            FindAugmentByKey(currentState.AugmentKeyUpper),
            FindAugmentByKey(currentState.AugmentKeyLower),
        };

        private PlayerInventoryDataState? _previousVerifiedState;

        [SerializeField] WeaponProperties startingWeapon;

        private WeaponProperties[] _weaponResources;
        private AugmentProperties[] _augmentResources;

        // Pending operations queued by the public mutator API; drained by UpdateInput each frame.
        private string _pendingWeaponAddKey;
        private WeaponSlot _pendingWeaponAddSlot;
        private string _pendingAugmentKeyAdd;

        protected override void LateAwake()
        {
            _weaponResources = Resources.LoadAll<WeaponProperties>("Content/Weapons");
            _augmentResources = Resources.LoadAll<AugmentProperties>("Content/Augments");
        }

        protected override PlayerInventoryDataState GetInitialState()
        {
            var state = new PlayerInventoryDataState();
            if (startingWeapon != null)
            {
                switch (startingWeapon.Slot)
                {
                    case WeaponSlot.Primary:
                        state.WeaponPrimaryKey = startingWeapon.Key;
                        break;
                    case WeaponSlot.Secondary:
                        state.WeaponSecondaryKey = startingWeapon.Key;
                        break;
                }
            }
            return state;
        }

        public void AddWeapon(WeaponProperties weaponToAdd)
        {
            if (weaponToAdd == null) return;
            _pendingWeaponAddKey = weaponToAdd.Key;
            _pendingWeaponAddSlot = weaponToAdd.Slot;
        }

        [SimulationOnly]
        public void RemoveWeapon(WeaponSlot slot)
        {
            switch (slot)
            {
                case WeaponSlot.Primary:
                    currentState.WeaponPrimaryKey = null;
                    break;
                case WeaponSlot.Secondary:
                    currentState.WeaponSecondaryKey = null;
                    break;
            }
        }

        public void AddAugment(AugmentProperties augmentToAdd)
        {
            if (augmentToAdd == null) return;
            _pendingAugmentKeyAdd = augmentToAdd.Key;
        }

        [SimulationOnly]
        public void RemoveAugment(AugmentSlot slot)
        {
            switch (slot)
            {
                case AugmentSlot.Upper:
                    currentState.AugmentKeyUpper = null;
                    break;
                case AugmentSlot.Lower:
                    currentState.AugmentKeyLower = null;
                    break;
            }
        }

        protected override void GetFinalInput(ref PlayerInventoryInputData input)
        {
            if (_pendingWeaponAddKey != null)
            {
                input.WeaponToAddKey = _pendingWeaponAddKey;
                input.WeaponToAddSlot = _pendingWeaponAddSlot;
                _pendingWeaponAddKey = null;
            }
            if (_pendingAugmentKeyAdd != null)
            {
                input.AugmentKeyToAdd = _pendingAugmentKeyAdd;
                _pendingAugmentKeyAdd = null;
            }
        }

        protected override void Simulate(PlayerInventoryInputData input, ref PlayerInventoryDataState state, float delta)
        {
            if (input.WeaponToAddKey != null)
            {
                switch (input.WeaponToAddSlot)
                {
                    case WeaponSlot.Primary:
                        state.WeaponPrimaryKey = input.WeaponToAddKey;
                        break;
                    case WeaponSlot.Secondary:
                        state.WeaponSecondaryKey = input.WeaponToAddKey;
                        break;
                }
            }

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
                }
            }
            else if (input.AugmentKeyToAdd != null)
            {
                Debug.Log($"[PlayerInventory] Attempted to assign augment key {input.AugmentKeyToAdd}, but unable to find corresponding augment");
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
                || _previousVerifiedState.Value.WeaponPrimaryKey != v.WeaponPrimaryKey
                || _previousVerifiedState.Value.WeaponSecondaryKey != v.WeaponSecondaryKey
                || _previousVerifiedState.Value.AugmentKeyUpper != v.AugmentKeyUpper
                || _previousVerifiedState.Value.AugmentKeyLower != v.AugmentKeyLower)
            {
                OnInventoryChanged?.Invoke();
            }

            _previousVerifiedState = v;
        }

        private WeaponProperties FindWeaponByKey(string key)
        {
            if (string.IsNullOrEmpty(key) || _weaponResources == null) return null;
            return System.Array.Find(_weaponResources, w => w.Key == key);
        }

        private AugmentProperties FindAugmentByKey(string key)
        {
            if (string.IsNullOrEmpty(key) || _augmentResources == null) return null;
            return System.Array.Find(_augmentResources, a => a.Key == key);
        }
    }
}
