namespace Resonance.Assemblies.AbilitySimulation.BubbleShield
{
    public class BubbleShieldSimulation
    {
        /// <summary>
        /// Per-tick simulation for the bubble shield ability. Note that simulation
        /// also happens in the AbilityBubbleShield.SimulateActivateAbility method.
        /// </summary>
        public static void Step(in BubbleShieldSimulationContext ctx, ref AbilityBubbleShieldState state)
        {
            var input = ctx.Input;
            var delta = ctx.Delta;

            // Per-tick output, consumed each tick, never accumulated.
            state.ShouldSpawnShield = false;

            state.SpawnPosition = input.SpawnPosition;
            state.LobDirection = input.LobDirection;

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
