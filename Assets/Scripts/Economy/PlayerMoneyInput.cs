using PurrNet.Prediction;

namespace Resonance.Economy
{
    public struct PlayerMoneyInput : IPredictedData
    {
        public float AmountToChange;

        public readonly void Dispose()
        {
        }
    }
}
