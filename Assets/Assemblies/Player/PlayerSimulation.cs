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
            float delta
        )
        {
            var derivedStats = CalculateDerivedStats(dependencyData, config);

            TickCameraMovement(inputData, dependencyData, ref state, config, delta);
            TickVerticalMovement(inputData, dependencyData, ref state, derivedStats, config, delta);
            TickLateralMovement(
                inputData,
                dependencyData,
                ref state,
                derivedStats,
                config,
                characterController,
                delta
            );
            TickCharacterControllerMovement(dependencyData, state, characterController, delta);

            state.LastSimulatedMovementState = dependencyData.CurrentPlayerMovementState;
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
            in PlayerDerivedStats derivedStats,
            in PlayerConfig config,
            CharacterController characterController,
            float delta
        )
        {
            bool isSliding = dependencyData.CurrentPlayerMovementState == PlayerMovementState.Sliding;

            if (isSliding)
            {
                HandleSlideMovement(
                    inputData,
                    dependencyData,
                    ref state,
                    derivedStats,
                    config,
                    characterController,
                    delta
                );
                return;
            }

            bool isSprinting = dependencyData.CurrentPlayerMovementState == PlayerMovementState.Sprinting;
            bool isGrounded = PlayerMovementStateUtils.IsStateGroundedState(dependencyData.CurrentPlayerMovementState);
            bool isCrouching = dependencyData.CurrentPlayerMovementState == PlayerMovementState.Crouching;

            // State dependent acceleration and speed
            float lateralAcceleration = !isGrounded ? derivedStats.inAirAcceleration :
                                        isCrouching ? derivedStats.crouchAcceleration :
                                        isSprinting ? derivedStats.sprintAcceleration : derivedStats.runAcceleration;
            float clampLateralMagnitude = !isGrounded ? derivedStats.sprintSpeed :
                                          isCrouching ? derivedStats.crouchSpeed :
                                          isSprinting ? derivedStats.sprintSpeed : derivedStats.runSpeed;

            float yawRad = state.CameraYaw * Mathf.Deg2Rad;
            Vector3 cameraForwardXZ = new Vector3(Mathf.Sin(yawRad), 0f, Mathf.Cos(yawRad));
            Vector3 cameraRightXZ = new Vector3(Mathf.Cos(yawRad), 0f, -Mathf.Sin(yawRad));
            Vector3 movementDirection = cameraRightXZ * inputData.MovementInput.x + cameraForwardXZ * inputData.MovementInput.y;

            Vector3 movementDelta = movementDirection * lateralAcceleration * delta;
            Vector3 localVelocity = characterController.velocity - dependencyData.trainVelocityOffset;
            Vector3 newVelocity = localVelocity + movementDelta;

            // Add drag to player
            Vector3 currentDrag = newVelocity.normalized * derivedStats.drag * delta;
            newVelocity = (newVelocity.magnitude > derivedStats.drag * delta) ? newVelocity - currentDrag : Vector3.zero;
            newVelocity = Vector3.ClampMagnitude(new Vector3(newVelocity.x, 0f, newVelocity.z), clampLateralMagnitude);
            newVelocity.y = state.Velocity.y;
            newVelocity = !isGrounded ? HandleSteepWalls(newVelocity, state.Velocity.y, characterController, dependencyData.groundLayers) : newVelocity;

            newVelocity += ConsumeImpulse(state);
            newVelocity.y = state.Velocity.y;

            state.Velocity = newVelocity;
            state.Velocity.y += dependencyData.trainKnockbackVertical;

            TickImpulse(ref state, delta);
        }

        private static void HandleSlideMovement(
            in PlayerInputData inputData,
            in PlayerDependencyData dependencyData,
            ref PlayerMovementDataState state,
            in PlayerDerivedStats derivedStats,
            in PlayerConfig config,
            CharacterController characterController,
            float delta
        )
        {
            // we'll do this in a bit
            Vector3 groundNormal = CharacterControllerUtils.GetNormalWithSphereCast(characterController, dependencyData.groundLayers);
            float slopeAngle = Vector3.Angle(groundNormal, Vector3.up);

            Vector3 slopeDownDirection = Vector3.ProjectOnPlane(Vector3.down, groundNormal).normalized;
    
            Vector3 slideDirection = new Vector3(characterController.velocity.x, 0f, characterController.velocity.z).normalized;
            float slopeDot = Vector3.Dot(slideDirection, slopeDownDirection);
    
            bool isDownhill = slopeAngle > config.slopeAngleThreshold && slopeDot > 0.1f;
            bool isUphill = slopeAngle > config.slopeAngleThreshold && slopeDot < -0.1f;
    
            // Update slide timer based on slope
            if (isDownhill)
            {
                state.SlideTimer -= delta * 0.5f; // Slower decay on downhill
            }
            else if (isUphill)
            {
                state.SlideTimer -= delta * config.uphillSlideDecelerationMultiplier;
            }
            else
            {
                state.SlideTimer -= delta;
            }
    
            // Calculate slide speed
            float slideProgress = 1f - (state.SlideTimer / config.slideDuration);
            float currentSlideSpeed = Mathf.Lerp(derivedStats.slideSpeed, derivedStats.minSlideSpeed, slideProgress);
    
            // Apply slope modifications to speed
            if (isDownhill)
            {
                currentSlideSpeed *= config.downhillSlideSpeedBoost;
            }
            else if (isUphill)
            {
                currentSlideSpeed = Mathf.Max(currentSlideSpeed - (config.slideDeceleration * config.uphillSlideDecelerationMultiplier * delta), derivedStats.minSlideSpeed);
            }
            else
            {
                currentSlideSpeed = Mathf.Max(currentSlideSpeed - (config.slideDeceleration * Time.deltaTime), derivedStats.minSlideSpeed);
            }
            
            // Apply Overdrive speed multiplier to slide
            if (dependencyData.IsInOverdrive)
            {
                currentSlideSpeed *= dependencyData.OverdriveSpeedMultiplier;
            }
    
            // End slide when timer expires
            if (state.SlideTimer <= 0f)
            {
                // TODO: make this flag do something
                // _playerLocomotionInput.DisableCrouch();
                return;
            }
    
            // Move in locked slide direction
            Vector3 slideVelocity = slideDirection * currentSlideSpeed;
            slideVelocity.y = state.Velocity.y;
            state.Velocity = slideVelocity;
    
            // Vector3 trainOffset = _trainPassengerPhysics != null ? _trainPassengerPhysics.GetFrameVelocityOffset() : Vector3.zero;
        }

        public static void TickVerticalMovement(
            in PlayerInputData inputData,
            in PlayerDependencyData dependencyData,
            ref PlayerMovementDataState state,
            in PlayerDerivedStats derivedStats,
            in PlayerConfig config,
            float delta
        )
        {
            var verticalVelocity = state.Velocity.y;

            // TODO: forward predict PlayerState (aka run it as part of a forward predicted loop)
            // Right now it relies on server propagation which causes a delay
            var isGrounded = PlayerMovementStateUtils.IsStateGroundedState(dependencyData.CurrentPlayerMovementState);
            verticalVelocity -= config.gravity * delta;

            if (isGrounded && verticalVelocity < 0f)
            {
                verticalVelocity = -derivedStats.antiBump;
                state.GrappleImpulse = Vector3.zero;
            }

            if (inputData.JumpPressed && isGrounded)
            {
                verticalVelocity += Mathf.Sqrt(config.jumpSpeed * 3 * config.gravity);
                state.JumpedLastSimulatedFrame = true;
            }

            // TODO: need to double check if this is the correct behavior
            if (PlayerMovementStateUtils.IsStateGroundedState(state.LastSimulatedMovementState) && !isGrounded)
            {
                verticalVelocity += derivedStats.antiBump;
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
            return state.GrappleImpulse;
        }

        private static void TickImpulse(ref PlayerMovementDataState state, float delta)
        {
            if (state.GrappleImpulse.sqrMagnitude <= 0.001f)
            {
                state.GrappleImpulse = Vector3.zero;
                return;
            }

            state.GrappleImpulse = Vector3.MoveTowards(state.GrappleImpulse, Vector3.zero, GrappleImpulseDecay * delta);
        }

        private static void TickCharacterControllerMovement(
            in PlayerDependencyData dependencyData,
            in PlayerMovementDataState state,
            in CharacterController characterController,
            float delta
        )
        {
            characterController.Move((state.Velocity + dependencyData.trainVelocityOffset) * delta);
        }

    }
}
