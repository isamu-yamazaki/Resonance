using System;
using UnityEngine;

namespace Resonance.Assemblies.Player
{
    public static class PlayerSimulation
    {
        // TODO: add the other necessary dependencies
        public static void Step(
            in PlayerInputData inputData,
            ref PlayerMovementDataState state,
            in PlayerConfig config,
            CharacterController characterController,
            float delta
        )
        {
            TickCameraMovement(inputData, ref state, config, delta);
            TickVerticalMovement(inputData, ref state, config, delta);
            TickLateralMovement(inputData, ref state, config, characterController, delta);
        }

        public static void TickCameraMovement(
            in PlayerInputData inputData,
            ref PlayerMovementDataState state,
            in PlayerConfig config,
            float delta
        )
        {
            throw new NotImplementedException();
        }

        public static void TickLateralMovement(
            in PlayerInputData inputData,
            ref PlayerMovementDataState state,
            in PlayerConfig config,
            CharacterController characterController,
            float delta
        )
        {
            var stats = CalculateDerivedStats(inputData, config);
            // TODO: lateral movement math (sprint/run/crouch acc + drag + slide branch)
            //       will consume `stats` here.
            throw new NotImplementedException();
        }

        public static void TickVerticalMovement(
            in PlayerInputData inputData,
            ref PlayerMovementDataState state,
            in PlayerConfig config,
            float delta
        )
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Derive per-tick movement stats from the input multiplier and the immutable config.
        /// Mirrors legacy PlayerController.UpdateStats().
        /// </summary>
        public static PlayerDerivedStats CalculateDerivedStats(
            in PlayerInputData inputData,
            in PlayerConfig config
        )
        {
            float m = inputData.MovementSpeedMultiplier;

            var stats = new PlayerDerivedStats
            {
                crouchSpeed = config.baseCrouchSpeed * m,
                runSpeed = config.baseRunSpeed * m,
                sprintSpeed = config.baseSprintSpeed * m,
                slideSpeed = config.baseSlideSpeed * m,
                minSlideSpeed = config.baseMinSlideSpeed * m,

                crouchAcceleration = config.baseCrouchAcceleration * m,
                runAcceleration = config.baseRunAcceleration * m,
                sprintAcceleration = config.baseSprintAcceleration * m,
                inAirAcceleration = config.baseInAirAcceleration * m,

                drag = config.baseDrag * m,
            };
            stats.antiBump = stats.sprintSpeed;
            return stats;
        }
    }
}
