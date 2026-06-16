using System;
using UnityEngine;

namespace Resonance.Assemblies.AbilitySimulation.SprintBurst
{
    [Serializable]
    public struct SprintBurstConfig
    {
        [Header("Sprint Burst Settings")]
        public float maxBurstSpeed;
        public float minBurstSpeed;
        public float maxMeter;
        public float meterRecoverySpeed;
        public float timeUntilRecovery;
    }
}
