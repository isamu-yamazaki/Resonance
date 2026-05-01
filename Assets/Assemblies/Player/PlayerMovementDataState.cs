
using UnityEngine;

namespace Resonance.Assemblies.Player
{
    /// <summary>
    /// The simulated movement state of the player.
    /// </summary>
    public struct PlayerMovementDataState
    {
        public Vector3 Velocity;

        /// <summary>
        /// The x-component of the camera rotation.
        /// Modified deterministically each tick by PlayerInputData.LookInput.
        /// </summary>
        public float CameraYaw;

        // Persistent values for deriving velocity later
        public Vector3 grappleImpulse;
        public bool jumpedLastSimulatedFrame;
        public PlayerMovementState lastSimulatedMovementState;
    }
}
