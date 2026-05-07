using PurrNet.Prediction;
using Resonance.Combat.Augments;

namespace Resonance.Inventory
{
    public struct PlayerInventoryDataState : IPredictedData<PlayerInventoryDataState>
    {
        public string WeaponPrimaryKey;
        public string WeaponSecondaryKey;

        public string AugmentKeyUpper;
        public string AugmentKeyLower;

        public readonly void Dispose() { }
    }
}
