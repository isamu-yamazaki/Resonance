using PurrNet.Prediction;
using UnityEngine;

namespace Resonance.Assemblies.Train
{
    public class TrainPredictedVelocity : PredictedIdentity<TrainPredictedState>
    {
        public Vector3 Velocity => currentState.Velocity;

        protected override TrainPredictedState GetInitialState()
        {
            return new TrainPredictedState
            {
                Velocity = default,
                lastTransformPosition = default
            };
        }

        protected override void Simulate(ref TrainPredictedState state, float delta)
        {
            if (!predictionManager.isReplaying)
            {
                Debug.Log($"[TrainPredictedVelocity] {state.Velocity}");
            }
            
            
            if (state.lastTransformPosition != default)
            {
                state.Velocity = (transform.position - state.lastTransformPosition) / delta;
            }

            state.lastTransformPosition = transform.position;
        }
    }
}
