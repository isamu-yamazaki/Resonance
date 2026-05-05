using PurrNet.Prediction;
using Resonance.Combat.Augments;
using Resonance.Combat.Weapons;

namespace Resonance.Inventory
{
    public struct PlayerInventoryDataState : IPredictedData<PlayerInventoryDataState>
    {
        public WeaponProperties WeaponPrimary;
        public WeaponProperties WeaponSecondary;

        public AugmentProperties AugmentUpper;
        public AugmentProperties AugmentLower;

        public readonly void Dispose() { }
    }
}
