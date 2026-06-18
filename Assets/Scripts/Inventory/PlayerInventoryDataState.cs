using PurrNet.Prediction;
using Resonance.Combat.Augments;
using Resonance.Combat.Weapons;

namespace Resonance.Inventory
{
    public struct PlayerInventoryDataState : IPredictedData<PlayerInventoryDataState>
    {
        public WeaponIdentity? WeaponPrimaryIdentity;
        public WeaponIdentity? WeaponSecondaryIdentity;

        public string AugmentKeyUpper;
        public string AugmentKeyLower;

        public readonly void Dispose() { }
    }
}
