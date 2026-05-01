using UnityEngine;

namespace Resonance.Assemblies.Player
{
    public struct PlayerInputData
    {
        public Vector2 MovementInput { get; set; }
        public Vector2 LookInput { get; set; }
        public bool JumpPressed { get; set; }
        public bool SprintToggledOn { get; set; }
        public bool CrouchToggledOn { get; set; }
    }
}
