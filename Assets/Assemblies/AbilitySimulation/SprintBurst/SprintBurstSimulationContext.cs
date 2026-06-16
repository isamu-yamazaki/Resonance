namespace Resonance.Assemblies.AbilitySimulation.SprintBurst
{
    /// <summary>
    /// All read-only context values for a single tick of the sprint burst ability.
    /// </summary>
    public struct SprintBurstSimulationContext
    {
        public readonly AbilitySprintBurstInput Input;
        public readonly SprintBurstConfig Config;
        public readonly float Delta;

        public SprintBurstSimulationContext(
            in AbilitySprintBurstInput input,
            in SprintBurstConfig config,
            float delta
        )
        {
            Input = input;
            Config = config;
            Delta = delta;
        }
    }
}
