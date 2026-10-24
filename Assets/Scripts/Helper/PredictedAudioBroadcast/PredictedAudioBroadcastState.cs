using PurrNet.Prediction;

namespace Resonance.Helper.PredictedAudioBroadcast
{
    /// <summary>
    /// Common state for indicating whether a predicted script should broadcast audio locally.
    /// </summary>
    public struct PredictedAudioBroadcastState : IPredictedData<PredictedAudioBroadcastState>
    {
        public bool BroadcastAudio;
        public bool BroadcastAudioNextTick;

        public readonly void Dispose()
        {
        }
    }
}
