using PurrNet.Prediction;
using UnityEngine;

namespace Resonance.Assemblies.AbilitySimulation.SonarDisc
{
    public struct SonarDiscAbilityInput : IPredictedData
    {
        public bool ActivatePressed;
        public Vector3 MuzzleForward;
        public Vector3 MuzzlePosition;

        public void Dispose() { }
    }
}
