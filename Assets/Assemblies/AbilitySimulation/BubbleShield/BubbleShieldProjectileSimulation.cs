namespace Resonance.Assemblies.AbilitySimulation.BubbleShield
{
    public class BubbleShieldProjectileSimulation
    {
        /// <summary>
        /// The projectile counts as descending (and therefore able to land) once its vertical
        /// velocity drops to or below this threshold. Matches the value previously hard-coded in
        /// the behaviour.
        /// </summary>
        public const float DescendVelocityThreshold = 0.01f;

        /// <summary>
        /// Per-tick simulation for the bubble shield projectile. While airborne it lands the
        /// projectile once it is descending and over ground; once landed it accumulates alive
        /// time and begins despawning when the shield's lifetime (minus the despawn animation)
        /// elapses. Does nothing once despawning has begun.
        /// </summary>
        public static void Step(in BubbleShieldProjectileSimulationContext ctx, ref BubbleShieldProjectileState state)
        {
            var config = ctx.Config;
            var delta = ctx.Delta;

            // Per-tick outputs, consumed each tick, never accumulated.
            state.ShouldFreezeBody = false;
            state.ShouldBeginDespawn = false;

            if (state.IsDespawning)
                return;

            if (!state.IsLanded)
            {
                bool descending = ctx.LinearVelocity.y <= DescendVelocityThreshold;
                if (descending && ctx.IsGrounded)
                {
                    state.IsLanded = true;
                    state.ShouldFreezeBody = true;
                }

                return;
            }

            state.AliveTime += delta;

            if (state.AliveTime >= config.shieldDuration - config.despawnAnimDuration)
                BeginDespawn(ref state);
        }

        /// <summary>
        /// Applies incoming damage and begins despawning when the shield's health is depleted.
        /// </summary>
        public static void ApplyDamage(ref BubbleShieldProjectileState state, float damage)
        {
            // Transient output for this call, consumed by the caller immediately afterwards.
            state.ShouldBeginDespawn = false;

            state.Health -= damage;

            if (state.Health <= 0f)
                BeginDespawn(ref state);
        }

        private static void BeginDespawn(ref BubbleShieldProjectileState state)
        {
            if (state.IsDespawning)
                return;

            state.IsDespawning = true;
            state.ShouldBeginDespawn = true;
        }
    }
}
