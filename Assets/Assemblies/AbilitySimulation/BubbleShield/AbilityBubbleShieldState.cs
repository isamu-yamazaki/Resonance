using PurrNet.Prediction;
using UnityEngine;

namespace Resonance.Assemblies.AbilitySimulation.BubbleShield
{
    public struct AbilityBubbleShieldState : IPredictedData<AbilityBubbleShieldState>
    {
        public float Cooldown;
        public Vector3 LobDirection;
        public Vector3 SpawnPosition;

        /// <summary>
        /// Per-tick output: true on the tick the simulation decides a shield should be spawned.
        /// Consumed each tick by the owning behaviour, never accumulated.
        /// </summary>
        public bool ShouldSpawnShield;

        public void Dispose() { }
    }
}
