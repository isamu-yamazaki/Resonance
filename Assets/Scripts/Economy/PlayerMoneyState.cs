using PurrNet.Prediction;

namespace Resonance.Economy
{
    public struct PlayerMoneyState : IPredictedData<PlayerMoneyState>
    {
        public float Balance;

        public readonly void Dispose()
        {
        }
    }
}
