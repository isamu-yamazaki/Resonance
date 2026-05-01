using System.Numerics;

namespace Resonance.Assemblies.Player
{
    public struct PlayerMovementDataState
    {
        public Vector3 Velocity;

        /// <summary>
        /// The x-component of the camera rotation.
        /// Modified deterministically each tick by PlayerInputData.LookInput.
        /// </summary>
        public float CameraYaw;
    }
}
