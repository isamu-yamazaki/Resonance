using PurrNet.Prediction;

namespace Resonance.Combat
{
    public struct PlayerEquipInputData : IPredictedData
    {
        public bool SwapWeaponPressed;
        public bool SwapSlotOnePressed;
        public bool SwapSlotTwoPressed;

        public bool PendingUpperAugmentRemoval;
        public bool PendingLowerAugmentRemoval;

        public readonly void Dispose() { }
    }
}
