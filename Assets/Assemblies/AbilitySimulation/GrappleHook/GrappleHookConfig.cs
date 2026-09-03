using System;
using UnityEngine;
using UnityEngine.Serialization;

namespace Resonance.Assemblies.AbilitySimulation.GrappleHook
{
    [Serializable]
    public struct GrappleHookConfig
    {
        [Header("Grapple Settings")]
        public float animationDelay;
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