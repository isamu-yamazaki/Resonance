using NUnit.Framework;
using Resonance.Assemblies.Player;

public class PlayerSimulationTests
{
    private const float Tolerance = 1e-5f;

    private static PlayerConfig MakeConfig(
        float crouchSpeed = 2f,
        float runSpeed = 4f,
        float sprintSpeed = 7f,
        float slideSpeed = 8f,
        float minSlideSpeed = 2f,
        float crouchAcc = 25f,
        float runAcc = 35f,
        float sprintAcc = 50f,
        float inAirAcc = 25f,
        float drag = 20f
    )
    {
        return new PlayerConfig
        {
            baseCrouchSpeed = crouchSpeed,
            baseRunSpeed = runSpeed,
            baseSprintSpeed = sprintSpeed,
            baseSlideSpeed = slideSpeed,
            baseMinSlideSpeed = minSlideSpeed,
            baseCrouchAcceleration = crouchAcc,
            baseRunAcceleration = runAcc,
            baseSprintAcceleration = sprintAcc,
            baseInAirAcceleration = inAirAcc,
            baseDrag = drag,
        };
    }

    private static PlayerDependencyData MakeDeps(float multiplier)
    {
        return new PlayerDependencyData
        {
            MovementSpeedMultiplier = multiplier,
        };
    }

    #region CalculateDerivedStats

    [Test]
    public void CalculateDerivedStats_MultiplierOne_ReturnsBaseValues()
    {
        var config = MakeConfig();
        var deps = MakeDeps(1f);

        var stats = PlayerSimulation.CalculateDerivedStats(deps, config);

        Assert.AreEqual(config.baseCrouchSpeed, stats.crouchSpeed, Tolerance);
        Assert.AreEqual(config.baseRunSpeed, stats.runSpeed, Tolerance);
        Assert.AreEqual(config.baseSprintSpeed, stats.sprintSpeed, Tolerance);
        Assert.AreEqual(config.baseSlideSpeed, stats.slideSpeed, Tolerance);
        Assert.AreEqual(config.baseMinSlideSpeed, stats.minSlideSpeed, Tolerance);
        Assert.AreEqual(config.baseCrouchAcceleration, stats.crouchAcceleration, Tolerance);
        Assert.AreEqual(config.baseRunAcceleration, stats.runAcceleration, Tolerance);
        Assert.AreEqual(config.baseSprintAcceleration, stats.sprintAcceleration, Tolerance);
        Assert.AreEqual(config.baseInAirAcceleration, stats.inAirAcceleration, Tolerance);
        Assert.AreEqual(config.baseDrag, stats.drag, Tolerance);
        Assert.AreEqual(config.baseSprintSpeed, stats.antiBump, Tolerance);
    }

    [Test]
    public void CalculateDerivedStats_MultiplierTwo_DoublesEveryField()
    {
        var config = MakeConfig();
        var deps = MakeDeps(2f);

        var stats = PlayerSimulation.CalculateDerivedStats(deps, config);

        Assert.AreEqual(config.baseCrouchSpeed * 2f, stats.crouchSpeed, Tolerance);
        Assert.AreEqual(config.baseRunSpeed * 2f, stats.runSpeed, Tolerance);
        Assert.AreEqual(config.baseSprintSpeed * 2f, stats.sprintSpeed, Tolerance);
        Assert.AreEqual(config.baseSlideSpeed * 2f, stats.slideSpeed, Tolerance);
        Assert.AreEqual(config.baseMinSlideSpeed * 2f, stats.minSlideSpeed, Tolerance);
        Assert.AreEqual(config.baseCrouchAcceleration * 2f, stats.crouchAcceleration, Tolerance);
        Assert.AreEqual(config.baseRunAcceleration * 2f, stats.runAcceleration, Tolerance);
        Assert.AreEqual(config.baseSprintAcceleration * 2f, stats.sprintAcceleration, Tolerance);
        Assert.AreEqual(config.baseInAirAcceleration * 2f, stats.inAirAcceleration, Tolerance);
        Assert.AreEqual(config.baseDrag * 2f, stats.drag, Tolerance);
        Assert.AreEqual(config.baseSprintSpeed * 2f, stats.antiBump, Tolerance);
    }

    [Test]
    public void CalculateDerivedStats_MultiplierZero_ZerosEveryField()
    {
        var config = MakeConfig();
        var deps = MakeDeps(0f);

        var stats = PlayerSimulation.CalculateDerivedStats(deps, config);

        Assert.AreEqual(0f, stats.crouchSpeed, Tolerance);
        Assert.AreEqual(0f, stats.runSpeed, Tolerance);
        Assert.AreEqual(0f, stats.sprintSpeed, Tolerance);
        Assert.AreEqual(0f, stats.slideSpeed, Tolerance);
        Assert.AreEqual(0f, stats.minSlideSpeed, Tolerance);
        Assert.AreEqual(0f, stats.crouchAcceleration, Tolerance);
        Assert.AreEqual(0f, stats.runAcceleration, Tolerance);
        Assert.AreEqual(0f, stats.sprintAcceleration, Tolerance);
        Assert.AreEqual(0f, stats.inAirAcceleration, Tolerance);
        Assert.AreEqual(0f, stats.drag, Tolerance);
        Assert.AreEqual(0f, stats.antiBump, Tolerance);
    }

    [Test]
    public void CalculateDerivedStats_AntiBumpEqualsSprintSpeed()
    {
        // Use deliberately different baseSprintSpeed so a constant-of-config bug would
        // fail this even at multiplier=1.
        var config = MakeConfig(sprintSpeed: 13f);
        var deps = MakeDeps(1.5f);

        var stats = PlayerSimulation.CalculateDerivedStats(deps, config);

        Assert.AreEqual(stats.sprintSpeed, stats.antiBump, Tolerance);
        Assert.AreEqual(13f * 1.5f, stats.antiBump, Tolerance);
    }

    [Test]
    public void CalculateDerivedStats_EachFieldUsesOwnBase()
    {
        // Distinct primes per field so any field-mixing bug surfaces.
        var config = MakeConfig(
            crouchSpeed: 2f,
            runSpeed: 3f,
            sprintSpeed: 5f,
            slideSpeed: 7f,
            minSlideSpeed: 11f,
            crouchAcc: 13f,
            runAcc: 17f,
            sprintAcc: 19f,
            inAirAcc: 23f,
            drag: 29f
        );
        var deps = MakeDeps(1f);

        var stats = PlayerSimulation.CalculateDerivedStats(deps, config);

        Assert.AreEqual(2f, stats.crouchSpeed, Tolerance);
        Assert.AreEqual(3f, stats.runSpeed, Tolerance);
        Assert.AreEqual(5f, stats.sprintSpeed, Tolerance);
        Assert.AreEqual(7f, stats.slideSpeed, Tolerance);
        Assert.AreEqual(11f, stats.minSlideSpeed, Tolerance);
        Assert.AreEqual(13f, stats.crouchAcceleration, Tolerance);
        Assert.AreEqual(17f, stats.runAcceleration, Tolerance);
        Assert.AreEqual(19f, stats.sprintAcceleration, Tolerance);
        Assert.AreEqual(23f, stats.inAirAcceleration, Tolerance);
        Assert.AreEqual(29f, stats.drag, Tolerance);
        Assert.AreEqual(5f, stats.antiBump, Tolerance);
    }

    [Test]
    public void CalculateDerivedStats_IsPure_RepeatCallsReturnEqualResults()
    {
        var config = MakeConfig();
        var deps = MakeDeps(1.25f);

        var first = PlayerSimulation.CalculateDerivedStats(deps, config);
        var second = PlayerSimulation.CalculateDerivedStats(deps, config);

        Assert.AreEqual(first.crouchSpeed, second.crouchSpeed, Tolerance);
        Assert.AreEqual(first.runSpeed, second.runSpeed, Tolerance);
        Assert.AreEqual(first.sprintSpeed, second.sprintSpeed, Tolerance);
        Assert.AreEqual(first.slideSpeed, second.slideSpeed, Tolerance);
        Assert.AreEqual(first.minSlideSpeed, second.minSlideSpeed, Tolerance);
        Assert.AreEqual(first.crouchAcceleration, second.crouchAcceleration, Tolerance);
        Assert.AreEqual(first.runAcceleration, second.runAcceleration, Tolerance);
        Assert.AreEqual(first.sprintAcceleration, second.sprintAcceleration, Tolerance);
        Assert.AreEqual(first.inAirAcceleration, second.inAirAcceleration, Tolerance);
        Assert.AreEqual(first.drag, second.drag, Tolerance);
        Assert.AreEqual(first.antiBump, second.antiBump, Tolerance);
    }

    #endregion
}
