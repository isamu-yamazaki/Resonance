using PurrNet.Prediction;
using UnityEngine;

namespace Resonance.Assemblies.Player
{
    /// <summary>
    /// The simulated movement state of the player.
    /// </summary>
    public struct PlayerMovementDataState : IPredictedData<PlayerMovementDataState>
    {
        public Vector3 Position;
        public Vector3 Velocity;

        /// <summary>
        /// The x-component of the camera rotation.
        /// Modified deterministically each tick by PlayerInputData.LookInput.
        /// </summary>
        public float CameraYaw;

        // Persistent values for deriving velocity later
        public Vector3 GrappleImpulse;
        public bool JumpedLastSimulatedFrame;
        public bool WasGroundedLastTick;
        
        /// <summary>
        /// Must be propagated back to PlayerState by the controller to work with other systems
        /// </summary>
        public PlayerMovementState SimulatedMovementStateResult;
        
        public float SlideTimer;

        public void Dispose() { }

        // IMath<T> Add/Negate/Scale: leave at the throwing default-interface impls.
        // PlayerPredictedController overrides Interpolate per-field instead.
    }
}
