using PurrNet.Prediction;
using UnityEngine;

namespace Resonance.Assemblies.Player
{
    public struct PlayerInputData : IPredictedData
    {
        public Vector2 MovementInput { get; set; }
        public bool JumpPressed { get; set; }
        public bool SprintToggledOn { get; set; }
        public bool CrouchToggledOn { get; set; }
        public float LookYawDelta { get; set; }

        public void Dispose() { }
    }
}
