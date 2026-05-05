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

        // Pending operations queued by the public mutator API; drained by UpdateInput each frame.
        private WeaponProperties _pendingWeaponAdd;
        private bool _pendingRemoveWeaponPrimary;
        private bool _pendingRemoveWeaponSecondary;
        private AugmentProperties _pendingAugmentAdd;
        private bool _pendingRemoveAugmentUpper;
        private bool _pendingRemoveAugmentLower;

        protected override PlayerInventoryDataState GetInitialState()
        {
            var state = new PlayerInventoryDataState();
            if (startingWeapon != null)
            {
                WeaponProperties weapon = startingWeapon.Clone();
                switch (weapon.Slot)
                {
                    case WeaponSlot.Primary:
                        state.WeaponPrimary = weapon;
                        break;
                    case WeaponSlot.Secondary:
                        state.WeaponSecondary = weapon;
                        break;
                }
            }
            return state;
        }

        public void AddWeapon(WeaponProperties weaponToAdd)
        {
            if (weaponToAdd == null) return;
            _pendingWeaponAdd = weaponToAdd;
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
            _pendingAugmentAdd = augmentToAdd;
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

        protected override void UpdateInput(ref PlayerInventoryInputData input)
        {
            if (_pendingWeaponAdd != null)
            {
                input.WeaponToAdd = _pendingWeaponAdd;
                _pendingWeaponAdd = null;
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
            if (_pendingAugmentAdd != null)
            {
                input.AugmentToAdd = _pendingAugmentAdd;
                _pendingAugmentAdd = null;
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
            if (input.WeaponToAdd != null)
            {
                switch (input.WeaponToAdd.Slot)
                {
                    case WeaponSlot.Primary:
                        state.WeaponPrimary = input.WeaponToAdd;
                        break;
                    case WeaponSlot.Secondary:
                        state.WeaponSecondary = input.WeaponToAdd;
                        break;
                }
            }
            if (input.RemoveWeaponPrimary) state.WeaponPrimary = null;
            if (input.RemoveWeaponSecondary) state.WeaponSecondary = null;

            if (input.AugmentToAdd != null)
            {
                switch (input.AugmentToAdd.Slot)
                {
                    case AugmentSlot.Upper:
                        state.AugmentUpper = input.AugmentToAdd;
                        break;
                    case AugmentSlot.Lower:
                        state.AugmentLower = input.AugmentToAdd;
                        break;
                }
            }
            if (input.RemoveAugmentUpper) state.AugmentUpper = null;
            if (input.RemoveAugmentLower) state.AugmentLower = null;
        }

        protected override PlayerInventoryDataState Interpolate(
            PlayerInventoryDataState from,
            PlayerInventoryDataState to,
            float t)
        {
            // Reference-typed slots are discrete; snap to `to`.
            return to;
        }

        protected override void UpdateView(PlayerInventoryDataState viewState, PlayerInventoryDataState? verified)
        {
            weaponInventory[0] = viewState.WeaponPrimary;
            weaponInventory[1] = viewState.WeaponSecondary;
            augmentInventory[0] = viewState.AugmentUpper;
            augmentInventory[1] = viewState.AugmentLower;
        }
    }
}
