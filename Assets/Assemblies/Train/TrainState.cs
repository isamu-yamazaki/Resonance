using PurrNet.Prediction;
using UnityEngine;

namespace Resonance.Assemblies.Train
{
    public struct TrainState : IPredictedData<TrainState>
    {
        public Vector3 position;
        public float currentSpeed;
        public TrainMovementState movementState;
        public TrainDirection direction;
        public int currentStationIndex;
        public int nextStationIndex;
        public float stopTimer;
        public bool preDepartFired;

        public void Dispose() { }

        public readonly bool IsMoving => movementState != TrainMovementState.StoppedAtStation;
    }
}
