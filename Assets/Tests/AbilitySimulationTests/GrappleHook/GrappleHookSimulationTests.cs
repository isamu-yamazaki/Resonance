using NUnit.Framework;
using NUnit.Framework.Internal;
using Resonance.Assemblies.AbilitySimulation.GrappleHook;
using UnityEngine;

public class GrappleHookSimulationTests
{
    private const float Tolerance = 1e-4f;

    private GameObject _grappleTarget;

    [TearDown]
    public void DestroyGrappleTarget()
    {
        if (_grappleTarget != null)
            Object.DestroyImmediate(_grappleTarget);
    }

    private static AbilityGrappleHookInput MakeInput(
        bool jumpPressed = false,
        Vector3 cameraPosition = default,
        Vector3 cameraForward = default,
        Vector3 localTransformPosition = default
    )
    {
        return new AbilityGrappleHookInput
        {
            CameraForward = cameraForward,
            CameraPosition = cameraPosition,
            JumpPressed = jumpPressed,
            LocalTransformPosition = localTransformPosition
        };
    }

    private static AbilityGrappleHookState MakeState(
        GrappleStatus grappleStatus = default,
        float pendingTime = 0,
        Vector3 hookPoint = default,
        float reelTime = 0,
        Vector3 reelVelocity = default,
        Vector3 exitImpulse = default,
        Vector3 cameraPosition = default,
        Vector3 cameraForward = default,
        float cooldown = 0,
        bool broadcastShootAndTravel = false,
        bool broadcastGrappleRegistration = false,
        bool broadcastStopTravel = false,
        bool broadcastRelease = false,
        Vector3 grappleRegistrationPosition = default
    )
    {
        return new AbilityGrappleHookState()
        {
            GrappleStatus = grappleStatus,
            PendingTime = pendingTime,
            HookPoint = hookPoint,
            ReelTime = reelTime,
            ReelVelocityThisTick = reelVelocity,
            ExitImpulse = exitImpulse,
            CameraPosition = cameraPosition,
            CameraForward = cameraForward,
            Cooldown = cooldown,
            BroadcastShootAndTravel = broadcastShootAndTravel,
            BroadcastGrappleRegistration = broadcastGrappleRegistration,
            BroadcastStopTravel = broadcastStopTravel,
            BroadcastRelease = broadcastRelease,
            GrappleRegistrationPosition = grappleRegistrationPosition
        };
    }

    private static GrappleHookConfig MakeConfig(
        float animationDelay = 5f,
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
            animationDelay = animationDelay,
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
            grappleStatus: GrappleStatus.Grappling,
            hookPoint: new Vector3(10f, 10f, 10f)
        );
        var config = MakeConfig();

        var ctx = new GrappleHookSimulationContext(
            input, config, delta: 0.1f);

        var expectedDirectionVelocity = (state.HookPoint - input.LocalTransformPosition).normalized;

        GrappleHookSimulation.Step(ctx, ref state);
        Assert.AreEqual(expectedDirectionVelocity, state.ReelVelocityThisTick.normalized);
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
            grappleStatus: GrappleStatus.None,
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
            grappleStatus: GrappleStatus.None,
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
    [TestCase(0.1f)]
    [TestCase(0.2f)]
    public void StepResetsIfMeetsOrExceedsConfiguredAnimationTimeAndNoRaycastHit(float delta)
    {
        var config = MakeConfig(
            animationDelay: 2f
        );

        var state = MakeState(
            grappleStatus: GrappleStatus.PendingWithDelay,
            pendingTime: 1.9f
        );

        var ctx = new GrappleHookSimulationContext(MakeInput(), config, delta);
        GrappleHookSimulation.Step(ctx, ref state);
        Assert.AreEqual(0f, state.PendingTime);
        Assert.AreEqual(GrappleStatus.None, state.GrappleStatus);
    }

    [Test]
    [TestCase(0.1f)]
    [TestCase(0.2f)]
    public void StepGrapplesIfMeetsOrExceedsConfiguredAnimationTimeAndRaycastHit(float delta)
    {
        // Physics.Raycast has no mock seam, so we place a real collider in the edit-mode
        // physics scene directly in front of the camera pose and within maxRange, then
        // sync transforms so the query sees it. The camera looks down +Z from the origin
        // at a unit cube centered at z = 3, whose front face sits at z = 2.5.
        const int defaultLayer = 0;
        _grappleTarget = GameObject.CreatePrimitive(PrimitiveType.Cube);
        _grappleTarget.layer = defaultLayer;
        _grappleTarget.transform.position = new Vector3(0f, 0f, 3f);
        Physics.SyncTransforms();

        var input = MakeInput(
            cameraPosition: Vector3.zero,
            cameraForward: Vector3.forward
        );
        var config = MakeConfig(
            animationDelay: 2f,
            maxRange: 5f,
            grappleLayerMask: 1 << defaultLayer
        );

        var state = MakeState(
            grappleStatus: GrappleStatus.PendingWithDelay,
            pendingTime: 1.9f
        );

        var ctx = new GrappleHookSimulationContext(input, config, delta);
        GrappleHookSimulation.Step(ctx, ref state);

        Assert.AreEqual(GrappleStatus.Grappling, state.GrappleStatus);
        Assert.AreEqual(0f, state.PendingTime);
        Assert.AreEqual(2.5f, state.HookPoint.z, Tolerance);
        Assert.IsTrue(state.BroadcastShootAndTravel);
        Assert.IsTrue(state.BroadcastGrappleRegistration);
        Assert.AreEqual(input.CameraPosition, state.GrappleRegistrationPosition);
    }

    [Test]
    public void StepExitsGrappleAndStartsCooldownIfJumpPressed()
    {
        var input = MakeInput(
            jumpPressed: true
        );
        var state = MakeState(
            grappleStatus: GrappleStatus.Grappling
        );
        const float startingCooldown = 15f;
        var config = MakeConfig(
            cooldown: startingCooldown
        );

        const float delta = 0.1f;
        var ctx = new GrappleHookSimulationContext(
            input, config, delta);

        GrappleHookSimulation.Step(ctx, ref state);
        Assert.AreEqual(GrappleStatus.None, state.GrappleStatus);
        Assert.AreEqual(startingCooldown, state.Cooldown);
    }

    [Test]
    public void StepExitsGrappleAndStartsCooldownIfMaxReelTimeExceeded()
    {
        var input = MakeInput();
        var state = MakeState(
            grappleStatus: GrappleStatus.Grappling,
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
        Assert.AreEqual(GrappleStatus.None, state.GrappleStatus);
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
            grappleStatus: GrappleStatus.Grappling,
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
        Assert.AreEqual(expectedVelocity, state.ReelVelocityThisTick);
    }

    [Test]
    public void StepSetsStopAndReleaseFlagsOnJumpExit()
    {
        var input = MakeInput(
            jumpPressed: true
        );
        var state = MakeState(
            grappleStatus: GrappleStatus.Grappling
        );
        var config = MakeConfig();

        var ctx = new GrappleHookSimulationContext(
            input, config, delta: 0.1f);

        GrappleHookSimulation.Step(ctx, ref state);

        Assert.IsTrue(state.BroadcastStopTravel);
        Assert.IsTrue(state.BroadcastRelease);
        Assert.AreEqual(GrappleStatus.None, state.GrappleStatus);
    }

    [Test]
    public void StepSetsStopAndReleaseFlagsOnMaxReelTimeExit()
    {
        var input = MakeInput();
        var state = MakeState(
            grappleStatus: GrappleStatus.Grappling,
            reelTime: 10f
        );
        var config = MakeConfig(
            maxReelTime: 10f
        );

        var ctx = new GrappleHookSimulationContext(
            input, config, delta: 0.1f);

        GrappleHookSimulation.Step(ctx, ref state);

        Assert.IsTrue(state.BroadcastStopTravel);
        Assert.IsTrue(state.BroadcastRelease);
    }

    [Test]
    public void StepLeavesAllBroadcastFlagsFalseOnSteadyGrapplingTick()
    {
        var input = MakeInput(
            localTransformPosition: new Vector3(0f, 0f, 0f)
        );
        var state = MakeState(
            grappleStatus: GrappleStatus.Grappling,
            reelTime: 1f,
            hookPoint: new Vector3(30f, 30f, 30f)
        );
        var config = MakeConfig(
            maxReelTime: 10f
        );

        var ctx = new GrappleHookSimulationContext(
            input, config, delta: 0.1f);

        GrappleHookSimulation.Step(ctx, ref state);

        Assert.IsFalse(state.BroadcastShootAndTravel);
        Assert.IsFalse(state.BroadcastGrappleRegistration);
        Assert.IsFalse(state.BroadcastStopTravel);
        Assert.IsFalse(state.BroadcastRelease);
    }

    [Test]
    public void StepResetsStalePreviousBroadcastFlags()
    {
        var input = MakeInput(
            localTransformPosition: new Vector3(0f, 0f, 0f)
        );
        var state = MakeState(
            grappleStatus: GrappleStatus.Grappling,
            reelTime: 1f,
            hookPoint: new Vector3(30f, 30f, 30f),
            broadcastShootAndTravel: true,
            broadcastGrappleRegistration: true,
            broadcastStopTravel: true,
            broadcastRelease: true
        );
        var config = MakeConfig(
            maxReelTime: 10f
        );

        var ctx = new GrappleHookSimulationContext(
            input, config, delta: 0.1f);

        GrappleHookSimulation.Step(ctx, ref state);

        Assert.IsFalse(state.BroadcastShootAndTravel);
        Assert.IsFalse(state.BroadcastGrappleRegistration);
        Assert.IsFalse(state.BroadcastStopTravel);
        Assert.IsFalse(state.BroadcastRelease);
    }
}