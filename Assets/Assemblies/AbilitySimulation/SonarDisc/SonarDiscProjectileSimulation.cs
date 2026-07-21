using UnityEngine;

namespace Resonance.Assemblies.AbilitySimulation.SonarDisc
{
    public class SonarDiscProjectileSimulation
    {
        /// <summary>
        /// The small offset pushed along the surface normal when attaching, so the disc rests just
        /// proud of the surface rather than z-fighting with it.
        /// </summary>
        public const float SurfaceAttachOffset = 0.01f;

        /// <summary>
        /// Counts down the ticks-until-shoot-sound gate. Runs every tick, in both the travel and
        /// attached phases.
        /// </summary>
        public static void TickShootSound(ref SonarDiscProjectileState state)
        {
            if (state.TicksUntilShootSound > 0)
                state.TicksUntilShootSound -= 1;
        }

        /// <summary>
        /// Travel-phase step (not attached, no collision this tick): accumulate distance travelled,
        /// signal a destroy once max range is reached, and advance the swept-raycast origin. The
        /// swept collision test and the attach itself are external (resolved by the behaviour).
        /// </summary>
        public static void TravelStep(in SonarDiscProjectileSimulationContext ctx, ref SonarDiscProjectileState state)
        {
            // Per-tick output, consumed each tick, never accumulated.
            state.ShouldDestroy = false;

            state.DistanceTravelled += Vector3.Distance(ctx.CurrentPosition, state.LastPosition);
            if (state.DistanceTravelled >= ctx.Config.maxRange)
                state.ShouldDestroy = true;

            // For the swept hit-test on the next tick.
            state.LastPosition = ctx.CurrentPosition;
        }

        /// <summary>
        /// Wall-pulse-phase step (attached to a wall): wait out the pre-pulse delay, flip into pulsing,
        /// expand the pulse radius, and signal a scan each pulsing tick. Signals a destroy once the
        /// pulse has fully expanded. The overlap-sphere scan is external and driven by the
        /// <see cref="SonarDiscProjectileState.ShouldScanThisTick"/> /
        /// <see cref="SonarDiscProjectileState.CurrentPulseRadius"/> outputs.
        /// </summary>
        public static void WallPulseStep(in SonarDiscProjectileSimulationContext ctx, ref SonarDiscProjectileState state)
        {
            // Per-tick outputs, consumed each tick, never accumulated.
            state.ShouldScanThisTick = false;
            state.CurrentPulseRadius = 0f;
            state.ShouldDestroy = false;

            if (state.IsDespawning)
                return;

            var config = ctx.Config;
            var delta = ctx.Delta;

            if (state.PulseElapsed >= config.pulseExpandDuration)
            {
                state.ShouldDestroy = true;
                return;
            }

            switch (state.IsPulsing)
            {
                case false when state.PrePulseElapsed >= config.pulseDelay:
                    state.IsPulsing = true;
                    break;
                case false:
                    state.PrePulseElapsed += delta;
                    break;
                default:
                {
                    state.PulseElapsed += delta;
                    state.CurrentPulseRadius = Mathf.Lerp(0f, config.pulseRadius, state.PulseElapsed / config.pulseExpandDuration);
                    state.ShouldScanThisTick = true;
                    break;
                }
            }
        }

        /// <summary>
        /// Computes the attach pose from a surface hit. Returns the world attach point (nudged off the
        /// surface) and the surface-aligned rotation, plus that pose expressed in the target's local
        /// space so the disc can ride a moving surface via <see cref="ComputeFollowPose"/>. Pure; the
        /// behaviour resolves the target transform and applies the result to the rigidbody.
        /// </summary>
        public static void ComputeLocalAttachPose(
            Vector3 hitPoint,
            Vector3 hitNormal,
            Vector3 targetPos,
            Quaternion targetRot,
            out Vector3 worldAttachPoint,
            out Quaternion surfaceAlignment,
            out Vector3 localPos,
            out Quaternion localRot
        )
        {
            surfaceAlignment = Quaternion.LookRotation(-hitNormal);
            worldAttachPoint = hitPoint + hitNormal * SurfaceAttachOffset;

            Quaternion inverseTargetRot = Quaternion.Inverse(targetRot);
            localPos = inverseTargetRot * (worldAttachPoint - targetPos);
            localRot = inverseTargetRot * surfaceAlignment;
        }

        /// <summary>
        /// Reconstructs the disc's world pose from a target's reconciled transform and the local attach
        /// pose, so the disc rides moving surfaces. Inverse of <see cref="ComputeLocalAttachPose"/>.
        /// </summary>
        public static void ComputeFollowPose(
            Vector3 targetPos,
            Quaternion targetRot,
            Vector3 localPos,
            Quaternion localRot,
            out Vector3 worldPos,
            out Quaternion worldRot
        )
        {
            worldPos = targetPos + targetRot * localPos;
            worldRot = targetRot * localRot;
        }
    }
}
