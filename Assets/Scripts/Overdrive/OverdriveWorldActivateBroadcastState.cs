using PurrNet.Prediction;

namespace Resonance.PlayerController
{
    public struct OverdriveWorldActivateBroadcastState : IPredictedData<OverdriveWorldActivateBroadcastState>
    {
        public bool BroadcastAudio;

        public readonly void Dispose()
        {
        }
    }
}
