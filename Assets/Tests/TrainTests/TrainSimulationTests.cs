using NUnit.Framework;
using Resonance.Assemblies.Train;
using UnityEngine;

public class TrainSimulationTests
{
    private const float Delta = 1f / 60f;

    private TrainConfig _config;
    private TrainStationData[] _stations;

    [SetUp]
    public void Setup()
    {
        _config = TrainConfig.Default;
        _stations = new[]
        {
            new TrainStationData(new Vector3(0f, 0f, 0f), "A"),
            new TrainStationData(new Vector3(100f, 0f, 0f), "B"),
            new TrainStationData(new Vector3(250f, 0f, 0f), "C"),
        };
    }

    private static TrainState InitialStateAt(Vector3 position, TrainStationData[] stations, TrainConfig config)
    {
        return new TrainState
        {
            position = position,
            currentSpeed = 0f,
            movementState = TrainMovementState.StoppedAtStation,
            direction = TrainDirection.Forward,
            currentStationIndex = 0,
            nextStationIndex = stations.Length > 1 ? 1 : 0,
            stopTimer = config.stationStopDuration + config.preDepartWarningTime,
            preDepartFired = false,
        };
    }

    private static void StepForSeconds(ref TrainState state, in TrainConfig config, TrainStationData[] stations, float seconds, float delta)
    {
        int steps = Mathf.CeilToInt(seconds / delta);
        for (int i = 0; i < steps; i++)
        {
            TrainSimulation.Step(ref state, config, stations, delta);
        }
    }

    #region Initial State

    [Test]
    public void InitialState_StoppedAtStation0_FacingForward()
    {
        TrainState state = InitialStateAt(_stations[0].stopPosition, _stations, _config);

        Assert.AreEqual(TrainMovementState.StoppedAtStation, state.movementState);
        Assert.AreEqual(TrainDirection.Forward, state.direction);
        Assert.AreEqual(0, state.currentStationIndex);
        Assert.AreEqual(1, state.nextStationIndex);
        Assert.AreEqual(0f, state.currentSpeed);
        Assert.IsFalse(state.preDepartFired);
    }

    #endregion

    #region Stopped-at-Station

    [Test]
    public void Stopped_PreDepartFires_AtCorrectElapsedTime()
    {
        TrainState state = InitialStateAt(_stations[0].stopPosition, _stations, _config);

        // Run slightly past the pre-depart threshold to avoid floating-point drift at the exact boundary.
        float secondsPastPreDepart = _config.stationStopDuration + 0.5f;
        StepForSeconds(ref state, _config, _stations, secondsPastPreDepart, Delta);

        Assert.IsTrue(state.preDepartFired, "preDepartFired should flip on once stopTimer crosses preDepartWarningTime.");
        Assert.AreEqual(TrainMovementState.StoppedAtStation, state.movementState);
    }

    [Test]
    public void Stopped_BeforePreDepartThreshold_DoesNotFirePreDepart()
    {
        TrainState state = InitialStateAt(_stations[0].stopPosition, _stations, _config);

        float secondsBeforePreDepart = _config.stationStopDuration - 1f;
        StepForSeconds(ref state, _config, _stations, secondsBeforePreDepart, Delta);

        Assert.IsFalse(state.preDepartFired);
    }

    [Test]
    public void Stopped_StopTimerElapsed_TransitionsToAccelerating()
    {
        TrainState state = InitialStateAt(_stations[0].stopPosition, _stations, _config);

        float totalStopTime = _config.stationStopDuration + _config.preDepartWarningTime + 0.1f;
        StepForSeconds(ref state, _config, _stations, totalStopTime, Delta);

        Assert.AreEqual(TrainMovementState.Accelerating, state.movementState);
    }

    #endregion

    #region Accelerating / Cruising / Braking

    [Test]
    public void Accelerating_SpeedRampsTowardsMax_OverAccelerationTime()
    {
        TrainState state = InitialStateAt(_stations[0].stopPosition, _stations, _config);
        state.movementState = TrainMovementState.Accelerating;
        state.stopTimer = 0f;

        StepForSeconds(ref state, _config, _stations, _config.accelerationTime, Delta);

        Assert.AreEqual(_config.maxSpeed, state.currentSpeed, 0.01f,
            "Speed should reach maxSpeed after accelerationTime seconds of constant accel.");
    }

    [Test]
    public void Cruising_SpeedEqualsMax_PositionAdvancesLinearly()
    {
        TrainState state = InitialStateAt(_stations[0].stopPosition, _stations, _config);
        state.movementState = TrainMovementState.Cruising;
        state.currentSpeed = _config.maxSpeed;
        state.stopTimer = 0f;

        Vector3 startPos = state.position;
        int steps = 30;
        for (int i = 0; i < steps; i++)
        {
            TrainSimulation.Step(ref state, _config, _stations, Delta);
        }

        float expectedDistance = _config.maxSpeed * steps * Delta;
        float actualDistance = Vector3.Distance(startPos, state.position);
        Assert.AreEqual(expectedDistance, actualDistance, 0.01f);
        Assert.AreEqual(_config.maxSpeed, state.currentSpeed, 0.001f);
    }

    [Test]
    public void Braking_StartsWhenDistanceToTargetApproachesBrakeDist()
    {
        TrainState state = InitialStateAt(_stations[0].stopPosition, _stations, _config);
        state.movementState = TrainMovementState.Cruising;
        state.currentSpeed = _config.maxSpeed;
        state.stopTimer = 0f;

        float brakeDist = (_config.maxSpeed * _config.maxSpeed) / (2f * _config.Deceleration);
        state.position = _stations[1].stopPosition - Vector3.right * (brakeDist - 0.5f);

        TrainSimulation.Step(ref state, _config, _stations, Delta);

        Assert.AreEqual(TrainMovementState.Braking, state.movementState,
            $"Cruising train should brake once within brakeDist ({brakeDist:F2}) of target. " +
            $"Distance: {Vector3.Distance(state.position, _stations[1].stopPosition):F2}");
    }

    #endregion

    #region Arrival

    [Test]
    public void Arrival_SnapsToStationPosition_AndAdvancesNextStation()
    {
        TrainState state = InitialStateAt(_stations[0].stopPosition, _stations, _config);
        state.movementState = TrainMovementState.Braking;
        state.currentSpeed = 0.1f;
        state.stopTimer = 0f;
        state.position = _stations[1].stopPosition - Vector3.right * (_config.arrivalTolerance * 0.5f);

        TrainSimulation.Step(ref state, _config, _stations, Delta);

        Assert.AreEqual(TrainMovementState.StoppedAtStation, state.movementState);
        Assert.AreEqual(_stations[1].stopPosition, state.position);
        Assert.AreEqual(1, state.currentStationIndex);
        Assert.AreEqual(2, state.nextStationIndex);
        Assert.AreEqual(0f, state.currentSpeed);
    }

    [Test]
    public void Arrival_ResetsStopTimer_AndClearsPreDepartFired()
    {
        TrainState state = InitialStateAt(_stations[0].stopPosition, _stations, _config);
        state.movementState = TrainMovementState.Braking;
        state.currentSpeed = 0.1f;
        state.stopTimer = 0f;
        state.preDepartFired = true;
        state.position = _stations[1].stopPosition;

        TrainSimulation.Step(ref state, _config, _stations, Delta);

        Assert.AreEqual(_config.stationStopDuration + _config.preDepartWarningTime, state.stopTimer, 0.001f);
        Assert.IsFalse(state.preDepartFired);
    }

    #endregion

    #region Direction Reversal

    [Test]
    public void EndOfLine_ReversesDirection_NextStationIsPriorStation()
    {
        TrainState state = InitialStateAt(_stations[2].stopPosition, _stations, _config);
        state.currentStationIndex = 1;
        state.nextStationIndex = 2;
        state.movementState = TrainMovementState.Braking;
        state.currentSpeed = 0.1f;
        state.stopTimer = 0f;
        state.position = _stations[2].stopPosition;

        TrainSimulation.Step(ref state, _config, _stations, Delta);

        Assert.AreEqual(2, state.currentStationIndex);
        Assert.AreEqual(1, state.nextStationIndex,
            "At end of line, train should reverse and target the prior station.");
        Assert.AreEqual(TrainDirection.Backward, state.direction);
    }

    #endregion

    #region Determinism

    [Test]
    public void Determinism_IdenticalInitialState_ProducesIdenticalTrajectory()
    {
        TrainState a = InitialStateAt(_stations[0].stopPosition, _stations, _config);
        TrainState b = InitialStateAt(_stations[0].stopPosition, _stations, _config);

        for (int i = 0; i < 600; i++)
        {
            TrainSimulation.Step(ref a, _config, _stations, Delta);
            TrainSimulation.Step(ref b, _config, _stations, Delta);
        }

        Assert.AreEqual(a.position, b.position);
        Assert.AreEqual(a.currentSpeed, b.currentSpeed);
        Assert.AreEqual(a.movementState, b.movementState);
        Assert.AreEqual(a.currentStationIndex, b.currentStationIndex);
        Assert.AreEqual(a.nextStationIndex, b.nextStationIndex);
        Assert.AreEqual(a.stopTimer, b.stopTimer);
        Assert.AreEqual(a.preDepartFired, b.preDepartFired);
        Assert.AreEqual(a.direction, b.direction);
    }

    [Test]
    public void Simulate_DoesNotMutateStationsArray()
    {
        TrainStationData[] originalSnapshot = (TrainStationData[])_stations.Clone();

        TrainState state = InitialStateAt(_stations[0].stopPosition, _stations, _config);
        for (int i = 0; i < 600; i++)
        {
            TrainSimulation.Step(ref state, _config, _stations, Delta);
        }

        for (int i = 0; i < _stations.Length; i++)
        {
            Assert.AreEqual(originalSnapshot[i].stopPosition, _stations[i].stopPosition);
            Assert.AreEqual(originalSnapshot[i].displayName, _stations[i].displayName);
        }
    }

    [Test]
    public void Simulate_WithZeroDelta_DoesNotAdvanceState()
    {
        TrainState state = InitialStateAt(_stations[0].stopPosition, _stations, _config);
        state.movementState = TrainMovementState.Cruising;
        state.currentSpeed = _config.maxSpeed;
        state.stopTimer = 0f;

        Vector3 startPos = state.position;
        float startSpeed = state.currentSpeed;
        for (int i = 0; i < 100; i++)
        {
            TrainSimulation.Step(ref state, _config, _stations, 0f);
        }

        Assert.AreEqual(startPos, state.position);
        Assert.AreEqual(startSpeed, state.currentSpeed);
    }

    #endregion
}
