using UnityEngine;

namespace Resonance.Assemblies.AbilitySimulation.BubbleShield
{
    /// <summary>
    /// All read-only context values for a single tick of the bubble shield projectile, including
    /// the physics-derived dependencies the simulation needs. The body's velocity is read from the
    /// predicted rigidbody and the ground-probe result from a sphere cast against the predicted
    /// physics scene; both are resolved by the owning behaviour and passed in here, keeping the
    /// simulation pure and deterministic under rollback/replay.
    /// </summary>
    public struct BubbleShieldProjectileSimulationContext
    {
        public readonly BubbleShieldProjectileConfig Config;
        public readonly float Delta;

        /// <summary>
        /// Predicted rigidbody velocity this tick. The simulation derives the descent check from
        /// its Y component.
        /// </summary>
        public readonly Vector3 LinearVelocity;

        /// <summary>
        /// Result of the downward ground probe this tick. Irreducibly external — a physics-scene
        /// sphere cast — so it is resolved by the behaviour rather than the simulation.
        /// </summary>
        public readonly bool IsGrounded;

        public BubbleShieldProjectileSimulationContext(
            in BubbleShieldProjectileConfig config,
            float delta,
            Vector3 linearVelocity,
            bool isGrounded
        )
        {
            Config = config;
            Delta = delta;
            LinearVelocity = linearVelocity;
            IsGrounded = isGrounded;
        }
    }
}
