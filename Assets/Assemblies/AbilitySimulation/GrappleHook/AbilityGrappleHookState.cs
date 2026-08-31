using PurrNet.Prediction;
using UnityEngine;

namespace Resonance.Assemblies.AbilitySimulation.GrappleHook
{
    public enum GrappleStatus
    {
        None,
        /// <summary>
        /// Player has activated the ability, but it is pending (animation timing).
        /// </summary>
        PendingWithDelay,
        Grappling,
    }

    public struct AbilityGrappleHookState : IPredictedData<AbilityGrappleHookState>
    {
        #region General grapple state

        public GrappleStatus GrappleStatus;
        public bool IsGrappling => GrappleStatus == GrappleStatus.Grappling;

        #endregion

        #region Pre-grapple and pending state

        public bool StartGrappleSequenceNextTick;
        public float PendingTime;

        #endregion

        #region Grapple state

        public Vector3 HookPoint;
        public float ReelTime;
        public Vector3 ReelVelocityThisTick;

        #endregion

        #region Post-grapple state

        /// <summary>Exit boost emitted on the tick an early exit occurs.</summary>
        public Vector3 ExitImpulse;
        public float Cooldown;

        #endregion

        #region Input forwarded data

        /// <summary>Latest owner camera pose, mirrored from input each tick so SimulationOnly code can read it.</summary>
        public Vector3 CameraPosition;

        /// <summary>
        /// Latest owner camera forward vector, mirrored from input each tick so SimulationOnly code can read it.
        /// </summary>
        public Vector3 CameraForward;

        #endregion

        #region Audio flags for the client

        public bool BroadcastShootAndTravel;
        public bool BroadcastGrappleRegistration;
        public bool BroadcastStopTravel;
        public bool BroadcastRelease;
        public Vector3 GrappleRegistrationPosition;

        #endregion

        public void Dispose() { }
    }
}