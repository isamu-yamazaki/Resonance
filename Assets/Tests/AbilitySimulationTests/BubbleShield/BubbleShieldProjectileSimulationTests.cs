using NUnit.Framework;
using Resonance.Assemblies.AbilitySimulation.BubbleShield;
using UnityEngine;

public class BubbleShieldProjectileSimulationTests
{
    private const float Tolerance = 1e-4f;

    private static BubbleShieldProjectileState MakeState(
        float aliveTime = 0f,
        bool isDespawning = false,
        bool isLanded = false,
        float health = 100f,
        bool shouldFreezeBody = false,
        bool shouldBeginDespawn = false
    )
    {
        return new BubbleShieldProjectileState
        {
            AliveTime = aliveTime,
            IsDespawning = isDespawning,
            IsLanded = isLanded,
            Health = health,
            ShouldFreezeBody = shouldFreezeBody,
            ShouldBeginDespawn = shouldBeginDespawn
        };
    }

    private static BubbleShieldProjectileConfig MakeConfig(
        float shieldHealth = 100f,
        float shieldDuration = 10f,
        float despawnAnimDuration = 2f,
        LayerMask groundMask = default,
        float groundProbeRadius = 0.5f,
        float groundProbeDistance = 0.1f
    )
    {
        return new BubbleShieldProjectileConfig
        {
            shieldHealth = shieldHealth,
            shieldDuration = shieldDuration,
            despawnAnimDuration = despawnAnimDuration,
            groundMask = groundMask,
            groundProbeRadius = groundProbeRadius,
            groundProbeDistance = groundProbeDistance
        };
    }

    private static BubbleShieldProjectileSimulationContext MakeContext(
        BubbleShieldProjectileConfig config,
        float delta = 0.1f,
        Vector3 linearVelocity = default,
        bool isGrounded = false
    )
    {
        return new BubbleShieldProjectileSimulationContext(config, delta, linearVelocity, isGrounded);
    }

    // A velocity that clearly counts as descending (Y at/below the descent threshold).
    private static readonly Vector3 DescendingVelocity = Vector3.down;

    // A velocity that clearly does not count as descending (Y above the descent threshold).
    private static readonly Vector3 RisingVelocity = Vector3.up;

    [Test]
    public void StepLandsAndFreezesBodyWhenDescendingAndGrounded()
    {
        var state = MakeState(isLanded: false);
        var config = MakeConfig();
        var ctx = MakeContext(config, linearVelocity: DescendingVelocity, isGrounded: true);

        BubbleShieldProjectileSimulation.Step(ctx, ref state);

        Assert.IsTrue(state.IsLanded);
        Assert.IsTrue(state.ShouldFreezeBody);
    }

    [Test]
    public void StepDoesNotLandWhenNotGrounded()
    {
        var state = MakeState(isLanded: false);
        var config = MakeConfig();
        var ctx = MakeContext(config, linearVelocity: DescendingVelocity, isGrounded: false);

        BubbleShieldProjectileSimulation.Step(ctx, ref state);

        Assert.IsFalse(state.IsLanded);
        Assert.IsFalse(state.ShouldFreezeBody);
    }

    [Test]
    public void StepDoesNotLandWhenNotDescending()
    {
        var state = MakeState(isLanded: false);
        var config = MakeConfig();
        var ctx = MakeContext(config, linearVelocity: RisingVelocity, isGrounded: true);

        BubbleShieldProjectileSimulation.Step(ctx, ref state);

        Assert.IsFalse(state.IsLanded);
        Assert.IsFalse(state.ShouldFreezeBody);
    }

    [Test]
    public void StepLandsWhenVelocityExactlyAtDescendThreshold()
    {
        var state = MakeState(isLanded: false);
        var config = MakeConfig();
        // Threshold is inclusive (velocity.y <= threshold counts as descending).
        var atThreshold = new Vector3(0f, BubbleShieldProjectileSimulation.DescendVelocityThreshold, 0f);
        var ctx = MakeContext(config, linearVelocity: atThreshold, isGrounded: true);

        BubbleShieldProjectileSimulation.Step(ctx, ref state);

        Assert.IsTrue(state.IsLanded);
        Assert.IsTrue(state.ShouldFreezeBody);
    }

    [Test]
    public void StepDoesNotLandWhenVelocityJustAboveDescendThreshold()
    {
        var state = MakeState(isLanded: false);
        var config = MakeConfig();
        var aboveThreshold = new Vector3(0f, BubbleShieldProjectileSimulation.DescendVelocityThreshold + 1f, 0f);
        var ctx = MakeContext(config, linearVelocity: aboveThreshold, isGrounded: true);

        BubbleShieldProjectileSimulation.Step(ctx, ref state);

        Assert.IsFalse(state.IsLanded);
        Assert.IsFalse(state.ShouldFreezeBody);
    }

    [Test]
    public void StepDoesNotAccumulateAliveTimeBeforeLanding()
    {
        var state = MakeState(isLanded: false, aliveTime: 0f);
        var config = MakeConfig();
        // Descending but not grounded, so it stays airborne.
        var ctx = MakeContext(config, delta: 0.1f, linearVelocity: DescendingVelocity, isGrounded: false);

        BubbleShieldProjectileSimulation.Step(ctx, ref state);

        Assert.AreEqual(0f, state.AliveTime);
    }

    [Test]
    public void StepAccumulatesAliveTimeAfterLanding()
    {
        var state = MakeState(isLanded: true, aliveTime: 0f);
        var config = MakeConfig(shieldDuration: 10f, despawnAnimDuration: 2f);

        const float delta = 0.1f;
        var ctx = MakeContext(config, delta: delta);

        BubbleShieldProjectileSimulation.Step(ctx, ref state);

        Assert.AreEqual(delta, state.AliveTime, Tolerance);
        // Already landed coming in, so the body is not frozen a second time.
        Assert.IsFalse(state.ShouldFreezeBody);
    }

    [Test]
    public void StepDoesNotAccumulateAliveTimeOnTheLandingTick()
    {
        // On the tick it lands, Step returns right after flipping IsLanded, so alive-time accrual
        // (and the despawn check) only start on the following tick.
        var state = MakeState(isLanded: false, aliveTime: 0f);
        var config = MakeConfig();
        var ctx = MakeContext(config, delta: 0.1f, linearVelocity: DescendingVelocity, isGrounded: true);

        BubbleShieldProjectileSimulation.Step(ctx, ref state);

        Assert.IsTrue(state.IsLanded);
        Assert.AreEqual(0f, state.AliveTime);
    }

    [Test]
    public void StepBeginsDespawnWhenAliveTimeReachesThreshold()
    {
        var config = MakeConfig(shieldDuration: 10f, despawnAnimDuration: 2f);
        // Despawn begins once AliveTime >= shieldDuration - despawnAnimDuration.
        float threshold = config.shieldDuration - config.despawnAnimDuration;
        var state = MakeState(isLanded: true, aliveTime: threshold);
        var ctx = MakeContext(config, delta: 0.1f);

        BubbleShieldProjectileSimulation.Step(ctx, ref state);

        Assert.IsTrue(state.IsDespawning);
        Assert.IsTrue(state.ShouldBeginDespawn);
    }

    [Test]
    public void StepDoesNotBeginDespawnBeforeThreshold()
    {
        var config = MakeConfig(shieldDuration: 10f, despawnAnimDuration: 2f);
        var state = MakeState(isLanded: true, aliveTime: 1f);
        var ctx = MakeContext(config, delta: 0.1f);

        BubbleShieldProjectileSimulation.Step(ctx, ref state);

        Assert.IsFalse(state.IsDespawning);
        Assert.IsFalse(state.ShouldBeginDespawn);
    }

    [Test]
    public void StepDoesNothingWhenDespawning()
    {
        var config = MakeConfig();
        var state = MakeState(isDespawning: true, isLanded: true, aliveTime: 5f);
        var ctx = MakeContext(config, delta: 0.1f, linearVelocity: DescendingVelocity, isGrounded: true);

        BubbleShieldProjectileSimulation.Step(ctx, ref state);

        // No alive-time accrual and no new freeze/despawn signals once despawning.
        Assert.AreEqual(5f, state.AliveTime);
        Assert.IsFalse(state.ShouldFreezeBody);
        Assert.IsFalse(state.ShouldBeginDespawn);
    }

    [Test]
    public void StepResetsTransientOutputsEachTick()
    {
        var config = MakeConfig();
        var state = MakeState(isLanded: true, aliveTime: 1f,
            shouldFreezeBody: true, shouldBeginDespawn: true);
        var ctx = MakeContext(config, delta: 0.1f);

        BubbleShieldProjectileSimulation.Step(ctx, ref state);

        Assert.IsFalse(state.ShouldFreezeBody);
        Assert.IsFalse(state.ShouldBeginDespawn);
    }

    [Test]
    public void ApplyDamageReducesHealth()
    {
        var state = MakeState(health: 100f);

        BubbleShieldProjectileSimulation.ApplyDamage(ref state, 30f);

        Assert.AreEqual(70f, state.Health, Tolerance);
        Assert.IsFalse(state.IsDespawning);
        Assert.IsFalse(state.ShouldBeginDespawn);
    }

    [Test]
    public void ApplyDamageBeginsDespawnWhenHealthDepleted()
    {
        var state = MakeState(health: 20f);

        BubbleShieldProjectileSimulation.ApplyDamage(ref state, 25f);

        Assert.IsTrue(state.IsDespawning);
        Assert.IsTrue(state.ShouldBeginDespawn);
        // Health is not clamped — it carries the overkill past zero.
        Assert.AreEqual(-5f, state.Health, Tolerance);
    }

    [Test]
    public void ApplyDamageClearsStaleBeginDespawnFlagWhenNonLethal()
    {
        // ShouldBeginDespawn is a per-call output: a non-lethal hit clears any stale value so the
        // caller never starts a despawn from a leftover flag.
        var state = MakeState(health: 100f, shouldBeginDespawn: true);

        BubbleShieldProjectileSimulation.ApplyDamage(ref state, 10f);

        Assert.IsFalse(state.ShouldBeginDespawn);
        Assert.IsFalse(state.IsDespawning);
        Assert.AreEqual(90f, state.Health, Tolerance);
    }

    [Test]
    public void ApplyDamageBeginsDespawnWhenHealthExactlyZero()
    {
        var state = MakeState(health: 20f);

        BubbleShieldProjectileSimulation.ApplyDamage(ref state, 20f);

        Assert.AreEqual(0f, state.Health, Tolerance);
        Assert.IsTrue(state.IsDespawning);
        Assert.IsTrue(state.ShouldBeginDespawn);
    }

    [Test]
    public void ApplyDamageDoesNotBeginDespawnTwiceWhenAlreadyDespawning()
    {
        var state = MakeState(health: 5f, isDespawning: true);

        BubbleShieldProjectileSimulation.ApplyDamage(ref state, 10f);

        // Already despawning, so no new begin-despawn signal is raised.
        Assert.IsFalse(state.ShouldBeginDespawn);
    }
}
