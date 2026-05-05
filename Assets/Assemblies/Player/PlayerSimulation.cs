using UnityEngine;

namespace Resonance.Assemblies.Player
{
    public static class PlayerSimulation
    {
        private const float GrappleImpulseDecay = 10f;

        public static void Step(in PlayerSimulationContext ctx, ref PlayerMovementDataState state)
        {
            TickCameraMovement(ctx, ref state);
            TickVerticalMovement(ctx, ref state);
            TickLateralMovement(ctx, ref state);
            TickCharacterControllerMovement(ctx, ref state);

            state.LastSimulatedMovementState = ctx.Dependency.CurrentPlayerMovementState;
        }

        public static void TickCameraMovement(in PlayerSimulationContext ctx, ref PlayerMovementDataState state)
        {
            state.CameraYaw += ctx.Input.LookInput.x * ctx.Config.lookSensitivityH;
        }

        public static void TickLateralMovement(in PlayerSimulationContext ctx, ref PlayerMovementDataState state)
        {
            bool isSliding = state.SlideTimer > 0f;

            if (isSliding)
            {
                HandleSlideMovement(ctx, ref state);
                return;
            }

            var derivedStats = CalculateDerivedStats(ctx.Dependency, ctx.Config);

            bool isGrounded = ctx.CharacterController.isGrounded;
            bool isCrouching = ctx.Input.CrouchToggledOn && !ctx.Input.SprintToggledOn;
            bool isSprinting = ctx.Input.SprintToggledOn
                               && ctx.Input.MovementInput != Vector2.zero
                               && !ctx.Input.CrouchToggledOn
                               && isGrounded;

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
            Vector3 movementDirection = cameraRightXZ * ctx.Input.MovementInput.x + cameraForwardXZ * ctx.Input.MovementInput.y;

            Vector3 movementDelta = movementDirection * lateralAcceleration * ctx.Delta;
            Vector3 localVelocity = new Vector3(
                state.Velocity.x - ctx.Dependency.trainVelocityOffset.x,
                0f,
                state.Velocity.z - ctx.Dependency.trainVelocityOffset.z);
            Vector3 newVelocity = localVelocity + movementDelta;

            // Add drag to player
            Vector3 currentDrag = newVelocity.normalized * derivedStats.drag * ctx.Delta;
            newVelocity = (newVelocity.magnitude > derivedStats.drag * ctx.Delta) ? newVelocity - currentDrag : Vector3.zero;
            newVelocity = Vector3.ClampMagnitude(new Vector3(newVelocity.x, 0f, newVelocity.z), clampLateralMagnitude);
            newVelocity.y = state.Velocity.y;
            newVelocity = !isGrounded ? HandleSteepWalls(ctx, newVelocity, state.Velocity.y) : newVelocity;

            newVelocity += ConsumeImpulse(state);
            newVelocity.y = state.Velocity.y;

            state.Velocity = newVelocity;
            state.Velocity.y += ctx.Dependency.trainKnockbackVertical;

            TickImpulse(ctx, ref state);
        }

        private static void HandleSlideMovement(in PlayerSimulationContext ctx, ref PlayerMovementDataState state)
        {
            var derivedStats = CalculateDerivedStats(ctx.Dependency, ctx.Config);

            // we'll do this in a bit
            Vector3 groundNormal = CharacterControllerUtils.GetNormalWithSphereCast(ctx.CharacterController, ctx.Dependency.groundLayers);
            float slopeAngle = Vector3.Angle(groundNormal, Vector3.up);

            Vector3 slopeDownDirection = Vector3.ProjectOnPlane(Vector3.down, groundNormal).normalized;

            Vector3 slideDirection = new Vector3(ctx.CharacterController.velocity.x, 0f, ctx.CharacterController.velocity.z).normalized;
            float slopeDot = Vector3.Dot(slideDirection, slopeDownDirection);

            bool isDownhill = slopeAngle > ctx.Config.slopeAngleThreshold && slopeDot > 0.1f;
            bool isUphill = slopeAngle > ctx.Config.slopeAngleThreshold && slopeDot < -0.1f;

            // Update slide timer based on slope
            if (isDownhill)
            {
                state.SlideTimer -= ctx.Delta * 0.5f; // Slower decay on downhill
            }
            else if (isUphill)
            {
                state.SlideTimer -= ctx.Delta * ctx.Config.uphillSlideDecelerationMultiplier;
            }
            else
            {
                state.SlideTimer -= ctx.Delta;
            }

            // Calculate slide speed
            float slideProgress = 1f - (state.SlideTimer / ctx.Config.slideDuration);
            float currentSlideSpeed = Mathf.Lerp(derivedStats.slideSpeed, derivedStats.minSlideSpeed, slideProgress);

            // Apply slope modifications to speed
            if (isDownhill)
            {
                currentSlideSpeed *= ctx.Config.downhillSlideSpeedBoost;
            }
            else if (isUphill)
            {
                currentSlideSpeed = Mathf.Max(currentSlideSpeed - (ctx.Config.slideDeceleration * ctx.Config.uphillSlideDecelerationMultiplier * ctx.Delta), derivedStats.minSlideSpeed);
            }
            else
            {
                currentSlideSpeed = Mathf.Max(currentSlideSpeed - (ctx.Config.slideDeceleration * ctx.Delta), derivedStats.minSlideSpeed);
            }

            // Apply Overdrive speed multiplier to slide
            if (ctx.Dependency.IsInOverdrive)
            {
                currentSlideSpeed *= ctx.Dependency.OverdriveSpeedMultiplier;
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
        }

        public static void TickVerticalMovement(in PlayerSimulationContext ctx, ref PlayerMovementDataState state)
        {
            var derivedStats = CalculateDerivedStats(ctx.Dependency, ctx.Config);
            var verticalVelocity = state.Velocity.y;

            // Use CharacterController.isGrounded (updated by last frame's Move) combined with
            // JumpedLastSimulatedFrame to cover the one-tick window right after a jump where the
            // CC hasn't moved off the ground yet. This mirrors the old _jumpedLastFrame pattern.
            bool isGrounded = ctx.CharacterController.isGrounded && !state.JumpedLastSimulatedFrame;
            state.JumpedLastSimulatedFrame = false;

            verticalVelocity -= ctx.Config.gravity * ctx.Delta;

            if (isGrounded && verticalVelocity < 0f)
            {
                verticalVelocity = -derivedStats.antiBump;
                state.GrappleImpulse = Vector3.zero;
            }

            if (ctx.Input.JumpPressed && isGrounded)
            {
                verticalVelocity += Mathf.Sqrt(ctx.Config.jumpSpeed * 3 * ctx.Config.gravity);
                state.JumpedLastSimulatedFrame = true;
            }

            // Fire once on the first airborne tick after leaving the ground (coyote-time antiBump).
            if (state.WasGroundedLastTick && !isGrounded)
            {
                verticalVelocity += derivedStats.antiBump;
            }

            state.WasGroundedLastTick = isGrounded;

            if (Mathf.Abs(verticalVelocity) > Mathf.Abs(ctx.Config.terminalVelocity))
            {
                verticalVelocity = -1f * Mathf.Abs(ctx.Config.terminalVelocity);
            }

            state.Velocity.y = verticalVelocity;
        }

        public static void TickCharacterControllerMovement(in PlayerSimulationContext ctx, ref PlayerMovementDataState state)
        {
            ctx.CharacterController.Move((state.Velocity + ctx.Dependency.trainVelocityOffset) * ctx.Delta);
            state.Position = ctx.CharacterController.transform.position;
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
            in PlayerSimulationContext ctx,
            Vector3 velocity,
            float verticalVelocity
        )
        {
            Vector3 normal = CharacterControllerUtils.GetNormalWithSphereCast(ctx.CharacterController, ctx.Dependency.groundLayers);
            float angle = Vector3.Angle(normal, Vector3.up);
            bool validAngle = angle <= ctx.CharacterController.slopeLimit;

            if (!validAngle && verticalVelocity < 0f)
                velocity = Vector3.ProjectOnPlane(velocity, normal);

            return velocity;
        }

        private static Vector3 ConsumeImpulse(in PlayerMovementDataState state)
        {
            return state.GrappleImpulse;
        }

        private static void TickImpulse(in PlayerSimulationContext ctx, ref PlayerMovementDataState state)
        {
            if (state.GrappleImpulse.sqrMagnitude <= 0.001f)
            {
                state.GrappleImpulse = Vector3.zero;
                return;
            }

            state.GrappleImpulse = Vector3.MoveTowards(state.GrappleImpulse, Vector3.zero, GrappleImpulseDecay * ctx.Delta);
        }
    }
}
