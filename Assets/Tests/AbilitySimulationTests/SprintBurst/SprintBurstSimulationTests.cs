using NUnit.Framework;
using Resonance.Assemblies.AbilitySimulation.SprintBurst;

public class SprintBurstSimulationTests
{
    private const float Tolerance = 1e-4f;

    private static AbilitySprintBurstInput MakeInput(bool sprinting = false)
    {
        return new AbilitySprintBurstInput
        {
            Sprinting = sprinting
        };
    }

    private static AbilitySprintBurstState MakeState(
        bool isEquipped = true,
        float timeSinceLastSprinting = 0f,
        float currentMeter = 5f,
        bool wasSprinting = false,
        float lastAppliedSpeedMod = 0f
    )
    {
        return new AbilitySprintBurstState
        {
            IsEquipped = isEquipped,
            TimeSinceLastSprinting = timeSinceLastSprinting,
            CurrentMeter = currentMeter,
            WasSprinting = wasSprinting,
            LastAppliedSpeedMod = lastAppliedSpeedMod
        };
    }

    private static SprintBurstConfig MakeConfig(
        float maxBurstSpeed = 2f,
        float minBurstSpeed = 1.2f,
        float maxMeter = 5f,
        float meterRecoverySpeed = 1f,
        float timeUntilRecovery = 0.5f
    )
    {
        return new SprintBurstConfig
        {
            maxBurstSpeed = maxBurstSpeed,
            minBurstSpeed = minBurstSpeed,
            maxMeter = maxMeter,
            meterRecoverySpeed = meterRecoverySpeed,
            timeUntilRecovery = timeUntilRecovery
        };
    }

    private static void Step(AbilitySprintBurstInput input, SprintBurstConfig config, float delta,
        ref AbilitySprintBurstState state)
    {
        SprintBurstSimulation.Step(new SprintBurstSimulationContext(input, config, delta), ref state);
    }

    [Test]
    public void Step_Sprinting_DepletesMeterByDelta()
    {
        var state = MakeState(currentMeter: 5f);
        const float delta = 0.1f;

        Step(MakeInput(sprinting: true), MakeConfig(maxMeter: 5f), delta, ref state);

        Assert.AreEqual(5f - delta, state.CurrentMeter, Tolerance);
    }

    [Test]
    public void Step_Sprinting_ClampsMeterAtZero()
    {
        var state = MakeState(currentMeter: 0.05f);

        Step(MakeInput(sprinting: true), MakeConfig(maxMeter: 5f), delta: 0.1f, ref state);

        Assert.AreEqual(0f, state.CurrentMeter, Tolerance);
    }

    [Test]
    public void Step_SprintingAtFullMeter_AppliesMaxBoost()
    {
        var state = MakeState(currentMeter: 5f);
        var config = MakeConfig(maxBurstSpeed: 2f, minBurstSpeed: 1.2f, maxMeter: 5f);

        // delta 0 keeps the meter full so the boost lerp sits at t = 1.
        Step(MakeInput(sprinting: true), config, delta: 0f, ref state);

        Assert.AreEqual(2f, state.LastAppliedSpeedMod, Tolerance);
    }

    [Test]
    public void Step_SprintingAtEmptyMeter_AppliesMinBoost()
    {
        var state = MakeState(currentMeter: 0f);
        var config = MakeConfig(maxBurstSpeed: 2f, minBurstSpeed: 1.2f, maxMeter: 5f);

        Step(MakeInput(sprinting: true), config, delta: 0f, ref state);

        Assert.AreEqual(1.2f, state.LastAppliedSpeedMod, Tolerance);
    }

    [Test]
    public void Step_SprintingAtHalfMeter_InterpolatesBoost()
    {
        var state = MakeState(currentMeter: 2.5f);
        var config = MakeConfig(maxBurstSpeed: 2f, minBurstSpeed: 1.2f, maxMeter: 5f);

        // t = 2.5 / 5 = 0.5 -> Lerp(1.2, 2, 0.5) = 1.6
        Step(MakeInput(sprinting: true), config, delta: 0f, ref state);

        Assert.AreEqual(1.6f, state.LastAppliedSpeedMod, Tolerance);
    }

    [Test]
    public void Step_Sprinting_SetsWasSprinting()
    {
        var state = MakeState(wasSprinting: false);

        Step(MakeInput(sprinting: true), MakeConfig(), delta: 0.1f, ref state);

        Assert.IsTrue(state.WasSprinting);
    }

    [Test]
    public void Step_StopsSprinting_ClearsModifierResetsTimerAndFlag()
    {
        var state = MakeState(wasSprinting: true, lastAppliedSpeedMod: 1.8f, timeSinceLastSprinting: 1f);

        Step(MakeInput(sprinting: false), MakeConfig(), delta: 0.1f, ref state);

        Assert.AreEqual(0f, state.LastAppliedSpeedMod, Tolerance);
        Assert.AreEqual(0f, state.TimeSinceLastSprinting, Tolerance);
        Assert.IsFalse(state.WasSprinting);
    }

    [Test]
    public void Step_NotSprintingBelowRecoveryDelay_DoesNotRecoverMeter()
    {
        var state = MakeState(wasSprinting: false, currentMeter: 2f, timeSinceLastSprinting: 0f);
        var config = MakeConfig(maxMeter: 5f, meterRecoverySpeed: 1f, timeUntilRecovery: 0.5f);

        // timer 0 -> 0.1, still below the 0.5 threshold, so the meter must not recover yet.
        Step(MakeInput(sprinting: false), config, delta: 0.1f, ref state);

        Assert.AreEqual(2f, state.CurrentMeter, Tolerance);
        Assert.AreEqual(0.1f, state.TimeSinceLastSprinting, Tolerance);
    }

    [Test]
    public void Step_NotSprintingAtRecoveryDelay_RecoversMeter()
    {
        var state = MakeState(wasSprinting: false, currentMeter: 2f, timeSinceLastSprinting: 0.5f);
        var config = MakeConfig(maxMeter: 5f, meterRecoverySpeed: 1f, timeUntilRecovery: 0.5f);

        // timer 0.5 -> 0.6 >= 0.5, so meter += delta * recoverySpeed = 0.1.
        Step(MakeInput(sprinting: false), config, delta: 0.1f, ref state);

        Assert.AreEqual(2.1f, state.CurrentMeter, Tolerance);
    }

    [Test]
    public void Step_NotSprinting_RecoveryClampsAtMaxMeter()
    {
        var state = MakeState(wasSprinting: false, currentMeter: 4.95f, timeSinceLastSprinting: 1f);
        var config = MakeConfig(maxMeter: 5f, meterRecoverySpeed: 1f, timeUntilRecovery: 0.5f);

        // 4.95 + 0.1 = 5.05, clamped to 5.
        Step(MakeInput(sprinting: false), config, delta: 0.1f, ref state);

        Assert.AreEqual(5f, state.CurrentMeter, Tolerance);
    }

    [Test]
    public void Step_SteadyNotSprinting_KeepsModifierZeroAndWasSprintingFalse()
    {
        var state = MakeState(wasSprinting: false, lastAppliedSpeedMod: 0f, currentMeter: 5f);

        Step(MakeInput(sprinting: false), MakeConfig(), delta: 0.1f, ref state);

        Assert.AreEqual(0f, state.LastAppliedSpeedMod, Tolerance);
        Assert.IsFalse(state.WasSprinting);
    }
}
