using PurrNet.Prediction;

namespace Resonance.Combat
{
    public struct PlayerEquipInputData : IPredictedData
    {
        public bool SwapWeaponPressed;
        public bool SwapSlotOnePressed;
        public bool SwapSlotTwoPressed;

        public string WeaponKeyToEquip;
        public string WeaponIdToEquip;
        public bool PendingPrimaryWeaponSlotRemoval;
        public bool PendingSecondaryWeaponSlotRemoval;
        public bool PendingUpperAugmentRemoval;
        public bool PendingLowerAugmentRemoval;

        public readonly void Dispose() { }
    }
}
