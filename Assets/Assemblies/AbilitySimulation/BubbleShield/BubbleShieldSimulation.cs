namespace Resonance.Assemblies.AbilitySimulation.BubbleShield
{
    public class BubbleShieldSimulation
    {
        /// <summary>
        /// Per-tick simulation for the bubble shield ability. Mirrors the owner aim into state,
        /// raises the cooldown and signals a spawn when activation is pressed, then ticks the
        /// cooldown down. Activation here is NOT cooldown-gated — the gate lives upstream in
        /// AbilityBubbleShield.ActivateAbilityExternal. The cooldown-gated path is TryActivate.
        /// </summary>
        public static void Step(in BubbleShieldSimulationContext ctx, ref AbilityBubbleShieldState state)
        {
            var input = ctx.Input;
            var config = ctx.Config;
            var delta = ctx.Delta;

            // Per-tick output, consumed each tick, never accumulated.
            state.ShouldSpawnShield = false;

            state.SpawnPosition = input.SpawnPosition;
            state.LobDirection = input.LobDirection;

            if (input.ActivatePressed)
            {
                state.Cooldown = config.cooldown;
                state.ShouldSpawnShield = true;
            }

            if (state.Cooldown > 0f)
                state.Cooldown -= delta;
        }

        /// <summary>
        /// Cooldown-gated activation used by the external [SimulationOnly] entry point.
        /// Returns true when activation succeeds (caller should spawn the shield).
        /// </summary>
        public static bool TryActivate(ref AbilityBubbleShieldState state, in BubbleShieldConfig config)
        {
            if (state.Cooldown > 0f)
                return false;

            state.Cooldown = config.cooldown;
            return true;
        }
    }
}
