using PurrNet.Prediction;

namespace Resonance.Assemblies.AbilitySimulation.BubbleShield
{
    public struct BubbleShieldProjectileState : IPredictedData<BubbleShieldProjectileState>
    {
        public float AliveTime;
        public bool IsDespawning;
        public bool IsLanded;
        public float Health;

        /// <summary>
        /// Per-tick output: true on the tick the projectile lands and its body should be frozen.
        /// Consumed each tick by the owning behaviour, never accumulated.
        /// </summary>
        public bool ShouldFreezeBody;

        /// <summary>
        /// Per-tick output: true on the tick the projectile begins despawning and the despawn
        /// animation/coroutine should start. Consumed each tick, never accumulated.
        /// </summary>
        public bool ShouldBeginDespawn;

        public void Dispose() { }
    }
}
