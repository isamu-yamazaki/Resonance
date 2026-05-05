using PurrNet.Prediction;
using Resonance.Combat.Augments;
using Resonance.Combat.Weapons.Enums;

namespace Resonance.Inventory
{
    public struct PlayerInventoryInputData : IPredictedData
    {
        public string WeaponToAddKey;
        public WeaponSlot WeaponToAddSlot;
        public bool RemoveWeaponPrimary;
        public bool RemoveWeaponSecondary;

        public AugmentProperties AugmentToAdd;
        public bool RemoveAugmentUpper;
        public bool RemoveAugmentLower;

        public void Dispose() { }
    }
}
