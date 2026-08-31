namespace Resonance.Assemblies.AbilitySimulation.BubbleShield
{
    /// <summary>
    /// All read-only context values for a single tick of the bubble shield ability.
    /// </summary>
    public struct BubbleShieldSimulationContext
    {
        public readonly AbilityBubbleShieldInput Input;
        public readonly float Delta;

        public BubbleShieldSimulationContext(
            in AbilityBubbleShieldInput input,
            float delta
        )
        {
            Input = input;
            Delta = delta;
        }
    }
}
