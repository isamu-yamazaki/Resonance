using System;
using UnityEngine;

namespace Resonance.Assemblies.AbilitySimulation.GrappleHook
{
    [Serializable]
    public struct GrappleHookConfig
    {
        [Header("Grapple Settings")]
        public float maxRange;
        public float reelSpeed;
        public float maxReelTime;
        public float exitBoost;
        public float upwardBias;
        public float cooldown;
        
        [Header("References")]
        public LayerMask grappleLayerMask;
    }
}