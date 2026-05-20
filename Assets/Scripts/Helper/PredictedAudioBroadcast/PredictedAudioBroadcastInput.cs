using PurrNet.Prediction;

namespace Resonance.Helper.PredictedAudioBroadcast
{
    /// <summary>
    /// Common input struct for requesting an audio broadcast in a predicted audio identity.
    /// </summary>
    public struct PredictedAudioBroadcastInput : IPredictedData
    {
        public bool RequestAudioBroadcastNextTick;

        public readonly void Dispose()
        {
        }
    }
}
