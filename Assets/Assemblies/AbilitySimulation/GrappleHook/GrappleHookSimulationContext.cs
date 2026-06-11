using UnityEngine;

namespace Resonance.Assemblies.AbilitySimulation.GrappleHook
{
    /// <summary>
    /// All read-only context values for a single tick of the grapple hook.
    /// </summary>
    public struct GrappleHookSimulationContext
    {
        public readonly GrappleHookConfig Config;
        public readonly AbilityGrappleHookInput Input;
        public readonly float Delta;

        public GrappleHookSimulationContext(
            in AbilityGrappleHookInput input,
            in GrappleHookConfig config,
            Vector3 transformPosition,
            float delta
            )
        {
            Config = config;
            Input = input;
            Delta = delta;
        }
    }
}