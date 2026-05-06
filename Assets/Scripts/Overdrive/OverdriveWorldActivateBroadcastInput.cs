using PurrNet.Prediction;

namespace Resonance.PlayerController
{
    public struct OverdriveWorldActivateBroadcastInput : IPredictedData
    {
        public bool RequestAudioBroadcastNextTick;

        public readonly void Dispose()
        {
        }
    }
}
