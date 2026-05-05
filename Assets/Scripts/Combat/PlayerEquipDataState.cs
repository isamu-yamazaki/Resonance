using PurrNet.Prediction;

namespace Resonance.Combat
{
    public struct PlayerEquipDataState : IPredictedData<PlayerEquipDataState>
    {
        public int CurrentSlot;

        public readonly void Dispose() { }
    }
}
