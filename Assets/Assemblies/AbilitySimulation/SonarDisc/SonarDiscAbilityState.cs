using PurrNet.Prediction;
using UnityEngine;

namespace Resonance.Assemblies.AbilitySimulation.SonarDisc
{
    public struct SonarDiscAbilityState : IPredictedData<SonarDiscAbilityState>
    {
        public float Cooldown;
        public bool SpawnDiscNextTick;

        // Latest owner muzzle pose, mirrored from input each tick so SimulationOnly code can read it
        // outside the input frame.
        public Vector3 MuzzlePosition;
        public Vector3 MuzzleForward;

        /// <summary>
        /// Per-tick output: true on the tick a disc should be spawned. Consumed each tick by the
        /// owning behaviour, never accumulated.
        /// </summary>
        public bool ShouldSpawnDisc;

        /// <summary>
        /// Per-tick outputs: the muzzle pose the disc should spawn from this tick. Valid only when
        /// <see cref="ShouldSpawnDisc"/> is true.
        /// </summary>
        public Vector3 SpawnPosition;
        public Vector3 SpawnDirection;

        public void Dispose() { }
    }
}
