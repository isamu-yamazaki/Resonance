using System;
using UnityEngine;

namespace Resonance.Assemblies.Player
{
    public static class PlayerSimulation
    {
        // TODO: add the other necessary dependencies
        public static void Step(
            in PlayerInputData inputData,
            in PlayerDependencyData dependencyData,
            ref PlayerMovementDataState state,
            in PlayerConfig config,
            CharacterController characterController,
            float delta
        )
        {
            TickCameraMovement(inputData, dependencyData, ref state, config, delta);
            TickVerticalMovement(inputData, dependencyData, ref state, config, delta);
            TickLateralMovement(inputData, dependencyData, ref state, config, characterController, delta);

            state.lastSimulatedMovementState = dependencyData.CurrentPlayerMovementState;

        }

        public static void TickCameraMovement(
            in PlayerInputData inputData,
            in PlayerDependencyData dependencyData,
            ref PlayerMovementDataState state,
            in PlayerConfig config,
            float delta
        )
        {
            throw new NotImplementedException();
        }

        public static void TickLateralMovement(
            in PlayerInputData inputData,
            in PlayerDependencyData dependencyData,
            ref PlayerMovementDataState state,
            in PlayerConfig config,
            CharacterController characterController,
            float delta
        )
        {
            var stats = CalculateDerivedStats(dependencyData, config);
            // TODO: lateral movement math (sprint/run/crouch acc + drag + slide branch)
            //       will consume `stats` here.
            throw new NotImplementedException();
        }

        public static void TickVerticalMovement(
            in PlayerInputData inputData,
            in PlayerDependencyData dependencyData,
            ref PlayerMovementDataState state,
            in PlayerConfig config,
            float delta
        )
        {
            var derived = CalculateDerivedStats(dependencyData, config);
            var verticalVelocity = state.Velocity.y;

            // TODO: forward predict PlayerState (aka run it as part of a forward predicted loop)
            // Right now it relies on server propagation which causes a delay
            var isGrounded = PlayerMovementStateUtils.IsStateGroundedState(dependencyData.CurrentPlayerMovementState);
            verticalVelocity -= config.gravity * delta;

            if (isGrounded && verticalVelocity < 0f)
            {
                verticalVelocity = -derived.antiBump;
                state.grappleImpulse = Vector3.zero;
            }

            if (inputData.JumpPressed && isGrounded)
            {
                verticalVelocity += Mathf.Sqrt(config.jumpSpeed * 3 * config.gravity);
                state.jumpedLastSimulatedFrame = true;
            }

            // TODO: need to double check if this is the correct behavior
            if (PlayerMovementStateUtils.IsStateGroundedState(state.lastSimulatedMovementState) && !isGrounded)
            {
                verticalVelocity += derived.antiBump;
            }

            if (Mathf.Abs(verticalVelocity) > Mathf.Abs(config.terminalVelocity))
            {
                verticalVelocity = -1f * Mathf.Abs(config.terminalVelocity);
            }

            state.Velocity.y = verticalVelocity;
        }

        /// <summary>
        /// Derive per-tick movement stats from the synced dependency snapshot
        /// and the immutable config. Mirrors legacy PlayerController.UpdateStats().
        /// </summary>
        public static PlayerDerivedStats CalculateDerivedStats(
            in PlayerDependencyData dependencyData,
            in PlayerConfig config
        )
        {
            float m = dependencyData.MovementSpeedMultiplier;

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
