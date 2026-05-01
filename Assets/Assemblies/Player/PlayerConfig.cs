using System;
using UnityEngine;

namespace Resonance.Assemblies.Player
{
    /// <summary>
    /// Collection of non-changing properties that determine a basis for
    /// the player movement.
    /// </summary>
    [Serializable]
    public struct PlayerConfig
    {
        [Header("Base Movement")]
        public float baseCrouchAcceleration;
        public float baseCrouchSpeed;
        public float baseRunAcceleration;
        public float baseRunSpeed;
        public float baseSprintAcceleration;
        public float baseSprintSpeed;
        public float baseInAirAcceleration;
        public float baseDrag;
        public float gravity;
        public float terminalVelocity;
        public float jumpSpeed;
        public float movingThreshold;

		[Header("Slide Settings")]
        public float baseSlideSpeed;
        public float baseMinSlideSpeed;

		public float slideDuration;
        public float slideDeceleration;
        public float slopeAngleThreshold;
        public float uphillSlideDecelerationMultiplier;
        public float downhillSlideSpeedBoost;

        [Header("Animation")]
        public float playerModelRotationSpeed;
        public float rotateToTargetTime;

        [Header("Camera Settings")]
        public float lookSensitivityH;
        public float lookSensitivityV;
        public float lookLimitV;
        public float baseFOV;
        public float sprintFOV;
        public float overdriveFOV;
        public float fovTransitionSpeed;
    }
}
