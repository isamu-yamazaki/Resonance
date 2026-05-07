using PurrNet.Prediction;

namespace Resonance.Combat
{
    public struct PlayerEquipDataState : IPredictedData<PlayerEquipDataState>
    {
        public string EquippedWeaponKey;
        public int CurrentSlot;

        public readonly void Dispose() { }
    }
}
