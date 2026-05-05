using PurrNet.Prediction;

namespace Resonance.Combat
{
    public struct PlayerEquipInputData : IPredictedData
    {
        public bool SwapWeaponPressed { get; set; }
        public bool SwapSlotOnePressed { get; set; }
        public bool SwapSlotTwoPressed { get; set; }

        public readonly void Dispose() { }
    }
}
