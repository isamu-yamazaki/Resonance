namespace Resonance.Assemblies.AbilitySimulation.BubbleShield
{
    /// <summary>
    /// All read-only context values for a single tick of the bubble shield ability.
    /// </summary>
    public struct BubbleShieldSimulationContext
    {
        public readonly AbilityBubbleShieldInput Input;
        public readonly BubbleShieldConfig Config;
        public readonly float Delta;

        public BubbleShieldSimulationContext(
            in AbilityBubbleShieldInput input,
            in BubbleShieldConfig config,
            float delta
        )
        {
            Input = input;
            Config = config;
            Delta = delta;
        }
    }
}
