using PurrNet.Prediction;

namespace Resonance.Assemblies.Train
{
    /// <summary>
    /// State to forward predict on each client.
    /// </summary>
    public struct TrainPredictedState : IPredictedData<TrainPredictedState>
    {
        public float Velocity;

        public void Dispose()
        {
        }
    }
}
