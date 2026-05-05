using PurrNet.Prediction;
using Resonance.Combat.Augments;
using Resonance.Combat.Weapons;

namespace Resonance.Inventory
{
    public struct PlayerInventoryInputData : IPredictedData
    {
        public WeaponProperties WeaponToAdd;
        public bool RemoveWeaponPrimary;
        public bool RemoveWeaponSecondary;

        public AugmentProperties AugmentToAdd;
        public bool RemoveAugmentUpper;
        public bool RemoveAugmentLower;

        public void Dispose() { }
    }
}
