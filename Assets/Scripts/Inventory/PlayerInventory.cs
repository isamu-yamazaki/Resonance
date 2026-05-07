using PurrNet.Prediction;
using Resonance.Combat.Augments;
using Resonance.Combat.Weapons;
using Resonance.Combat.Weapons.Enums;
using UnityEngine;

namespace Resonance.Inventory
{
    public class PlayerInventory : PredictedIdentity<PlayerInventoryInputData, PlayerInventoryDataState>
    {
        public WeaponProperties[] weaponInventory { get; private set; } = new WeaponProperties[2];
        public AugmentProperties[] augmentInventory { get; private set; } = new AugmentProperties[2];

        [SerializeField] WeaponProperties startingWeapon;

        private WeaponProperties[] _weaponResources;
        private AugmentProperties[] _augmentResources;

        // Pending operations queued by the public mutator API; drained by UpdateInput each frame.
        private string _pendingWeaponAddKey;
        private WeaponSlot _pendingWeaponAddSlot;
        private bool _pendingRemoveWeaponPrimary;
        private bool _pendingRemoveWeaponSecondary;
        private string _pendingAugmentKeyAdd;
        private bool _pendingRemoveAugmentUpper;
        private bool _pendingRemoveAugmentLower;

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

        public void RemoveWeapon(WeaponSlot slot)
        {
            switch (slot)
            {
                case WeaponSlot.Primary:
                    _pendingRemoveWeaponPrimary = true;
                    break;
                case WeaponSlot.Secondary:
                    _pendingRemoveWeaponSecondary = true;
                    break;
            }
        }

        public void AddAugment(AugmentProperties augmentToAdd)
        {
            if (augmentToAdd == null) return;
            _pendingAugmentKeyAdd = augmentToAdd.Key;
        }

        public void RemoveAugment(AugmentSlot slot)
        {
            switch (slot)
            {
                case AugmentSlot.Upper:
                    _pendingRemoveAugmentUpper = true;
                    break;
                case AugmentSlot.Lower:
                    _pendingRemoveAugmentLower = true;
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
            if (_pendingRemoveWeaponPrimary)
            {
                input.RemoveWeaponPrimary = true;
                _pendingRemoveWeaponPrimary = false;
            }
            if (_pendingRemoveWeaponSecondary)
            {
                input.RemoveWeaponSecondary = true;
                _pendingRemoveWeaponSecondary = false;
            }
            if (_pendingAugmentKeyAdd != null)
            {
                input.AugmentKeyToAdd = _pendingAugmentKeyAdd;
                _pendingAugmentKeyAdd = null;
            }
            if (_pendingRemoveAugmentUpper)
            {
                input.RemoveAugmentUpper = true;
                _pendingRemoveAugmentUpper = false;
            }
            if (_pendingRemoveAugmentLower)
            {
                input.RemoveAugmentLower = true;
                _pendingRemoveAugmentLower = false;
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
            if (input.RemoveWeaponPrimary) state.WeaponPrimaryKey = null;
            if (input.RemoveWeaponSecondary) state.WeaponSecondaryKey = null;

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
            if (input.RemoveAugmentUpper) state.AugmentKeyUpper = null;
            if (input.RemoveAugmentLower) state.AugmentKeyLower = null;
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
            weaponInventory[0] = FindWeaponByKey(viewState.WeaponPrimaryKey);
            weaponInventory[1] = FindWeaponByKey(viewState.WeaponSecondaryKey);
            augmentInventory[0] = FindAugmentByKey(viewState.AugmentKeyUpper);
            augmentInventory[1] = FindAugmentByKey(viewState.AugmentKeyLower);
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
