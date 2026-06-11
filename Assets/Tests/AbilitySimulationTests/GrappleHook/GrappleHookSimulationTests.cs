using NUnit.Framework;
using Resonance.Assemblies.AbilitySimulation.GrappleHook;
using UnityEngine;

public class GrappleHookSimulationTests
{
    private const float Tolerance = 1e-4f;

    private static AbilityGrappleHookInput MakeInput(
        bool activatePressed = false,
        Vector3 hookPoint = default,
        bool jumpPressed = false,
        Vector3 cameraPosition = default,
        Vector3 cameraForward = default,
        Vector3 localTransformPosition = default
    )
    {
        return new AbilityGrappleHookInput
        {
            ActivatePressed = activatePressed,
            CameraForward = cameraForward,
            CameraPosition = cameraPosition,
            HookPoint = hookPoint,
            JumpPressed = jumpPressed,
            LocalTransformPosition = localTransformPosition
        };
    }

    private static AbilityGrappleHookState MakeState(
        bool isGrappling = false,
        Vector3 hookPoint = default,
        float reelTime = 0,
        Vector3 reelVelocity = default,
        Vector3 exitImpulse = default,
        Vector3 cameraPosition = default,
        Vector3 cameraForward = default,
        float cooldown = 0
    )
    {
        return new AbilityGrappleHookState()
        {
            IsGrappling = isGrappling,
            HookPoint = hookPoint,
            ReelTime = reelTime,
            ReelVelocity = reelVelocity,
            ExitImpulse = exitImpulse,
            CameraPosition = cameraPosition,
            CameraForward = cameraForward,
            Cooldown = cooldown
        };
    }

    private static GrappleHookConfig MakeConfig(
        float maxRange = 5f,
        float reelSpeed = 2f,
        float maxReelTime = 5f,
        float exitBoost = 2f,
        float upwardBias = 1f,
        float cooldown = 1f,
        LayerMask grappleLayerMask = default
    )
    {
        return new GrappleHookConfig()
        {
            maxRange = maxRange,
            reelSpeed = reelSpeed,
            maxReelTime = maxReelTime,
            exitBoost = exitBoost,
            upwardBias = upwardBias,
            cooldown = cooldown,
            grappleLayerMask = grappleLayerMask
        };
    }

    [Test]
    public void StepUsesLocalTransformPositionToDetermineDirection()
    {
        // In the future, transform position should be server-auth, not input-based
        var input = MakeInput(
            localTransformPosition: new Vector3(5f, 5f, 10f));
        var state = MakeState(
            hookPoint: new Vector3(10f, 10f, 10f),
            isGrappling: true
        );
        var config = MakeConfig();

        var ctx = new GrappleHookSimulationContext(
            input, config, delta: 0.1f);

        var expectedDirectionVelocity = (state.HookPoint - input.LocalTransformPosition).normalized;

        GrappleHookSimulation.Step(ctx, ref state);
        Assert.AreEqual(expectedDirectionVelocity, state.ReelVelocity.normalized);
    }

    [Test]
    public void StepStartsGrapplingWhenActivatePressedAndNotAlreadyGrappling()
    {
        var input = MakeInput(
            activatePressed: true,
            hookPoint: new Vector3(10f, 10f, 10f)
        );
        var state = MakeState(
            isGrappling: false
        );
        var config = MakeConfig();

        const float delta = 0.1f;
        var ctx = new GrappleHookSimulationContext(
            input, config, delta);

        GrappleHookSimulation.Step(ctx, ref state);

        Assert.IsTrue(state.IsGrappling);
        Assert.AreEqual(input.HookPoint, state.HookPoint);

        // expect the reel time to start out incremented by the delta
        Assert.AreEqual(0f + delta, state.ReelTime, Tolerance);
    }

    [Test]
    public void StepDoesNotUpdateHookPointOrReelTimeIfAlreadyGrappling()
    {
        var input = MakeInput(
            activatePressed: true,
            hookPoint: new Vector3(10f, 10f, 10f)
        );
        const float preExistingReelTime = 4f;
        var preExistingHookPoint = new Vector3(20f, 20f, 20f);
        var state = MakeState(
            isGrappling: true,
            hookPoint: preExistingHookPoint,
            reelTime: preExistingReelTime
        );
        var config = MakeConfig(
            maxReelTime: 10f
        );

        const float delta = 0.1f;
        var ctx = new GrappleHookSimulationContext(
            input, config, delta);

        GrappleHookSimulation.Step(ctx, ref state);

        Assert.AreEqual(preExistingHookPoint, state.HookPoint);
        Assert.AreEqual(preExistingReelTime + delta, state.ReelTime, Tolerance);
    }

    [Test]
    public void StepReadsInCameraInput()
    {
        // In the future, use a server-side player camera
        var input = MakeInput(
            cameraPosition: new Vector3(5f, 5f, 5f),
            cameraForward: new Vector3(2f, 2f, 2f)
        );

        var state = MakeState();
        var config = MakeConfig();

        var ctx = new GrappleHookSimulationContext(
            input, config, 0.1f);

        GrappleHookSimulation.Step(ctx, ref state);

        Assert.AreEqual(input.CameraForward, state.CameraForward);
        Assert.AreEqual(input.CameraPosition, state.CameraPosition);
    }

    [Test]
    public void StepDecrementsCooldownByDeltaIfAboveZero()
    {
        var input = MakeInput();
        const float initialCooldown = 10f;
        var state = MakeState(
            isGrappling: false,
            cooldown: initialCooldown
        );

        var config = MakeConfig(
            cooldown: 15f
        );

        const float delta = 0.1f;
        var ctx = new GrappleHookSimulationContext(
            input, config, delta);

        GrappleHookSimulation.Step(ctx, ref state);
        Assert.AreEqual(initialCooldown - delta, state.Cooldown, Tolerance);
    }

    [Test]
    public void StepDoesNotDecrementCooldownIfAtZero()
    {
        var input = MakeInput();
        var state = MakeState(
            isGrappling: false,
            cooldown: 0f
        );

        var config = MakeConfig(
            cooldown: 15f
        );

        const float delta = 0.1f;
        var ctx = new GrappleHookSimulationContext(
            input, config, delta);

        GrappleHookSimulation.Step(ctx, ref state);
        Assert.AreEqual(0f, state.Cooldown);
    }

    [Test]
    public void StepExitsGrappleAndStartsCooldownIfJumpPressed()
    {
        var input = MakeInput(
            jumpPressed: true
        );
        var state = MakeState(
            isGrappling: true
        );
        const float startingCooldown = 15f;
        var config = MakeConfig(
            cooldown: startingCooldown
        );

        const float delta = 0.1f;
        var ctx = new GrappleHookSimulationContext(
            input, config, delta);

        GrappleHookSimulation.Step(ctx, ref state);
        Assert.IsFalse(state.IsGrappling);
        Assert.AreEqual(startingCooldown, state.Cooldown);
    }

    [Test]
    public void StepExitsGrappleAndStartsCooldownIfMaxReelTimeExceeded()
    {
        var input = MakeInput();
        var state = MakeState(
            isGrappling: true,
            reelTime: 10f
        );
        const float startingCooldown = 15f;
        var config = MakeConfig(
            cooldown: startingCooldown,
            maxReelTime: 10f
        );

        const float delta = 0.1f;
        var ctx = new GrappleHookSimulationContext(
            input, config, delta);

        GrappleHookSimulation.Step(ctx, ref state);
        Assert.IsFalse(state.IsGrappling);
        Assert.AreEqual(startingCooldown, state.Cooldown);
    }

    [Test]
    public void StepUpdatesReelVelocityBasedOnHookDirectionAndReelSpeed()
    {
        var input = MakeInput(
            localTransformPosition: new Vector3(10f, 10f, 10f)
        );
        var hookPoint = new Vector3(30f, 30f, 30f);
        var state = MakeState(
            isGrappling: true,
            reelTime: 5f,
            hookPoint: hookPoint
        );
        var config = MakeConfig(
            maxReelTime: 10f,
            reelSpeed: 5f
        );

        const float delta = 0.1f;
        var ctx = new GrappleHookSimulationContext(
            input, config, delta);
        
        GrappleHookSimulation.Step(ctx, ref state);

        var expectedVelocity = (hookPoint - input.LocalTransformPosition).normalized * config.reelSpeed;
        Assert.AreEqual(expectedVelocity, state.ReelVelocity);
    }
}