namespace Resonance.Assemblies.AbilitySimulation.SonarDisc
{
    public class SonarDiscSimulation
    {
        /// <summary>
        /// Per-tick simulation for the sonar disc ability. Ticks the cooldown down, spawns a disc on
        /// the tick after activation is pressed (resetting the cooldown), and mirrors the owner muzzle
        /// pose into state so SimulationOnly code can read it outside the input frame. Activation here
        /// is NOT cooldown-gated — the gate lives upstream in SonarDiscAbility.ActivateAbilityExternal.
        /// </summary>
        public static void Step(in SonarDiscSimulationContext ctx, ref SonarDiscAbilityState state)
        {
            var input = ctx.Input;
            var config = ctx.Config;
            var delta = ctx.Delta;

            // Per-tick output, consumed each tick, never accumulated.
            state.ShouldSpawnDisc = false;

            if (state.Cooldown > 0f)
                state.Cooldown -= delta;

            if (state.SpawnDiscNextTick)
            {
                state.Cooldown = config.cooldown;
                state.ShouldSpawnDisc = true;

                // Spawn from the muzzle pose mirrored on the previous tick — matches the original
                // behaviour, which fired before mirroring this tick's input. Captured here so the
                // behaviour spawns from a stable, replay-safe value.
                state.SpawnPosition = state.MuzzlePosition;
                state.SpawnDirection = state.MuzzleForward;

                state.SpawnDiscNextTick = false;
            }

            // Mirror the owner muzzle pose into state for the next tick's spawn / SimulationOnly reads.
            state.MuzzleForward = input.MuzzleForward;
            state.MuzzlePosition = input.MuzzlePosition;
        }
    }
}
