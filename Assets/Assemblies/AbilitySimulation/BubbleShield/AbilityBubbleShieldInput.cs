using PurrNet.Prediction;
using UnityEngine;

namespace Resonance.Assemblies.AbilitySimulation.BubbleShield
{
    public struct AbilityBubbleShieldInput : IPredictedData
    {
        public bool ActivatePressed;
        public Vector3 SpawnPosition;
        public Vector3 LobDirection;

        public void Dispose() { }
    }
}
