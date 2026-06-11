using NUnit.Framework;
using Resonance.Assemblies.AbilitySimulation.BubbleShield;
using UnityEngine;

public class BubbleShieldSimulationTests
{
    private const float Tolerance = 1e-4f;

    private static AbilityBubbleShieldInput MakeInput(
        bool activatePressed = false,
        Vector3 spawnPosition = default,
        Vector3 lobDirection = default
    )
    {
        return new AbilityBubbleShieldInput
        {
            ActivatePressed = activatePressed,
            SpawnPosition = spawnPosition,
            LobDirection = lobDirection
        };
    }

    private static AbilityBubbleShieldState MakeState(
        float cooldown = 0f,
        Vector3 lobDirection = default,
        Vector3 spawnPosition = default,
        bool shouldSpawnShield = false
    )
    {
        return new AbilityBubbleShieldState
        {
            Cooldown = cooldown,
            LobDirection = lobDirection,
            SpawnPosition = spawnPosition,
            ShouldSpawnShield = shouldSpawnShield
        };
    }

    private static BubbleShieldConfig MakeConfig(
        float lobForce = 10f,
        float upwardLobBias = 0.5f,
        float cooldown = 5f
    )
    {
        return new BubbleShieldConfig
        {
            lobForce = lobForce,
            upwardLobBias = upwardLobBias,
            cooldown = cooldown
        };
    }

    [Test]
    public void StepMirrorsSpawnPositionAndLobDirectionFromInput()
    {
        var input = MakeInput(
            spawnPosition: new Vector3(1f, 2f, 3f),
            lobDirection: new Vector3(0f, 1f, 0f)
        );
        var state = MakeState();
        var config = MakeConfig();

        var ctx = new BubbleShieldSimulationContext(input, config, delta: 0.1f);

        BubbleShieldSimulation.Step(ctx, ref state);

        Assert.AreEqual(input.SpawnPosition, state.SpawnPosition);
        Assert.AreEqual(input.LobDirection, state.LobDirection);
    }

    [Test]
    public void StepSetsCooldownAndSignalsSpawnWhenActivatePressed()
    {
        var input = MakeInput(activatePressed: true);
        var state = MakeState(cooldown: 0f);
        const float cooldown = 5f;
        var config = MakeConfig(cooldown: cooldown);

        const float delta = 0.1f;
        var ctx = new BubbleShieldSimulationContext(input, config, delta);

        BubbleShieldSimulation.Step(ctx, ref state);

        Assert.IsTrue(state.ShouldSpawnShield);
        // Cooldown is raised to config.cooldown on activation, then decremented the same tick.
        Assert.AreEqual(cooldown - delta, state.Cooldown, Tolerance);
    }

    [Test]
    public void StepDoesNotSignalSpawnWhenActivateNotPressed()
    {
        var input = MakeInput(activatePressed: false);
        var state = MakeState();
        var config = MakeConfig();

        var ctx = new BubbleShieldSimulationContext(input, config, delta: 0.1f);

        BubbleShieldSimulation.Step(ctx, ref state);

        Assert.IsFalse(state.ShouldSpawnShield);
    }

    [Test]
    public void StepActivationIsNotCooldownGatedAndResetsRunningCooldown()
    {
        // Gating lives upstream in AbilityBubbleShield.ActivateAbilityExternal; the per-tick
        // Step always activates when pressed, even with a cooldown still running, and overwrites
        // that running cooldown with config.cooldown.
        var input = MakeInput(activatePressed: true);
        var state = MakeState(cooldown: 100f);
        var config = MakeConfig(cooldown: 5f);

        const float delta = 0.1f;
        var ctx = new BubbleShieldSimulationContext(input, config, delta);

        BubbleShieldSimulation.Step(ctx, ref state);

        Assert.IsTrue(state.ShouldSpawnShield);
        // The 100f running cooldown is replaced by config.cooldown, then decremented this tick.
        Assert.AreEqual(config.cooldown - delta, state.Cooldown, Tolerance);
    }

    [Test]
    public void StepCooldownDecrementsPastZeroWhenBelowDelta()
    {
        // The cooldown is not clamped at zero: any positive cooldown is decremented by the full
        // delta, so a cooldown smaller than delta dips slightly negative. AbilityReady treats
        // <= 0 as ready, so this is harmless — pinned here to document the lack of clamping.
        var input = MakeInput();
        const float delta = 0.1f;
        var state = MakeState(cooldown: 0.05f);
        var config = MakeConfig();

        var ctx = new BubbleShieldSimulationContext(input, config, delta);

        BubbleShieldSimulation.Step(ctx, ref state);

        Assert.AreEqual(0.05f - delta, state.Cooldown, Tolerance);
        Assert.Less(state.Cooldown, 0f);
    }

    [Test]
    public void StepResetsShouldSpawnShieldEachTick()
    {
        var input = MakeInput(activatePressed: false);
        var state = MakeState(shouldSpawnShield: true);
        var config = MakeConfig();

        var ctx = new BubbleShieldSimulationContext(input, config, delta: 0.1f);

        BubbleShieldSimulation.Step(ctx, ref state);

        Assert.IsFalse(state.ShouldSpawnShield);
    }

    [Test]
    public void StepDecrementsCooldownByDeltaWhenAboveZero()
    {
        var input = MakeInput();
        const float initialCooldown = 3f;
        var state = MakeState(cooldown: initialCooldown);
        var config = MakeConfig();

        const float delta = 0.1f;
        var ctx = new BubbleShieldSimulationContext(input, config, delta);

        BubbleShieldSimulation.Step(ctx, ref state);

        Assert.AreEqual(initialCooldown - delta, state.Cooldown, Tolerance);
    }

    [Test]
    public void StepDoesNotDecrementCooldownWhenAtZero()
    {
        var input = MakeInput();
        var state = MakeState(cooldown: 0f);
        var config = MakeConfig();

        var ctx = new BubbleShieldSimulationContext(input, config, delta: 0.1f);

        BubbleShieldSimulation.Step(ctx, ref state);

        Assert.AreEqual(0f, state.Cooldown);
    }

    [Test]
    public void TryActivateSucceedsAndSetsCooldownWhenOffCooldown()
    {
        var state = MakeState(cooldown: 0f);
        const float cooldown = 5f;
        var config = MakeConfig(cooldown: cooldown);

        bool activated = BubbleShieldSimulation.TryActivate(ref state, config);

        Assert.IsTrue(activated);
        Assert.AreEqual(cooldown, state.Cooldown);
    }

    [Test]
    public void TryActivateFailsAndLeavesCooldownWhenOnCooldown()
    {
        const float existingCooldown = 2f;
        var state = MakeState(cooldown: existingCooldown);
        var config = MakeConfig(cooldown: 5f);

        bool activated = BubbleShieldSimulation.TryActivate(ref state, config);

        Assert.IsFalse(activated);
        Assert.AreEqual(existingCooldown, state.Cooldown);
    }
}
