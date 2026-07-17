using PurrNet.Prediction;
using UnityEngine;

namespace Resonance.Assemblies.AbilitySimulation.GrappleHook
{
    public struct AbilityGrappleHookState : IPredictedData<AbilityGrappleHookState>
    {
        public bool GrappleNextTick;
        public bool IsGrappling;
        public Vector3 HookPoint;
        public float ReelTime;

        /// <summary>Reel velocity computed this tick (zero when not grappling).</summary>
        public Vector3 ReelVelocity;

        /// <summary>Exit boost emitted on the tick an early exit occurs (zero otherwise).</summary>
        public Vector3 ExitImpulse;

        /// <summary>Latest owner camera pose, mirrored from input each tick so SimulationOnly code can read it.</summary>
        public Vector3 CameraPosition;
        public Vector3 CameraForward;

        public float Cooldown;

        public bool BroadcastShootAndTravel;
        public bool BroadcastGrappleRegistration;
        public bool BroadcastStopTravel;
        public bool BroadcastRelease;
        public Vector3 GrappleRegistrationPosition;

        public void Dispose() { }
    }
}