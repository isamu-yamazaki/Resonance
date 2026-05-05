using NUnit.Framework;
using Resonance.Assemblies.Player;
using UnityEngine;

public class PlayerSimulationTests
{
    private const float Tolerance = 1e-4f;

    // Per-test CharacterController fixture. PlayerSimulation reads cc.velocity and
    // (in TickLateralMovement) calls HandleSteepWalls' sphere cast against this CC.
    // Tests run in PlayMode so AddComponent<CharacterController>() is valid.
    private GameObject _ccGameObject;
    private CharacterController _cc;

    [SetUp]
    public void SetUp()
    {
        _ccGameObject = new GameObject("TestCC");
        _cc = _ccGameObject.AddComponent<CharacterController>();
    }

    [TearDown]
    public void TearDown()
    {
        if (_ccGameObject != null) Object.DestroyImmediate(_ccGameObject);
        _ccGameObject = null;
        _cc = null;
    }

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
        float terminalVelocity = 50f,
        float slideDuration = 1f,
        float slideDeceleration = 8f,
        float slopeAngleThreshold = 15f,
        float uphillSlideDecelerationMultiplier = 2f,
        float downhillSlideSpeedBoost = 1.5f
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
            slideDuration = slideDuration,
            slideDeceleration = slideDeceleration,
            slopeAngleThreshold = slopeAngleThreshold,
            uphillSlideDecelerationMultiplier = uphillSlideDecelerationMultiplier,
            downhillSlideSpeedBoost = downhillSlideSpeedBoost,
        };
    }

    private static PlayerDependencyData MakeDeps(
        float multiplier = 1f,
        PlayerMovementState movementState = PlayerMovementState.Falling,
        Vector3 trainVelocityOffset = default,
        float trainKnockbackVertical = 0f,
        LayerMask groundLayers = default,
        float overdriveSpeedMultiplier = 1f,
        bool isInOverdrive = false
    )
    {
        return new PlayerDependencyData
        {
            MovementSpeedMultiplier = multiplier,
            CurrentPlayerMovementState = movementState,
            trainVelocityOffset = trainVelocityOffset,
            trainKnockbackVertical = trainKnockbackVertical,
            groundLayers = groundLayers,
            OverdriveSpeedMultiplier = overdriveSpeedMultiplier,
            IsInOverdrive = isInOverdrive,
        };
    }

    private static PlayerInputData MakeInput(
        Vector2 movementInput = default,
        bool jumpPressed = false
    )
    {
        return new PlayerInputData
        {
            MovementInput = movementInput,
            JumpPressed = jumpPressed,
        };
    }

    private static PlayerMovementDataState MakeState(
        Vector3 velocity = default,
        float cameraYaw = 0f,
        Vector3 grappleImpulse = default,
        bool jumpedLastSimulatedFrame = false,
        PlayerMovementState lastSimulatedMovementState = PlayerMovementState.Falling,
        float slideTimer = 0f
    )
    {
        return new PlayerMovementDataState
        {
            Velocity = velocity,
            CameraYaw = cameraYaw,
            GrappleImpulse = grappleImpulse,
            JumpedLastSimulatedFrame = jumpedLastSimulatedFrame,
            LastSimulatedMovementState = lastSimulatedMovementState,
            SlideTimer = slideTimer,
        };
    }

    private PlayerSimulationContext MakeContext(
        PlayerConfig? config = null,
        PlayerDependencyData? deps = null,
        PlayerInputData? input = null,
        float delta = 1f / 60f
    )
    {
        return new PlayerSimulationContext(
            input ?? MakeInput(),
            deps ?? MakeDeps(),
            config ?? MakeConfig(),
            _cc,
            delta
        );
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
        var ctx = MakeContext(config: config, deps: deps, input: input, delta: 0.1f);

        PlayerSimulation.TickVerticalMovement(ctx, ref state);

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
        var ctx = MakeContext(config: config, deps: deps, input: input);

        PlayerSimulation.TickVerticalMovement(ctx, ref state);

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
        var ctx = MakeContext(config: config, deps: deps, input: input);

        PlayerSimulation.TickVerticalMovement(ctx, ref state);

        Assert.AreEqual(Vector3.zero, state.GrappleImpulse);
    }

    [Test]
    public void TickVerticalMovement_GroundedButRising_DoesNotClamp()
    {
        var config = MakeConfig(gravity: 0f, sprintSpeed: 7f, terminalVelocity: 1000f);
        var deps = MakeDeps(movementState: PlayerMovementState.Idling);
        var input = MakeInput();
        var state = MakeState(velocity: new Vector3(0f, 5f, 0f), lastSimulatedMovementState: PlayerMovementState.Idling);
        var ctx = MakeContext(config: config, deps: deps, input: input);

        PlayerSimulation.TickVerticalMovement(ctx, ref state);

        Assert.AreEqual(5f, state.Velocity.y, Tolerance);
    }

    [Test]
    public void TickVerticalMovement_AirborneAndFalling_DoesNotClamp()
    {
        var config = MakeConfig(gravity: 0f, sprintSpeed: 7f, terminalVelocity: 1000f);
        var deps = MakeDeps(movementState: PlayerMovementState.Falling);
        var input = MakeInput();
        var state = MakeState(velocity: new Vector3(0f, -5f, 0f), lastSimulatedMovementState: PlayerMovementState.Falling);
        var ctx = MakeContext(config: config, deps: deps, input: input);

        PlayerSimulation.TickVerticalMovement(ctx, ref state);

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
        var ctx = MakeContext(config: config, deps: deps, input: input, delta: 0.1f);

        PlayerSimulation.TickVerticalMovement(ctx, ref state);

        Assert.AreEqual(Mathf.Sqrt(1f * 3f * 25f), state.Velocity.y, Tolerance);
        Assert.IsTrue(state.JumpedLastSimulatedFrame);
    }

    [Test]
    public void TickVerticalMovement_AirborneAndJumpPressed_DoesNotJump()
    {
        var config = MakeConfig(gravity: 0f, jumpSpeed: 1f, sprintSpeed: 0f, terminalVelocity: 1000f);
        var deps = MakeDeps(movementState: PlayerMovementState.Falling);
        var input = MakeInput(jumpPressed: true);
        var state = MakeState(velocity: Vector3.zero, lastSimulatedMovementState: PlayerMovementState.Falling);
        var ctx = MakeContext(config: config, deps: deps, input: input);

        PlayerSimulation.TickVerticalMovement(ctx, ref state);

        Assert.AreEqual(0f, state.Velocity.y, Tolerance);
        Assert.IsFalse(state.JumpedLastSimulatedFrame);
    }

    [Test]
    public void TickVerticalMovement_GroundedButJumpNotPressed_DoesNotSetJumpedFlag()
    {
        var config = MakeConfig(gravity: 0f, jumpSpeed: 1f, sprintSpeed: 0f, terminalVelocity: 1000f);
        var deps = MakeDeps(movementState: PlayerMovementState.Idling);
        var input = MakeInput(jumpPressed: false);
        var state = MakeState(velocity: Vector3.zero, lastSimulatedMovementState: PlayerMovementState.Idling);
        var ctx = MakeContext(config: config, deps: deps, input: input);

        PlayerSimulation.TickVerticalMovement(ctx, ref state);

        Assert.IsFalse(state.JumpedLastSimulatedFrame);
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
        var ctx = MakeContext(config: config, deps: deps, input: input);

        PlayerSimulation.TickVerticalMovement(ctx, ref state);

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
        var ctx = MakeContext(config: config, deps: deps, input: input);

        PlayerSimulation.TickVerticalMovement(ctx, ref state);

        Assert.AreEqual(5f, state.Velocity.y, Tolerance);
    }

    [Test]
    public void TickVerticalMovement_LastFrameAirborneAndStillAirborne_DoesNotAddAntiBump()
    {
        var config = MakeConfig(gravity: 0f, sprintSpeed: 10f, terminalVelocity: 1000f);
        var deps = MakeDeps(multiplier: 1f, movementState: PlayerMovementState.Falling);
        var input = MakeInput();
        var state = MakeState(velocity: Vector3.zero, lastSimulatedMovementState: PlayerMovementState.Falling);
        var ctx = MakeContext(config: config, deps: deps, input: input);

        PlayerSimulation.TickVerticalMovement(ctx, ref state);

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
        var ctx = MakeContext(config: config, deps: deps, input: input);

        PlayerSimulation.TickVerticalMovement(ctx, ref state);

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
        var ctx = MakeContext(config: config, deps: deps, input: input);

        PlayerSimulation.TickVerticalMovement(ctx, ref state);

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
        var ctx = MakeContext(config: config, deps: deps, input: input, delta: 0.1f);

        PlayerSimulation.TickVerticalMovement(ctx, ref state);

        Assert.AreEqual(3f, state.Velocity.x, Tolerance);
        Assert.AreEqual(5f, state.Velocity.z, Tolerance);
    }

    #endregion

    #endregion

    #region TickLateralMovement

    // Branches in TickLateralMovement (in execution order):
    //   A. Sliding early-return  -> delegates to HandleSlideMovement
    //   B. Acceleration selector (4 paths): inAir, crouch, sprint, run
    //   C. Clamp magnitude selector (4 paths): same conditions as B
    //   D. Camera basis from CameraYaw -> movementDirection
    //   E. Train velocity offset subtracted from cc.velocity
    //   F. Drag clamp (2 paths): magnitude > drag*delta -> subtract; else snap to zero
    //   G. ClampMagnitude on XZ to clampLateralMagnitude
    //   H. y preservation: newVelocity.y = state.Velocity.y (twice)
    //   I. HandleSteepWalls only when !isGrounded (no-op without ground colliders)
    //   J. Impulse: ConsumeImpulse adds GrappleImpulse to velocity; TickImpulse decays it
    //   K. Train knockback added to state.Velocity.y at the end
    //
    // Acceleration tests use drag=0 so newVelocity = movementDirection*acc*delta exactly,
    // skipping Branch F. Drag tests set acc=0 so the drag math is the only effect.
    //
    // The CharacterController fixture in [SetUp] starts with cc.velocity=Vector3.zero
    // (no Move called yet), so localVelocity = -trainVelocityOffset cleanly.

    #region Branch A: Sliding

    [Test]
    public void TickLateralMovement_Sliding_DelegatesToHandleSlideMovementAndDecrementsTimer()
    {
        // Sliding path takes the early return into HandleSlideMovement, which decrements
        // SlideTimer by `delta` on the flat-ground branch (no slope -> else branch).
        var config = MakeConfig();
        var deps = MakeDeps(movementState: PlayerMovementState.Sliding);
        var state = MakeState(slideTimer: 1f);
        var ctx = MakeContext(config: config, deps: deps);

        PlayerSimulation.TickLateralMovement(ctx, ref state);

        Assert.AreEqual(1f - 1f / 60f, state.SlideTimer, Tolerance);
    }

    [Test]
    public void TickLateralMovement_Sliding_SkipsRunSprintAccelerationPath()
    {
        // If the slide branch were not taken, with input=(0,1) and runAcc=35 we'd get
        // forward thrust on z. Confirm slide path keeps lateral velocity at 0 (since
        // cc.velocity=0, slideDirection is zero -> slideVelocity is zero).
        var config = MakeConfig();
        var deps = MakeDeps(movementState: PlayerMovementState.Sliding);
        var input = MakeInput(movementInput: new Vector2(0f, 1f));
        var state = MakeState(slideTimer: 1f);
        var ctx = MakeContext(config: config, deps: deps, input: input);

        PlayerSimulation.TickLateralMovement(ctx, ref state);

        Assert.AreEqual(0f, state.Velocity.x, Tolerance);
        Assert.AreEqual(0f, state.Velocity.z, Tolerance);
    }

    #endregion

    #region Branch B: Acceleration selector

    [Test]
    public void TickLateralMovement_Falling_UsesInAirAcceleration()
    {
        // drag=0 -> Branch F produces newVelocity = movementDelta directly.
        // input=(0,1), yaw=0 -> movementDirection=(0,0,1), so result.z = inAirAcc * delta.
        var config = MakeConfig(inAirAcc: 25f, runAcc: 999f, sprintAcc: 999f, crouchAcc: 999f, drag: 0f, sprintSpeed: 1000f);
        var deps = MakeDeps(movementState: PlayerMovementState.Falling);
        var input = MakeInput(movementInput: new Vector2(0f, 1f));
        var state = MakeState();
        var ctx = MakeContext(config: config, deps: deps, input: input);

        PlayerSimulation.TickLateralMovement(ctx, ref state);

        Assert.AreEqual(25f / 60f, state.Velocity.z, Tolerance);
        Assert.AreEqual(0f, state.Velocity.x, Tolerance);
    }

    [Test]
    public void TickLateralMovement_Crouching_UsesCrouchAcceleration()
    {
        var config = MakeConfig(inAirAcc: 999f, runAcc: 999f, sprintAcc: 999f, crouchAcc: 13f, drag: 0f, crouchSpeed: 1000f);
        var deps = MakeDeps(movementState: PlayerMovementState.Crouching);
        var input = MakeInput(movementInput: new Vector2(0f, 1f));
        var state = MakeState();
        var ctx = MakeContext(config: config, deps: deps, input: input);

        PlayerSimulation.TickLateralMovement(ctx, ref state);

        Assert.AreEqual(13f / 60f, state.Velocity.z, Tolerance);
    }

    [Test]
    public void TickLateralMovement_Sprinting_UsesSprintAcceleration()
    {
        var config = MakeConfig(inAirAcc: 999f, runAcc: 999f, sprintAcc: 50f, crouchAcc: 999f, drag: 0f, sprintSpeed: 1000f);
        var deps = MakeDeps(movementState: PlayerMovementState.Sprinting);
        var input = MakeInput(movementInput: new Vector2(0f, 1f));
        var state = MakeState();
        var ctx = MakeContext(config: config, deps: deps, input: input);

        PlayerSimulation.TickLateralMovement(ctx, ref state);

        Assert.AreEqual(50f / 60f, state.Velocity.z, Tolerance);
    }

    [Test]
    public void TickLateralMovement_Running_UsesRunAcceleration()
    {
        var config = MakeConfig(inAirAcc: 999f, runAcc: 35f, sprintAcc: 999f, crouchAcc: 999f, drag: 0f, runSpeed: 1000f);
        var deps = MakeDeps(movementState: PlayerMovementState.Running);
        var input = MakeInput(movementInput: new Vector2(0f, 1f));
        var state = MakeState();
        var ctx = MakeContext(config: config, deps: deps, input: input);

        PlayerSimulation.TickLateralMovement(ctx, ref state);

        Assert.AreEqual(35f / 60f, state.Velocity.z, Tolerance);
    }

    [Test]
    public void TickLateralMovement_Idling_UsesRunAccelerationFallthrough()
    {
        // Idling is grounded but neither sprinting nor crouching -> falls through to runAcc.
        var config = MakeConfig(inAirAcc: 999f, runAcc: 35f, sprintAcc: 999f, crouchAcc: 999f, drag: 0f, runSpeed: 1000f);
        var deps = MakeDeps(movementState: PlayerMovementState.Idling);
        var input = MakeInput(movementInput: new Vector2(0f, 1f));
        var state = MakeState();
        var ctx = MakeContext(config: config, deps: deps, input: input);

        PlayerSimulation.TickLateralMovement(ctx, ref state);

        Assert.AreEqual(35f / 60f, state.Velocity.z, Tolerance);
    }

    #endregion

    #region Branch C: Clamp magnitude selector

    [Test]
    public void TickLateralMovement_Falling_ClampsToSprintSpeed()
    {
        // High acc + low clampSpeed forces ClampMagnitude to bite. With acc=1000, delta=1/60
        // -> movementDelta = 16.67 (much larger than sprintSpeed=2).
        var config = MakeConfig(inAirAcc: 1000f, drag: 0f, sprintSpeed: 2f);
        var deps = MakeDeps(movementState: PlayerMovementState.Falling);
        var input = MakeInput(movementInput: new Vector2(0f, 1f));
        var state = MakeState();
        var ctx = MakeContext(config: config, deps: deps, input: input);

        PlayerSimulation.TickLateralMovement(ctx, ref state);

        Assert.AreEqual(2f, state.Velocity.z, Tolerance);
    }

    [Test]
    public void TickLateralMovement_Crouching_ClampsToCrouchSpeed()
    {
        var config = MakeConfig(crouchAcc: 1000f, drag: 0f, crouchSpeed: 1.5f);
        var deps = MakeDeps(movementState: PlayerMovementState.Crouching);
        var input = MakeInput(movementInput: new Vector2(0f, 1f));
        var state = MakeState();
        var ctx = MakeContext(config: config, deps: deps, input: input);

        PlayerSimulation.TickLateralMovement(ctx, ref state);

        Assert.AreEqual(1.5f, state.Velocity.z, Tolerance);
    }

    [Test]
    public void TickLateralMovement_Sprinting_ClampsToSprintSpeed()
    {
        var config = MakeConfig(sprintAcc: 1000f, drag: 0f, sprintSpeed: 6f);
        var deps = MakeDeps(movementState: PlayerMovementState.Sprinting);
        var input = MakeInput(movementInput: new Vector2(0f, 1f));
        var state = MakeState();
        var ctx = MakeContext(config: config, deps: deps, input: input);

        PlayerSimulation.TickLateralMovement(ctx, ref state);

        Assert.AreEqual(6f, state.Velocity.z, Tolerance);
    }

    [Test]
    public void TickLateralMovement_Running_ClampsToRunSpeed()
    {
        var config = MakeConfig(runAcc: 1000f, drag: 0f, runSpeed: 3f);
        var deps = MakeDeps(movementState: PlayerMovementState.Running);
        var input = MakeInput(movementInput: new Vector2(0f, 1f));
        var state = MakeState();
        var ctx = MakeContext(config: config, deps: deps, input: input);

        PlayerSimulation.TickLateralMovement(ctx, ref state);

        Assert.AreEqual(3f, state.Velocity.z, Tolerance);
    }

    [Test]
    public void TickLateralMovement_Running_BelowClamp_DoesNotClamp()
    {
        // Confirm the clamp only fires when newVelocity exceeds it: result should be the
        // unclamped accel*delta value.
        var config = MakeConfig(runAcc: 35f, drag: 0f, runSpeed: 100f);
        var deps = MakeDeps(movementState: PlayerMovementState.Running);
        var input = MakeInput(movementInput: new Vector2(0f, 1f));
        var state = MakeState();
        var ctx = MakeContext(config: config, deps: deps, input: input);

        PlayerSimulation.TickLateralMovement(ctx, ref state);

        Assert.AreEqual(35f / 60f, state.Velocity.z, Tolerance);
    }

    #endregion

    #region Branch D: Camera basis from yaw

    [Test]
    public void TickLateralMovement_YawZero_ForwardInput_ProducesPositiveZ()
    {
        var config = MakeConfig(runAcc: 35f, drag: 0f, runSpeed: 1000f);
        var deps = MakeDeps(movementState: PlayerMovementState.Running);
        var input = MakeInput(movementInput: new Vector2(0f, 1f));
        var state = MakeState(cameraYaw: 0f);
        var ctx = MakeContext(config: config, deps: deps, input: input);

        PlayerSimulation.TickLateralMovement(ctx, ref state);

        Assert.AreEqual(0f, state.Velocity.x, Tolerance);
        Assert.AreEqual(35f / 60f, state.Velocity.z, Tolerance);
    }

    [Test]
    public void TickLateralMovement_Yaw90_ForwardInput_ProducesPositiveX()
    {
        var config = MakeConfig(runAcc: 35f, drag: 0f, runSpeed: 1000f);
        var deps = MakeDeps(movementState: PlayerMovementState.Running);
        var input = MakeInput(movementInput: new Vector2(0f, 1f));
        var state = MakeState(cameraYaw: 90f);
        var ctx = MakeContext(config: config, deps: deps, input: input);

        PlayerSimulation.TickLateralMovement(ctx, ref state);

        Assert.AreEqual(35f / 60f, state.Velocity.x, Tolerance);
        Assert.AreEqual(0f, state.Velocity.z, Tolerance);
    }

    [Test]
    public void TickLateralMovement_YawZero_StrafeRightInput_ProducesPositiveX()
    {
        var config = MakeConfig(runAcc: 35f, drag: 0f, runSpeed: 1000f);
        var deps = MakeDeps(movementState: PlayerMovementState.Running);
        var input = MakeInput(movementInput: new Vector2(1f, 0f));
        var state = MakeState(cameraYaw: 0f);
        var ctx = MakeContext(config: config, deps: deps, input: input);

        PlayerSimulation.TickLateralMovement(ctx, ref state);

        Assert.AreEqual(35f / 60f, state.Velocity.x, Tolerance);
        Assert.AreEqual(0f, state.Velocity.z, Tolerance);
    }

    [Test]
    public void TickLateralMovement_Yaw180_ForwardInput_ProducesNegativeZ()
    {
        var config = MakeConfig(runAcc: 35f, drag: 0f, runSpeed: 1000f);
        var deps = MakeDeps(movementState: PlayerMovementState.Running);
        var input = MakeInput(movementInput: new Vector2(0f, 1f));
        var state = MakeState(cameraYaw: 180f);
        var ctx = MakeContext(config: config, deps: deps, input: input);

        PlayerSimulation.TickLateralMovement(ctx, ref state);

        Assert.AreEqual(0f, state.Velocity.x, Tolerance);
        Assert.AreEqual(-35f / 60f, state.Velocity.z, Tolerance);
    }

    #endregion

    #region Branch E: Train velocity offset

    [Test]
    public void TickLateralMovement_TrainVelocityOffset_SubtractedFromLocalVelocity()
    {
        // No input, no drag, no acc -> newVelocity = -trainVelocityOffset (XZ part).
        // Then state.Velocity.x = -trainVelocityOffset.x.
        var config = MakeConfig(runAcc: 0f, drag: 0f, runSpeed: 1000f);
        var deps = MakeDeps(
            movementState: PlayerMovementState.Running,
            trainVelocityOffset: new Vector3(5f, 0f, 0f)
        );
        var input = MakeInput();
        var state = MakeState();
        var ctx = MakeContext(config: config, deps: deps, input: input);

        PlayerSimulation.TickLateralMovement(ctx, ref state);

        Assert.AreEqual(-5f, state.Velocity.x, Tolerance);
        Assert.AreEqual(0f, state.Velocity.z, Tolerance);
    }

    #endregion

    #region Branch F: Drag clamp

    [Test]
    public void TickLateralMovement_NewVelocityAboveDragThreshold_SubtractsDrag()
    {
        // movementDelta magnitude = runAcc*delta = 35/60 ≈ 0.583
        // drag*delta = 20/60 ≈ 0.333
        // 0.583 > 0.333 -> subtract drag along velocity direction
        // result = (35-20)/60 = 15/60 = 0.25 along z.
        var config = MakeConfig(runAcc: 35f, drag: 20f, runSpeed: 1000f);
        var deps = MakeDeps(movementState: PlayerMovementState.Running);
        var input = MakeInput(movementInput: new Vector2(0f, 1f));
        var state = MakeState();
        var ctx = MakeContext(config: config, deps: deps, input: input);

        PlayerSimulation.TickLateralMovement(ctx, ref state);

        Assert.AreEqual(15f / 60f, state.Velocity.z, Tolerance);
    }

    [Test]
    public void TickLateralMovement_NewVelocityBelowDragThreshold_SnapsToZero()
    {
        // movementDelta = movementDirection*acc*delta. With small input.y=0.4, acc=35, delta=1/60:
        // movementDirection = (0, 0, 0.4); movementDelta = (0, 0, 35*0.4/60) = (0, 0, 0.233)
        // magnitude = 0.233; drag*delta = 20/60 = 0.333; 0.233 < 0.333 -> snap to zero.
        var config = MakeConfig(runAcc: 35f, drag: 20f, runSpeed: 1000f);
        var deps = MakeDeps(movementState: PlayerMovementState.Running);
        var input = MakeInput(movementInput: new Vector2(0f, 0.4f));
        var state = MakeState();
        var ctx = MakeContext(config: config, deps: deps, input: input);

        PlayerSimulation.TickLateralMovement(ctx, ref state);

        Assert.AreEqual(0f, state.Velocity.x, Tolerance);
        Assert.AreEqual(0f, state.Velocity.z, Tolerance);
    }

    #endregion

    #region Branch H: Vertical (y) preservation

    [Test]
    public void TickLateralMovement_PreservesStateVelocityY()
    {
        // ClampMagnitude zeros y, then newVelocity.y = state.Velocity.y restores it.
        // No train knockback, so final state.Velocity.y == initial state.Velocity.y.
        var config = MakeConfig(runAcc: 35f, drag: 0f, runSpeed: 1000f);
        var deps = MakeDeps(movementState: PlayerMovementState.Running);
        var input = MakeInput(movementInput: new Vector2(0f, 1f));
        var state = MakeState(velocity: new Vector3(0f, 7f, 0f));
        var ctx = MakeContext(config: config, deps: deps, input: input);

        PlayerSimulation.TickLateralMovement(ctx, ref state);

        Assert.AreEqual(7f, state.Velocity.y, Tolerance);
    }

    #endregion

    #region Branch K: Train knockback vertical

    [Test]
    public void TickLateralMovement_TrainKnockbackVertical_AddedToStateVelocityY()
    {
        var config = MakeConfig(runAcc: 0f, drag: 0f, runSpeed: 1000f);
        var deps = MakeDeps(
            movementState: PlayerMovementState.Running,
            trainKnockbackVertical: 4.5f
        );
        var input = MakeInput();
        var state = MakeState(velocity: new Vector3(0f, 2f, 0f));
        var ctx = MakeContext(config: config, deps: deps, input: input);

        PlayerSimulation.TickLateralMovement(ctx, ref state);

        Assert.AreEqual(2f + 4.5f, state.Velocity.y, Tolerance);
    }

    #endregion

    #region Branch J: Grapple impulse

    [Test]
    public void TickLateralMovement_GrappleImpulse_ConsumedIntoVelocity()
    {
        // No input, no acc, no drag -> newVelocity starts at zero, then +GrappleImpulse.
        // State.Velocity.x = grapple.x. Y is overwritten to state.Velocity.y (preserved).
        var config = MakeConfig(runAcc: 0f, drag: 0f, runSpeed: 1000f);
        var deps = MakeDeps(movementState: PlayerMovementState.Running);
        var input = MakeInput();
        var state = MakeState(grappleImpulse: new Vector3(10f, 99f, -3f));
        var ctx = MakeContext(config: config, deps: deps, input: input);

        PlayerSimulation.TickLateralMovement(ctx, ref state);

        Assert.AreEqual(10f, state.Velocity.x, Tolerance);
        Assert.AreEqual(-3f, state.Velocity.z, Tolerance);
        Assert.AreEqual(0f, state.Velocity.y, Tolerance); // y comes from state.Velocity.y, not impulse
    }

    [Test]
    public void TickLateralMovement_GrappleImpulse_DecaysByMoveTowards()
    {
        // GrappleImpulseDecay = 10. delta = 1/60. Decay step = 10/60 = 0.16667.
        // From (10, 0, 0): magnitude 10; MoveTowards(10,0,0 -> 0,0,0; maxDelta=0.16667)
        // Result: (10 - 0.16667, 0, 0) = (9.8333, 0, 0).
        var config = MakeConfig(runAcc: 0f, drag: 0f, runSpeed: 1000f);
        var deps = MakeDeps(movementState: PlayerMovementState.Running);
        var input = MakeInput();
        var state = MakeState(grappleImpulse: new Vector3(10f, 0f, 0f));
        var ctx = MakeContext(config: config, deps: deps, input: input);

        PlayerSimulation.TickLateralMovement(ctx, ref state);

        Assert.AreEqual(10f - 10f / 60f, state.GrappleImpulse.x, Tolerance);
        Assert.AreEqual(0f, state.GrappleImpulse.y, Tolerance);
        Assert.AreEqual(0f, state.GrappleImpulse.z, Tolerance);
    }

    [Test]
    public void TickLateralMovement_GrappleImpulse_TinyImpulseSnapsToZero()
    {
        // sqrMagnitude <= 0.001 -> snaps to Vector3.zero. (0.01)^2 = 0.0001 < 0.001.
        var config = MakeConfig(runAcc: 0f, drag: 0f, runSpeed: 1000f);
        var deps = MakeDeps(movementState: PlayerMovementState.Running);
        var input = MakeInput();
        var state = MakeState(grappleImpulse: new Vector3(0.01f, 0f, 0f));
        var ctx = MakeContext(config: config, deps: deps, input: input);

        PlayerSimulation.TickLateralMovement(ctx, ref state);

        Assert.AreEqual(Vector3.zero, state.GrappleImpulse);
    }

    #endregion

    #region HandleSteepWalls (no-collider passthrough)

    // Without ground colliders the sphere cast misses, normal defaults to Vector3.up,
    // angle=0, validAngle=true (always), so no projection happens. This documents that
    // the !isGrounded branch is taken (HandleSteepWalls is invoked) but the function is
    // a noop in this fixture. Steep-wall projection requires a tilted ground collider
    // and is left for an integration test.

    [Test]
    public void TickLateralMovement_Falling_HandleSteepWallsIsNoopWithoutColliders()
    {
        var config = MakeConfig(inAirAcc: 25f, drag: 0f, sprintSpeed: 1000f);
        var deps = MakeDeps(movementState: PlayerMovementState.Falling);
        var input = MakeInput(movementInput: new Vector2(0f, 1f));
        var state = MakeState();
        var ctx = MakeContext(config: config, deps: deps, input: input);

        PlayerSimulation.TickLateralMovement(ctx, ref state);

        // Same as the grounded inAir-equivalent: the steep-wall noop preserves velocity.
        Assert.AreEqual(25f / 60f, state.Velocity.z, Tolerance);
    }

    #endregion

    #endregion
}
