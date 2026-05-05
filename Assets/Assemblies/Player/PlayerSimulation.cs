using System;
using UnityEngine;

namespace Resonance.Assemblies.Player
{
    public static class PlayerSimulation
    {
        private const float GrappleImpulseDecay = 10f;

        public static void Step(
            in PlayerInputData inputData,
            in PlayerDependencyData dependencyData,
            ref PlayerMovementDataState state,
            in PlayerConfig config,
            CharacterController characterController,
            LayerMask groundLayers,
            Vector3 trainFrameVelocityOffset,
            float trainKnockbackVertical,
            float delta
        )
        {
            TickCameraMovement(inputData, dependencyData, ref state, config, delta);
            TickVerticalMovement(inputData, dependencyData, ref state, config, delta);
            TickLateralMovement(
                inputData,
                dependencyData,
                ref state,
                config,
                characterController,
                groundLayers,
                trainFrameVelocityOffset,
                trainKnockbackVertical,
                delta
            );

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
            // think this should be it?
            state.CameraYaw += inputData.MovementInput.x;
        }

        public static void TickLateralMovement(
            in PlayerInputData inputData,
            in PlayerDependencyData dependencyData,
            ref PlayerMovementDataState state,
            in PlayerConfig config,
            CharacterController characterController,
            LayerMask groundLayers,
            Vector3 trainFrameVelocityOffset,
            float trainKnockbackVertical,
            float delta
        )
        {
            var stats = CalculateDerivedStats(dependencyData, config);

            bool isSliding = dependencyData.CurrentPlayerMovementState == PlayerMovementState.Sliding;

            if (isSliding)
            {
                HandleSlideMovement();
                return;
            }

            bool isSprinting = dependencyData.CurrentPlayerMovementState == PlayerMovementState.Sprinting;
            bool isGrounded = PlayerMovementStateUtils.IsStateGroundedState(dependencyData.CurrentPlayerMovementState);
            bool isCrouching = dependencyData.CurrentPlayerMovementState == PlayerMovementState.Crouching;

            // State dependent acceleration and speed
            float lateralAcceleration = !isGrounded ? stats.inAirAcceleration :
                                        isCrouching ? stats.crouchAcceleration :
                                        isSprinting ? stats.sprintAcceleration : stats.runAcceleration;
            float clampLateralMagnitude = !isGrounded ? stats.sprintSpeed :
                                          isCrouching ? stats.crouchSpeed :
                                          isSprinting ? stats.sprintSpeed : stats.runSpeed;

            float yawRad = state.CameraYaw * Mathf.Deg2Rad;
            Vector3 cameraForwardXZ = new Vector3(Mathf.Sin(yawRad), 0f, Mathf.Cos(yawRad));
            Vector3 cameraRightXZ = new Vector3(Mathf.Cos(yawRad), 0f, -Mathf.Sin(yawRad));
            Vector3 movementDirection = cameraRightXZ * inputData.MovementInput.x + cameraForwardXZ * inputData.MovementInput.y;

            Vector3 movementDelta = movementDirection * lateralAcceleration * delta;
            Vector3 localVelocity = characterController.velocity - trainFrameVelocityOffset;
            Vector3 newVelocity = localVelocity + movementDelta;

            // Add drag to player
            Vector3 currentDrag = newVelocity.normalized * stats.drag * delta;
            newVelocity = (newVelocity.magnitude > stats.drag * delta) ? newVelocity - currentDrag : Vector3.zero;
            newVelocity = Vector3.ClampMagnitude(new Vector3(newVelocity.x, 0f, newVelocity.z), clampLateralMagnitude);
            newVelocity.y = state.Velocity.y;
            newVelocity = !isGrounded ? HandleSteepWalls(newVelocity, state.Velocity.y, characterController, groundLayers) : newVelocity;

            // Move character (Unity suggests only calling this once per tick)
            state.Velocity.y += trainKnockbackVertical;
            newVelocity.y = state.Velocity.y;
            newVelocity += ConsumeImpulse(state);
            TickImpulse(ref state, delta);
            characterController.Move((newVelocity + trainFrameVelocityOffset) * delta);
        }

        private static void HandleSlideMovement()
        {
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

        private static Vector3 HandleSteepWalls(
            Vector3 velocity,
            float verticalVelocity,
            CharacterController characterController,
            LayerMask groundLayers
        )
        {
            Vector3 normal = GetGroundNormalWithSphereCast(characterController, groundLayers);
            float angle = Vector3.Angle(normal, Vector3.up);
            bool validAngle = angle <= characterController.slopeLimit;

            if (!validAngle && verticalVelocity < 0f)
                velocity = Vector3.ProjectOnPlane(velocity, normal);

            return velocity;
        }

        private static Vector3 GetGroundNormalWithSphereCast(CharacterController characterController, LayerMask layerMask)
        {
            Vector3 normal = Vector3.up;
            Vector3 center = characterController.transform.position + characterController.center;
            float distance = characterController.height / 2f + characterController.stepOffset + 0.01f;

            if (Physics.SphereCast(center, characterController.radius, Vector3.down, out RaycastHit hit, distance, layerMask))
            {
                normal = hit.normal;
            }
            return normal;
        }

        private static Vector3 ConsumeImpulse(in PlayerMovementDataState state)
        {
            return state.grappleImpulse;
        }

        private static void TickImpulse(ref PlayerMovementDataState state, float delta)
        {
            if (state.grappleImpulse.sqrMagnitude <= 0.001f)
            {
                state.grappleImpulse = Vector3.zero;
                return;
            }

            state.grappleImpulse = Vector3.MoveTowards(state.grappleImpulse, Vector3.zero, GrappleImpulseDecay * delta);
        }
    }
}
