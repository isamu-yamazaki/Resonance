using UnityEngine;

namespace Resonance.Assemblies.Player
{
    /// <summary>
    /// All read-only inputs to a single PlayerSimulation tick. Passed by `in` so the
    /// compiler hands a pointer to stack memory — zero copies, zero heap allocation.
    /// State is intentionally not in this struct: it's mutated each tick and passed
    /// separately by `ref`.
    /// </summary>
    public readonly struct PlayerSimulationContext
    {
        public readonly PlayerInputData Input;
        public readonly PlayerDependencyData Dependency;
        public readonly PlayerConfig Config;
        public readonly CharacterController CharacterController;
        public readonly float Delta;

        public PlayerSimulationContext(
            in PlayerInputData input,
            in PlayerDependencyData dependency,
            in PlayerConfig config,
            CharacterController characterController,
            float delta)
        {
            Input = input;
            Dependency = dependency;
            Config = config;
            CharacterController = characterController;
            Delta = delta;
        }
    }
}
