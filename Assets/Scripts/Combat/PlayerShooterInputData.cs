using PurrNet.Prediction;
using UnityEngine;

namespace Resonance.Combat
{
    public struct PlayerShooterInputData : IPredictedData
    {
        public bool AttackPressed;
        public bool AttackHeld;
        public bool ReloadPressed;
        public Vector3 PlayerCameraPosition;
        public Vector3 PlayerCameraForward;

        public readonly void Dispose() { }
    }
}
