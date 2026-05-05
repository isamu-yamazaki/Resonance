using PurrNet.Prediction;
using UnityEngine;

namespace Resonance.Assemblies.Train
{
    /// <summary>
    /// State to forward predict on each client.
    /// </summary>
    public struct TrainPredictedState : IPredictedData<TrainPredictedState>
    {
        /// <summary>
        /// Predicted velocity propagated to consumers for physics-related logic.
        /// </summary>
        public Vector3 Velocity;
        public Vector3 lastTransformPosition;

        public void Dispose()
        {
        }
    }
}
