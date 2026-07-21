using System;
using UnityEngine;

namespace Resonance.Assemblies.AbilitySimulation.SonarDisc
{
    [Serializable]
    public struct SonarDiscProjectileConfig
    {
        [Header("Travel")]
        public float travelSpeed;
        public float maxRange;

        [Header("Pulse")]
        public float pulseDelay;
        public float pulseRadius;
        public float pulseExpandDuration;
    }
}
