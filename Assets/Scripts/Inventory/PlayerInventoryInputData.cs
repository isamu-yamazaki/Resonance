using PurrNet.Prediction;
using Resonance.Combat.Augments;
using Resonance.Combat.Weapons.Enums;

namespace Resonance.Inventory
{
    public struct PlayerInventoryInputData : IPredictedData
    {
        public string WeaponToAddKey;
        public WeaponSlot WeaponToAddSlot;

        public string AugmentKeyToAdd;

        public void Dispose() { }
    }
}
