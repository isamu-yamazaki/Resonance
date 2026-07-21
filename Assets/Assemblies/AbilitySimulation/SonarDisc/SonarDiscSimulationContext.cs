namespace Resonance.Assemblies.AbilitySimulation.SonarDisc
{
    /// <summary>
    /// All read-only context values for a single tick of the sonar disc ability.
    /// </summary>
    public struct SonarDiscSimulationContext
    {
        public readonly SonarDiscAbilityInput Input;
        public readonly SonarDiscConfig Config;
        public readonly float Delta;

        public SonarDiscSimulationContext(
            in SonarDiscAbilityInput input,
            in SonarDiscConfig config,
            float delta
        )
        {
            Input = input;
            Config = config;
            Delta = delta;
        }
    }
}
