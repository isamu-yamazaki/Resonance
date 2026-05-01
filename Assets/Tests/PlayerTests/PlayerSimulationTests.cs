using NUnit.Framework;
using Resonance.Assemblies.Player;
using UnityEngine;

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
        float drag = 20f,
        float gravity = 25f,
        float jumpSpeed = 1f,
        float terminalVelocity = 50f
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
            gravity = gravity,
            jumpSpeed = jumpSpeed,
            terminalVelocity = terminalVelocity,
        };
    }

    private static PlayerDependencyData MakeDeps(
        float multiplier = 1f,
        PlayerMovementState movementState = PlayerMovementState.Falling
    )
    {
        return new PlayerDependencyData
        {
            MovementSpeedMultiplier = multiplier,
            CurrentPlayerMovementState = movementState,
        };
    }

    private static PlayerInputData MakeInput(bool jumpPressed = false)
    {
        return new PlayerInputData
        {
            JumpPressed = jumpPressed,
        };
    }

    private static PlayerMovementDataState MakeState(
        Vector3 velocity = default,
        Vector3 grappleImpulse = default,
        bool jumpedLastSimulatedFrame = false,
        PlayerMovementState lastSimulatedMovementState = PlayerMovementState.Falling
    )
    {
        return new PlayerMovementDataState
        {
            Velocity = velocity,
            grappleImpulse = grappleImpulse,
            jumpedLastSimulatedFrame = jumpedLastSimulatedFrame,
            lastSimulatedMovementState = lastSimulatedMovementState,
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

    #region TickVerticalMovement

    // Branches in TickVerticalMovement (in execution order):
    //   A. Gravity:     vy -= gravity * delta            (always)
    //   B. Anti-bump:   if grounded && vy<0  -> vy = -antiBump; grappleImpulse = 0
    //   C. Jump:        if JumpPressed && grounded -> vy += sqrt(jumpSpeed*3*gravity); jumpedLastSimulatedFrame = true
    //   D. Was-grounded-now-airborne: if lastSim grounded && current not grounded -> vy += antiBump
    //   E. Terminal:    if |vy| > |terminal| -> vy = -|terminal|  (always negative; quirk)
    //
    // Tests neutralize unrelated branches by setting gravity=0, sprintSpeed=0 (antiBump=0),
    // or pushing terminalVelocity high so they don't interact with the branch under test.

    #region Branch A: Gravity

    [Test]
    public void TickVerticalMovement_GravityApplied_VyDecreasesByGravityTimesDelta()
    {
        var config = MakeConfig(gravity: 25f, terminalVelocity: 1000f);
        var deps = MakeDeps(movementState: PlayerMovementState.Falling);
        var input = MakeInput();
        var state = MakeState(velocity: new Vector3(0f, 10f, 0f), lastSimulatedMovementState: PlayerMovementState.Falling);

        PlayerSimulation.TickVerticalMovement(input, deps, ref state, config, 0.1f);

        Assert.AreEqual(10f - 25f * 0.1f, state.Velocity.y, Tolerance);
    }

    #endregion

    #region Branch B: Grounded anti-bump clamp

    [Test]
    public void TickVerticalMovement_GroundedAndFalling_ClampsVyToNegativeAntiBump()
    {
        var config = MakeConfig(gravity: 0f, sprintSpeed: 7f, terminalVelocity: 1000f);
        var deps = MakeDeps(multiplier: 1f, movementState: PlayerMovementState.Idling);
        var input = MakeInput();
        var state = MakeState(velocity: new Vector3(0f, -5f, 0f), lastSimulatedMovementState: PlayerMovementState.Idling);

        PlayerSimulation.TickVerticalMovement(input, deps, ref state, config, 1f / 60f);

        Assert.AreEqual(-7f, state.Velocity.y, Tolerance);
    }

    [Test]
    public void TickVerticalMovement_GroundedAndFalling_ClearsGrappleImpulse()
    {
        var config = MakeConfig(gravity: 0f, sprintSpeed: 7f);
        var deps = MakeDeps(movementState: PlayerMovementState.Idling);
        var input = MakeInput();
        var state = MakeState(
            velocity: new Vector3(0f, -5f, 0f),
            grappleImpulse: new Vector3(1f, 2f, 3f),
            lastSimulatedMovementState: PlayerMovementState.Idling
        );

        PlayerSimulation.TickVerticalMovement(input, deps, ref state, config, 1f / 60f);

        Assert.AreEqual(Vector3.zero, state.grappleImpulse);
    }

    [Test]
    public void TickVerticalMovement_GroundedButRising_DoesNotClamp()
    {
        var config = MakeConfig(gravity: 0f, sprintSpeed: 7f, terminalVelocity: 1000f);
        var deps = MakeDeps(movementState: PlayerMovementState.Idling);
        var input = MakeInput();
        var state = MakeState(velocity: new Vector3(0f, 5f, 0f), lastSimulatedMovementState: PlayerMovementState.Idling);

        PlayerSimulation.TickVerticalMovement(input, deps, ref state, config, 1f / 60f);

        Assert.AreEqual(5f, state.Velocity.y, Tolerance);
    }

    [Test]
    public void TickVerticalMovement_AirborneAndFalling_DoesNotClamp()
    {
        var config = MakeConfig(gravity: 0f, sprintSpeed: 7f, terminalVelocity: 1000f);
        var deps = MakeDeps(movementState: PlayerMovementState.Falling);
        var input = MakeInput();
        var state = MakeState(velocity: new Vector3(0f, -5f, 0f), lastSimulatedMovementState: PlayerMovementState.Falling);

        PlayerSimulation.TickVerticalMovement(input, deps, ref state, config, 1f / 60f);

        Assert.AreEqual(-5f, state.Velocity.y, Tolerance);
    }

    #endregion

    #region Branch C: Jump

    [Test]
    public void TickVerticalMovement_GroundedAndJumpPressed_AddsJumpVelocityAndSetsFlag()
    {
        // sprintSpeed=0 so antiBump=0; Branch B clamps to 0 (instead of -antiBump),
        // making the post-Branch-C value exactly sqrt(jumpSpeed*3*gravity).
        var config = MakeConfig(gravity: 25f, jumpSpeed: 1f, sprintSpeed: 0f, terminalVelocity: 1000f);
        var deps = MakeDeps(movementState: PlayerMovementState.Idling);
        var input = MakeInput(jumpPressed: true);
        var state = MakeState(velocity: Vector3.zero, lastSimulatedMovementState: PlayerMovementState.Idling);

        PlayerSimulation.TickVerticalMovement(input, deps, ref state, config, 0.1f);

        Assert.AreEqual(Mathf.Sqrt(1f * 3f * 25f), state.Velocity.y, Tolerance);
        Assert.IsTrue(state.jumpedLastSimulatedFrame);
    }

    [Test]
    public void TickVerticalMovement_AirborneAndJumpPressed_DoesNotJump()
    {
        var config = MakeConfig(gravity: 0f, jumpSpeed: 1f, sprintSpeed: 0f, terminalVelocity: 1000f);
        var deps = MakeDeps(movementState: PlayerMovementState.Falling);
        var input = MakeInput(jumpPressed: true);
        var state = MakeState(velocity: Vector3.zero, lastSimulatedMovementState: PlayerMovementState.Falling);

        PlayerSimulation.TickVerticalMovement(input, deps, ref state, config, 1f / 60f);

        Assert.AreEqual(0f, state.Velocity.y, Tolerance);
        Assert.IsFalse(state.jumpedLastSimulatedFrame);
    }

    [Test]
    public void TickVerticalMovement_GroundedButJumpNotPressed_DoesNotSetJumpedFlag()
    {
        var config = MakeConfig(gravity: 0f, jumpSpeed: 1f, sprintSpeed: 0f, terminalVelocity: 1000f);
        var deps = MakeDeps(movementState: PlayerMovementState.Idling);
        var input = MakeInput(jumpPressed: false);
        var state = MakeState(velocity: Vector3.zero, lastSimulatedMovementState: PlayerMovementState.Idling);

        PlayerSimulation.TickVerticalMovement(input, deps, ref state, config, 1f / 60f);

        Assert.IsFalse(state.jumpedLastSimulatedFrame);
    }

    #endregion

    #region Branch D: Was-grounded-now-airborne anti-bump

    [Test]
    public void TickVerticalMovement_LastFrameGroundedNowAirborne_AddsAntiBumpToVy()
    {
        var config = MakeConfig(gravity: 0f, sprintSpeed: 10f, terminalVelocity: 1000f);
        var deps = MakeDeps(multiplier: 1f, movementState: PlayerMovementState.Falling);
        var input = MakeInput();
        var state = MakeState(velocity: Vector3.zero, lastSimulatedMovementState: PlayerMovementState.Idling);

        PlayerSimulation.TickVerticalMovement(input, deps, ref state, config, 1f / 60f);

        Assert.AreEqual(10f, state.Velocity.y, Tolerance);
    }

    [Test]
    public void TickVerticalMovement_LastFrameGroundedAndStillGrounded_DoesNotAddAntiBump()
    {
        var config = MakeConfig(gravity: 0f, sprintSpeed: 10f, terminalVelocity: 1000f);
        var deps = MakeDeps(multiplier: 1f, movementState: PlayerMovementState.Sprinting);
        var input = MakeInput();
        // vy=+5 to skip Branch B (which requires vy<0).
        var state = MakeState(velocity: new Vector3(0f, 5f, 0f), lastSimulatedMovementState: PlayerMovementState.Idling);

        PlayerSimulation.TickVerticalMovement(input, deps, ref state, config, 1f / 60f);

        Assert.AreEqual(5f, state.Velocity.y, Tolerance);
    }

    [Test]
    public void TickVerticalMovement_LastFrameAirborneAndStillAirborne_DoesNotAddAntiBump()
    {
        var config = MakeConfig(gravity: 0f, sprintSpeed: 10f, terminalVelocity: 1000f);
        var deps = MakeDeps(multiplier: 1f, movementState: PlayerMovementState.Falling);
        var input = MakeInput();
        var state = MakeState(velocity: Vector3.zero, lastSimulatedMovementState: PlayerMovementState.Falling);

        PlayerSimulation.TickVerticalMovement(input, deps, ref state, config, 1f / 60f);

        Assert.AreEqual(0f, state.Velocity.y, Tolerance);
    }

    #endregion

    #region Branch E: Terminal velocity clamp

    [Test]
    public void TickVerticalMovement_VyBelowNegativeTerminal_ClampsToNegativeTerminal()
    {
        var config = MakeConfig(gravity: 0f, sprintSpeed: 0f, terminalVelocity: 50f);
        var deps = MakeDeps(movementState: PlayerMovementState.Falling);
        var input = MakeInput();
        var state = MakeState(velocity: new Vector3(0f, -100f, 0f), lastSimulatedMovementState: PlayerMovementState.Falling);

        PlayerSimulation.TickVerticalMovement(input, deps, ref state, config, 1f / 60f);

        Assert.AreEqual(-50f, state.Velocity.y, Tolerance);
    }

    [Test]
    public void TickVerticalMovement_VyAboveTerminal_FlipsToNegativeTerminal()
    {
        // Documents legacy quirk: terminal-velocity branch always assigns the negative
        // terminal value, even when vy was positive and overshot upward.
        var config = MakeConfig(gravity: 0f, sprintSpeed: 0f, terminalVelocity: 50f);
        var deps = MakeDeps(movementState: PlayerMovementState.Falling);
        var input = MakeInput();
        var state = MakeState(velocity: new Vector3(0f, 100f, 0f), lastSimulatedMovementState: PlayerMovementState.Falling);

        PlayerSimulation.TickVerticalMovement(input, deps, ref state, config, 1f / 60f);

        Assert.AreEqual(-50f, state.Velocity.y, Tolerance);
    }

    #endregion

    #region Lateral preservation

    [Test]
    public void TickVerticalMovement_DoesNotModifyLateralVelocity()
    {
        var config = MakeConfig(gravity: 25f, jumpSpeed: 1f, sprintSpeed: 7f, terminalVelocity: 50f);
        var deps = MakeDeps(movementState: PlayerMovementState.Idling);
        var input = MakeInput(jumpPressed: true);
        var state = MakeState(velocity: new Vector3(3f, 0f, 5f), lastSimulatedMovementState: PlayerMovementState.Idling);

        PlayerSimulation.TickVerticalMovement(input, deps, ref state, config, 0.1f);

        Assert.AreEqual(3f, state.Velocity.x, Tolerance);
        Assert.AreEqual(5f, state.Velocity.z, Tolerance);
    }

    #endregion

    #endregion
}
