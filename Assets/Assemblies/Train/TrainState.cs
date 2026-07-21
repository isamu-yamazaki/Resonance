using PurrNet.Prediction;
using UnityEngine;

namespace Resonance.Assemblies.Train
{
    /// <summary>
    /// Predicted state governed by the server, replayed locally each client tick.
    /// </summary>
    public struct TrainState : IPredictedData<TrainState>
    {
        public Vector3 position;
        public Vector3 velocity;
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
