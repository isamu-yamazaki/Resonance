using PurrNet.Prediction;
using UnityEngine;

namespace Resonance.Assemblies.AbilitySimulation.GrappleHook
{
    public struct AbilityGrappleHookInput : IPredictedData
    {
        public bool ActivatePressed;
        public Vector3 HookPoint;
        public bool JumpPressed;

        /// <summary>Owner camera pose this tick, forwarded into state so SimulationOnly code can read it.</summary>
        public Vector3 CameraPosition;
        public Vector3 CameraForward;

        public void Dispose() { }
    }
}