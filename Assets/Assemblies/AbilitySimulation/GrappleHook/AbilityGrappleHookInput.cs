using PurrNet.Prediction;
using UnityEngine;

namespace Resonance.Assemblies.AbilitySimulation.GrappleHook
{
    public struct AbilityGrappleHookInput : IPredictedData
    {
        public bool JumpPressed;

        /// <summary>Owner camera pose this tick, forwarded into state so SimulationOnly code can read it.</summary>
        public Vector3 CameraPosition;
        public Vector3 CameraForward;
        
        /// <summary>
        /// Owner transform position. Determines where the grapple fires from.
        /// </summary>
        public Vector3 LocalTransformPosition;

        public void Dispose() { }
    }
}