using PurrNet.Prediction;
using Resonance.Combat.Augments;

namespace Resonance.Inventory
{
    public struct PlayerInventoryDataState : IPredictedData<PlayerInventoryDataState>
    {
        public string WeaponPrimaryKey;
        public string WeaponSecondaryKey;

        public AugmentProperties AugmentUpper;
        public AugmentProperties AugmentLower;

        public readonly void Dispose() { }
    }
}
