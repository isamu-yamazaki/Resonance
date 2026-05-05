using PurrNet.Prediction;
using UnityEngine;

namespace Resonance.Assemblies.Train
{
    public class TrainPredictedVelocity : MonoBehaviour
    {
        public Vector3 Velocity { get; private set; }
        private Vector3 lastTransformPosition;


        // protected override TrainPredictedState GetInitialState()
        // {
        //     return new TrainPredictedState
        //     {
        //         Velocity = default,
        //         lastTransformPosition = default
        //     };
        // }

        private void FixedUpdate()
        {   
            if (lastTransformPosition != default)
            {
                Velocity = (transform.position - lastTransformPosition) / Time.fixedDeltaTime;
            }

            lastTransformPosition = transform.position;

            Debug.Log($"[TrainPredictedVelocity] {Velocity}");
        }
    }
}
