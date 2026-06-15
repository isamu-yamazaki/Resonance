using NUnit.Framework;
using Resonance.Assemblies.AbilitySimulation.SonarDisc;
using UnityEngine;

public class SonarDiscProjectileSimulationTests
{
    private const float Tolerance = 1e-4f;

    private static SonarDiscProjectileState MakeState(
        Vector3 lastPosition = default,
        bool isAttached = false,
        bool isDespawning = false,
        bool isPulsing = false,
        float prePulseElapsed = 0f,
        float pulseElapsed = 0f,
        float distanceTravelled = 0f,
        int ticksUntilShootSound = 0,
        bool shouldDestroy = false,
        bool shouldScanThisTick = false,
        float currentPulseRadius = 0f
    )
    {
        return new SonarDiscProjectileState
        {
            LastPosition = lastPosition,
            IsAttached = isAttached,
            IsDespawning = isDespawning,
            IsPulsing = isPulsing,
            PrePulseElapsed = prePulseElapsed,
            PulseElapsed = pulseElapsed,
            DistanceTravelled = distanceTravelled,
            TicksUntilShootSound = ticksUntilShootSound,
            ShouldDestroy = shouldDestroy,
            ShouldScanThisTick = shouldScanThisTick,
            CurrentPulseRadius = currentPulseRadius
        };
    }

    private static SonarDiscProjectileConfig MakeConfig(
        float travelSpeed = 28f,
        float maxRange = 40f,
        float pulseDelay = 1f,
        float pulseRadius = 30f,
        float pulseExpandDuration = 0.6f
    )
    {
        return new SonarDiscProjectileConfig
        {
            travelSpeed = travelSpeed,
            maxRange = maxRange,
            pulseDelay = pulseDelay,
            pulseRadius = pulseRadius,
            pulseExpandDuration = pulseExpandDuration
        };
    }

    private static SonarDiscProjectileSimulationContext MakeContext(
        SonarDiscProjectileConfig config,
        float delta = 0.1f,
        Vector3 currentPosition = default
    )
    {
        return new SonarDiscProjectileSimulationContext(config, delta, currentPosition);
    }

    #region TickShootSound

    [Test]
    public void TickShootSoundDecrementsCountdown()
    {
        var state = MakeState(ticksUntilShootSound: 3);

        SonarDiscProjectileSimulation.TickShootSound(ref state);

        Assert.AreEqual(2, state.TicksUntilShootSound);
        Assert.IsFalse(state.PlayShootSound);
    }

    [Test]
    public void TickShootSoundIsFlooredAtZero()
    {
        var state = MakeState(ticksUntilShootSound: 0);

        SonarDiscProjectileSimulation.TickShootSound(ref state);

        Assert.AreEqual(0, state.TicksUntilShootSound);
        Assert.IsTrue(state.PlayShootSound);
    }

    #endregion

    #region TravelStep

    [Test]
    public void TravelStepAccumulatesDistanceAndAdvancesLastPosition()
    {
        var state = MakeState(lastPosition: Vector3.zero, distanceTravelled: 0f);
        var current = new Vector3(3f, 0f, 4f); // distance 5 from origin
        var ctx = MakeContext(MakeConfig(maxRange: 40f), currentPosition: current);

        SonarDiscProjectileSimulation.TravelStep(ctx, ref state);

        Assert.AreEqual(5f, state.DistanceTravelled, Tolerance);
        Assert.AreEqual(current, state.LastPosition);
        Assert.IsFalse(state.ShouldDestroy);
    }

    [Test]
    public void TravelStepSignalsDestroyWhenMaxRangeReached()
    {
        var state = MakeState(lastPosition: Vector3.zero, distanceTravelled: 38f);
        var current = new Vector3(3f, 0f, 4f); // +5 => 43 >= 40
        var ctx = MakeContext(MakeConfig(maxRange: 40f), currentPosition: current);

        SonarDiscProjectileSimulation.TravelStep(ctx, ref state);

        Assert.IsTrue(state.ShouldDestroy);
    }

    [Test]
    public void TravelStepResetsStaleDestroyFlag()
    {
        var state = MakeState(lastPosition: Vector3.zero, distanceTravelled: 0f, shouldDestroy: true);
        var ctx = MakeContext(MakeConfig(maxRange: 40f), currentPosition: new Vector3(1f, 0f, 0f));

        SonarDiscProjectileSimulation.TravelStep(ctx, ref state);

        Assert.IsFalse(state.ShouldDestroy);
    }

    #endregion

    #region WallPulseStep

    [Test]
    public void WallPulseStepDoesNothingWhenDespawning()
    {
        var state = MakeState(isDespawning: true, isPulsing: true, pulseElapsed: 0.2f,
            shouldScanThisTick: true, currentPulseRadius: 10f);
        var ctx = MakeContext(MakeConfig(), delta: 0.1f);

        SonarDiscProjectileSimulation.WallPulseStep(ctx, ref state);

        Assert.AreEqual(0.2f, state.PulseElapsed, Tolerance);
        Assert.IsFalse(state.ShouldScanThisTick);
        Assert.IsFalse(state.ShouldDestroy);
        Assert.AreEqual(0f, state.CurrentPulseRadius);
    }

    [Test]
    public void WallPulseStepAccumulatesPrePulseDelayBeforePulsing()
    {
        var state = MakeState(isPulsing: false, prePulseElapsed: 0f);
        var ctx = MakeContext(MakeConfig(pulseDelay: 1f), delta: 0.1f);

        SonarDiscProjectileSimulation.WallPulseStep(ctx, ref state);

        Assert.AreEqual(0.1f, state.PrePulseElapsed, Tolerance);
        Assert.IsFalse(state.IsPulsing);
        Assert.IsFalse(state.ShouldScanThisTick);
    }

    [Test]
    public void WallPulseStepFlipsToPulsingOnceDelayElapses()
    {
        // On the flip tick it only sets IsPulsing; expansion/scan begin the following tick.
        var state = MakeState(isPulsing: false, prePulseElapsed: 1f, pulseElapsed: 0f);
        var ctx = MakeContext(MakeConfig(pulseDelay: 1f), delta: 0.1f);

        SonarDiscProjectileSimulation.WallPulseStep(ctx, ref state);

        Assert.IsTrue(state.IsPulsing);
        Assert.AreEqual(0f, state.PulseElapsed);
        Assert.IsFalse(state.ShouldScanThisTick);
    }

    [Test]
    public void WallPulseStepExpandsRadiusAndSignalsScanWhilePulsing()
    {
        var config = MakeConfig(pulseRadius: 30f, pulseExpandDuration: 0.6f);
        var state = MakeState(isPulsing: true, pulseElapsed: 0f);
        var ctx = MakeContext(config, delta: 0.1f);

        SonarDiscProjectileSimulation.WallPulseStep(ctx, ref state);

        Assert.AreEqual(0.1f, state.PulseElapsed, Tolerance);
        Assert.IsTrue(state.ShouldScanThisTick);
        float expectedRadius = Mathf.Lerp(0f, config.pulseRadius, 0.1f / config.pulseExpandDuration);
        Assert.AreEqual(expectedRadius, state.CurrentPulseRadius, Tolerance);
    }

    [Test]
    public void WallPulseStepSignalsDestroyWhenPulseFullyExpanded()
    {
        var config = MakeConfig(pulseExpandDuration: 0.6f);
        var state = MakeState(isPulsing: true, pulseElapsed: config.pulseExpandDuration);
        var ctx = MakeContext(config, delta: 0.1f);

        SonarDiscProjectileSimulation.WallPulseStep(ctx, ref state);

        Assert.IsTrue(state.ShouldDestroy);
        Assert.IsFalse(state.ShouldScanThisTick);
    }

    [Test]
    public void WallPulseStepResetsTransientOutputsEachTick()
    {
        // Stale scan/destroy/radius outputs from a previous tick must not survive a pre-pulse tick.
        var state = MakeState(isPulsing: false, prePulseElapsed: 0f,
            shouldScanThisTick: true, shouldDestroy: true, currentPulseRadius: 15f);
        var ctx = MakeContext(MakeConfig(pulseDelay: 1f), delta: 0.1f);

        SonarDiscProjectileSimulation.WallPulseStep(ctx, ref state);

        Assert.IsFalse(state.ShouldScanThisTick);
        Assert.IsFalse(state.ShouldDestroy);
        Assert.AreEqual(0f, state.CurrentPulseRadius);
    }

    #endregion

    #region Attach / follow pose

    [Test]
    public void ComputeAttachAndFollowPoseRoundTripsToTheWorldAttachPose()
    {
        var hitPoint = new Vector3(2f, 3f, 4f);
        var hitNormal = new Vector3(0f, 1f, 0f);
        var targetPos = new Vector3(10f, 0f, -5f);
        var targetRot = Quaternion.Euler(15f, 40f, 80f);

        SonarDiscProjectileSimulation.ComputeLocalAttachPose(
            hitPoint, hitNormal, targetPos, targetRot,
            out Vector3 worldAttachPoint, out Quaternion surfaceAlignment,
            out Vector3 localPos, out Quaternion localRot);

        SonarDiscProjectileSimulation.ComputeFollowPose(
            targetPos, targetRot, localPos, localRot,
            out Vector3 worldPos, out Quaternion worldRot);

        Assert.Less(Vector3.Distance(worldAttachPoint, worldPos), Tolerance);
        Assert.Less(Quaternion.Angle(surfaceAlignment, worldRot), Tolerance);
    }

    [Test]
    public void ComputeLocalAttachPoseNudgesAlongTheSurfaceNormal()
    {
        var hitPoint = new Vector3(1f, 1f, 1f);
        var hitNormal = new Vector3(0f, 0f, 1f);

        SonarDiscProjectileSimulation.ComputeLocalAttachPose(
            hitPoint, hitNormal, Vector3.zero, Quaternion.identity,
            out Vector3 worldAttachPoint, out _, out _, out _);

        var expected = hitPoint + hitNormal * SonarDiscProjectileSimulation.SurfaceAttachOffset;
        Assert.Less(Vector3.Distance(expected, worldAttachPoint), Tolerance);
    }

    #endregion
}
