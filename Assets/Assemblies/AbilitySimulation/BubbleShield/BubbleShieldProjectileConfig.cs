using System;
using UnityEngine;

namespace Resonance.Assemblies.AbilitySimulation.BubbleShield
{
    [Serializable]
    public struct BubbleShieldProjectileConfig
    {
        [Header("Shield Settings")]
        public float shieldHealth;
        public float shieldDuration;

        [Header("Despawn Timing")]
        public float despawnAnimDuration;

        [Header("Landing Detection")]
        // Layers the shield counts as "ground". Static geometry only — never predicted
        // identities — so the probe stays deterministic under rollback.
        public LayerMask groundMask;
        // Radius of the downward sweep. Usually ~the projectile's collider radius.
        public float groundProbeRadius;
        // How far below the sphere surface still counts as a landing. Larger = lands
        // slightly earlier (less bounce/roll before the gameplay state flips).
        public float groundProbeDistance;
    }
}
