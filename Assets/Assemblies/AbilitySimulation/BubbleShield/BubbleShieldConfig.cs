using System;
using UnityEngine;

namespace Resonance.Assemblies.AbilitySimulation.BubbleShield
{
    [Serializable]
    public struct BubbleShieldConfig
    {
        [Header("Shield Settings")]
        public float lobForce;
        public float upwardLobBias;
        public float cooldown;
    }
}
