using PurrNet.Prediction;
using Resonance.Combat.Augments;
using Resonance.Combat.Weapons;
using Resonance.Combat.Weapons.Enums;

namespace Resonance.Inventory
{
    public struct PlayerInventoryInputData : IPredictedData
    {
        public WeaponSlot WeaponToAddSlot;
        public WeaponIdentity? WeaponIdentityToAdd;

        public string AugmentKeyToAdd;

        public void Dispose() { }
    }
}
