using PurrNet.Prediction;

namespace Resonance.PlayerController
{
    public struct OverdriveTrailEffectInput : IPredictedData
    {
        public bool ShouldSpawnGhostsForEveryone;

        public readonly void Dispose()
        {
        }
    }
}
