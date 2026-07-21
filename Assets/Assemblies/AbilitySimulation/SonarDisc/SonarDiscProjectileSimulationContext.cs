using UnityEngine;

namespace Resonance.Assemblies.AbilitySimulation.SonarDisc
{
    /// <summary>
    /// All read-only context values for a single tick of the sonar disc projectile. The swept-raycast
    /// collision test, the overlap-sphere scan and the rigidbody writes are irreducibly external, so
    /// they are resolved by the owning behaviour; only the projectile's reconciled position is fed in
    /// here for the travel/range step, keeping the simulation pure and deterministic under
    /// rollback/replay.
    /// </summary>
    public struct SonarDiscProjectileSimulationContext
    {
        public readonly SonarDiscProjectileConfig Config;
        public readonly float Delta;

        /// <summary>
        /// The projectile's reconciled position this tick. Read by the travel step to accumulate
        /// distance travelled and advance the swept-raycast origin.
        /// </summary>
        public readonly Vector3 CurrentPosition;

        public SonarDiscProjectileSimulationContext(
            in SonarDiscProjectileConfig config,
            float delta,
            Vector3 currentPosition
        )
        {
            Config = config;
            Delta = delta;
            CurrentPosition = currentPosition;
        }
    }
}
