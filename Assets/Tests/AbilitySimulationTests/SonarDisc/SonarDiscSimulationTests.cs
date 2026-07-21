using NUnit.Framework;
using Resonance.Assemblies.AbilitySimulation.SonarDisc;
using UnityEngine;

public class SonarDiscSimulationTests
{
    private const float Tolerance = 1e-4f;

    private static SonarDiscAbilityInput MakeInput(
        bool activatePressed = false,
        Vector3 muzzlePosition = default,
        Vector3 muzzleForward = default
    )
    {
        return new SonarDiscAbilityInput
        {
            ActivatePressed = activatePressed,
            MuzzlePosition = muzzlePosition,
            MuzzleForward = muzzleForward
        };
    }

    private static SonarDiscAbilityState MakeState(
        float cooldown = 0f,
        bool spawnDiscNextTick = false,
        Vector3 muzzlePosition = default,
        Vector3 muzzleForward = default,
        bool shouldSpawnDisc = false,
        Vector3 spawnPosition = default,
        Vector3 spawnDirection = default
    )
    {
        return new SonarDiscAbilityState
        {
            Cooldown = cooldown,
            SpawnDiscNextTick = spawnDiscNextTick,
            MuzzlePosition = muzzlePosition,
            MuzzleForward = muzzleForward,
            ShouldSpawnDisc = shouldSpawnDisc,
            SpawnPosition = spawnPosition,
            SpawnDirection = spawnDirection
        };
    }

    private static SonarDiscConfig MakeConfig(float cooldown = 12f)
    {
        return new SonarDiscConfig { cooldown = cooldown };
    }

    [Test]
    public void StepDecrementsCooldownByDeltaWhenAboveZero()
    {
        const float initialCooldown = 10f;
        var state = MakeState(cooldown: initialCooldown);
        var ctx = new SonarDiscSimulationContext(MakeInput(), MakeConfig(), delta: 0.1f);

        SonarDiscSimulation.Step(ctx, ref state);

        Assert.AreEqual(initialCooldown - 0.1f, state.Cooldown, Tolerance);
    }

    [Test]
    public void StepDoesNotDecrementCooldownAtZero()
    {
        var state = MakeState(cooldown: 0f);
        var ctx = new SonarDiscSimulationContext(MakeInput(), MakeConfig(), delta: 0.1f);

        SonarDiscSimulation.Step(ctx, ref state);

        Assert.AreEqual(0f, state.Cooldown);
    }

    [Test]
    public void StepDoesNotSpawnOnTheTickActivateIsPressed()
    {
        // Activation only arms the spawn; the disc spawns on the following tick (one-tick delay).
        var state = MakeState(spawnDiscNextTick: false);
        var ctx = new SonarDiscSimulationContext(MakeInput(activatePressed: true), MakeConfig(), delta: 0.1f);

        SonarDiscSimulation.Step(ctx, ref state);

        Assert.IsFalse(state.ShouldSpawnDisc);
        Assert.IsTrue(state.SpawnDiscNextTick);
    }

    [Test]
    public void StepSpawnsOnTheTickAfterActivation()
    {
        var config = MakeConfig(cooldown: 12f);
        var state = MakeState(cooldown: 0f, spawnDiscNextTick: true);
        var ctx = new SonarDiscSimulationContext(MakeInput(), config, delta: 0.1f);

        SonarDiscSimulation.Step(ctx, ref state);

        Assert.IsTrue(state.ShouldSpawnDisc);
        // Spawning resets the cooldown to the configured max.
        Assert.AreEqual(config.cooldown, state.Cooldown, Tolerance);
        // The arm flag is consumed.
        Assert.IsFalse(state.SpawnDiscNextTick);
    }

    [Test]
    public void StepSpawnsFromThePreviouslyMirroredMuzzlePose()
    {
        // FireDisc historically used the muzzle pose mirrored on the previous tick, not this tick's
        // input. The spawn outputs must capture the pose already in state, before the fresh mirror.
        var storedPosition = new Vector3(1f, 2f, 3f);
        var storedForward = new Vector3(0f, 0f, 1f);
        var freshInputPosition = new Vector3(9f, 9f, 9f);
        var freshInputForward = new Vector3(1f, 0f, 0f);

        var state = MakeState(
            spawnDiscNextTick: true,
            muzzlePosition: storedPosition,
            muzzleForward: storedForward);
        var input = MakeInput(muzzlePosition: freshInputPosition, muzzleForward: freshInputForward);
        var ctx = new SonarDiscSimulationContext(input, MakeConfig(), delta: 0.1f);

        SonarDiscSimulation.Step(ctx, ref state);

        Assert.AreEqual(storedPosition, state.SpawnPosition);
        Assert.AreEqual(storedForward, state.SpawnDirection);
        // The fresh input is still mirrored into state for the next tick.
        Assert.AreEqual(freshInputPosition, state.MuzzlePosition);
        Assert.AreEqual(freshInputForward, state.MuzzleForward);
    }

    [Test]
    public void StepResetsShouldSpawnDiscEachTick()
    {
        // A stale ShouldSpawnDisc from a previous tick must not survive a tick that does not spawn.
        var state = MakeState(spawnDiscNextTick: false, shouldSpawnDisc: true);
        var ctx = new SonarDiscSimulationContext(MakeInput(), MakeConfig(), delta: 0.1f);

        SonarDiscSimulation.Step(ctx, ref state);

        Assert.IsFalse(state.ShouldSpawnDisc);
    }

    [Test]
    public void StepMirrorsMuzzlePoseFromInput()
    {
        var input = MakeInput(
            muzzlePosition: new Vector3(5f, 6f, 7f),
            muzzleForward: new Vector3(0f, 1f, 0f));
        var state = MakeState();
        var ctx = new SonarDiscSimulationContext(input, MakeConfig(), delta: 0.1f);

        SonarDiscSimulation.Step(ctx, ref state);

        Assert.AreEqual(input.MuzzlePosition, state.MuzzlePosition);
        Assert.AreEqual(input.MuzzleForward, state.MuzzleForward);
    }
}
