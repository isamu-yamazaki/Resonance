using UnityEngine;

namespace Resonance.Assemblies.AbilitySimulation.SprintBurst
{
    public class SprintBurstSimulation
    {
        /// <summary>
        /// Per-tick simulation for the sprint burst ability. While sprinting it drains the burst meter
        /// and computes the desired speed modifier into <see cref="AbilitySprintBurstState.LastAppliedSpeedMod"/>
        /// (the boost lerps from max at a full meter down to min at an empty meter); while idle it
        /// recovers the meter after a delay and clears the modifier. This is pure: the owning
        /// AbilitySprintBurst behaviour syncs LastAppliedSpeedMod to PlayerStats outside Step.
        /// </summary>
        public static void Step(in SprintBurstSimulationContext ctx, ref AbilitySprintBurstState state)
        {
            if (state.WasSprinting && !ctx.Input.Sprinting)
            {
                JustStoppedSprinting(ref state);
            }
            else if (ctx.Input.Sprinting)
            {
                Sprinting(in ctx, ref state);
            }
            else
            {
                NotSprinting(in ctx, ref state);
            }
        }

        private static void JustStoppedSprinting(ref AbilitySprintBurstState state)
        {
            state.LastAppliedSpeedMod = 0f;
            state.TimeSinceLastSprinting = 0f;
            state.WasSprinting = false;
        }

        private static void Sprinting(in SprintBurstSimulationContext ctx, ref AbilitySprintBurstState state)
        {
            var config = ctx.Config;

            state.CurrentMeter = Mathf.Clamp(state.CurrentMeter - ctx.Delta, 0f, config.maxMeter);
            state.LastAppliedSpeedMod =
                Mathf.Lerp(config.minBurstSpeed, config.maxBurstSpeed, state.CurrentMeter / config.maxMeter);

            state.WasSprinting = true;
        }

        private static void NotSprinting(in SprintBurstSimulationContext ctx, ref AbilitySprintBurstState state)
        {
            var config = ctx.Config;

            state.TimeSinceLastSprinting += ctx.Delta;

            if (state.TimeSinceLastSprinting >= config.timeUntilRecovery)
            {
                state.CurrentMeter = Mathf.Clamp(
                    state.CurrentMeter + (ctx.Delta * config.meterRecoverySpeed), 0f, config.maxMeter);
            }

            state.LastAppliedSpeedMod = 0f;
            state.WasSprinting = false;
        }
    }
}
