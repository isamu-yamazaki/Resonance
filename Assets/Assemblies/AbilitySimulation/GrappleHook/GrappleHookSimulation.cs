using UnityEngine;

namespace Resonance.Assemblies.AbilitySimulation.GrappleHook
{
    public class GrappleHookSimulation
    {
        public static void Step(in GrappleHookSimulationContext ctx, ref AbilityGrappleHookState state)
        {
            var delta = ctx.Delta;
            var input = ctx.Input;
            var config = ctx.Config;
            
            if (state.Cooldown > 0)
                state.Cooldown -= delta;
            
            // Per-tick outputs are consumed each tick, never accumulated.
            state.ReelVelocity = Vector3.zero;
            state.ExitImpulse = Vector3.zero;
            state.BroadcastShootAndTravel = false;
            state.BroadcastGrappleRegistration = false;
            state.BroadcastStopTravel = false;
            state.BroadcastRelease = false;

            // Mirror the owner camera pose into state so SimulationOnly code (SimulateActivateAbility)
            // can read it outside the input frame.
            state.CameraPosition = input.CameraPosition;
            state.CameraForward = input.CameraForward;

            if (state.GrappleNextTick)
            {
                state.GrappleNextTick = false;

                Ray ray = new Ray(state.CameraPosition, state.CameraForward);
                if (Physics.Raycast(ray, out RaycastHit hit, config.maxRange, config.grappleLayerMask))
                {
                    state.IsGrappling = true;
                    state.HookPoint = hit.point;
                    state.ReelTime = 0f;
                    state.BroadcastShootAndTravel = true;
                    state.BroadcastGrappleRegistration = true;
                    state.GrappleRegistrationPosition = state.CameraPosition;
                }
            }

            if (!state.IsGrappling)
                return;

            state.ReelTime += delta;

            // In the future, transform position should be server-auth
            Vector3 directionToHook = state.HookPoint - input.LocalTransformPosition;
            float distanceToHook = directionToHook.magnitude;

            // Start the cooldown after the grappling hook ends
            if (input.JumpPressed)
            {
                ExitGrapple(in config, ref state, directionToHook, earlyExit: true);
                state.Cooldown = config.cooldown;
                return;
            }

            if (state.ReelTime >= config.maxReelTime || distanceToHook < 0.5f)
            {
                ExitGrapple(in config, ref state, directionToHook, earlyExit: false);
                state.Cooldown = config.cooldown;
                return;
            }

            // Send the player according to the reel speed, in the determined direction
            state.ReelVelocity = directionToHook.normalized * config.reelSpeed;
        }
        
        private static void ExitGrapple(
            in GrappleHookConfig config,
            ref AbilityGrappleHookState state,
            Vector3 directionToHook,
            bool earlyExit
        )
        {
            state.IsGrappling = false;
            state.BroadcastStopTravel = true;
            state.BroadcastRelease = true;

            if (earlyExit)
            {
                Vector3 pullDirection = directionToHook.normalized;
                Vector3 exitDirection = Vector3.Lerp(pullDirection, Vector3.up, config.upwardBias).normalized;
                state.ExitImpulse = exitDirection * (config.reelSpeed + config.exitBoost);
            }
        }
    }
    
}