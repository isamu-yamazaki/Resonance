using PurrNet.Prediction;
using UnityEngine;

namespace Resonance.Player
{
    public struct PlayerStatsInputData : IPredictedData
    {
        public float ExternalHealAmount;
        public float ExternalDamageAmount;
        public Vector3 ExternalAttackerPosition;
        public float? ExternalSpeedModifierToAdd;
        public float? ExternalSpeedModifierToRemove;
        public float? ExternalDamageReductionModifierToAdd;
        public float? ExternalDamageReductionModifierToRemove;

        public readonly void Dispose() { }
    }
}
