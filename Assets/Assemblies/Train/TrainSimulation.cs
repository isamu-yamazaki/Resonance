using UnityEngine;

namespace Resonance.Assemblies.Train
{
    public static class TrainSimulation
    {
        public static void Step(
            ref TrainState state,
            in TrainConfig config,
            TrainStationData[] stations,
            float delta)
        {
            switch (state.movementState)
            {
                case TrainMovementState.StoppedAtStation:
                    TickStopped(ref state, config, delta);
                    break;
                case TrainMovementState.Accelerating:
                case TrainMovementState.Cruising:
                case TrainMovementState.Braking:
                    TickMovement(ref state, config, stations, delta);
                    break;
            }
        }

        private static void TickStopped(ref TrainState state, in TrainConfig config, float delta)
        {
            state.currentSpeed = 0f;
            state.stopTimer -= delta;

            if (!state.preDepartFired && state.stopTimer <= config.preDepartWarningTime)
            {
                state.preDepartFired = true;
            }

            if (state.stopTimer <= 0f)
            {
                state.movementState = TrainMovementState.Accelerating;
            }
        }

        private static void TickMovement(
            ref TrainState state,
            in TrainConfig config,
            TrainStationData[] stations,
            float delta)
        {
            if (!IsValidIndex(state.nextStationIndex, stations))
            {
                return;
            }

            Vector3 targetPos = stations[state.nextStationIndex].stopPosition;
            Vector3 toTarget = targetPos - state.position;
            float distance = toTarget.magnitude;
            Vector3 moveDirection = distance > 0f ? toTarget / distance : Vector3.zero;

            float brakeDist = config.Deceleration > 0f
                ? (state.currentSpeed * state.currentSpeed) / (2f * config.Deceleration)
                : 0f;

            if (distance <= config.arrivalTolerance)
            {
                Arrive(ref state, config, stations);
                return;
            }

            if (distance <= brakeDist + config.arrivalTolerance)
            {
                state.movementState = TrainMovementState.Braking;
            }
            else if (state.currentSpeed >= config.maxSpeed)
            {
                state.movementState = TrainMovementState.Cruising;
            }
            else if (state.movementState != TrainMovementState.Accelerating)
            {
                state.movementState = TrainMovementState.Accelerating;
            }

            switch (state.movementState)
            {
                case TrainMovementState.Accelerating:
                    state.currentSpeed = Mathf.MoveTowards(
                        state.currentSpeed, config.maxSpeed, config.Acceleration * delta);
                    break;
                case TrainMovementState.Cruising:
                    state.currentSpeed = config.maxSpeed;
                    break;
                case TrainMovementState.Braking:
                    state.currentSpeed = Mathf.MoveTowards(
                        state.currentSpeed, 0f, config.Deceleration * delta);
                    break;
            }

            float step = state.currentSpeed * delta;
            state.position += moveDirection * Mathf.Min(step, distance);
        }

        private static void Arrive(ref TrainState state, in TrainConfig config, TrainStationData[] stations)
        {
            state.currentSpeed = 0f;
            state.position = stations[state.nextStationIndex].stopPosition;
            state.currentStationIndex = state.nextStationIndex;
            state.movementState = TrainMovementState.StoppedAtStation;
            state.stopTimer = config.stationStopDuration + config.preDepartWarningTime;
            state.preDepartFired = false;
            AdvanceTarget(ref state, stations);
        }

        private static void AdvanceTarget(ref TrainState state, TrainStationData[] stations)
        {
            int candidate = state.currentStationIndex + (int)state.direction;

            if (candidate < 0 || candidate >= stations.Length)
            {
                state.direction = state.direction == TrainDirection.Forward
                    ? TrainDirection.Backward
                    : TrainDirection.Forward;
                candidate = state.currentStationIndex + (int)state.direction;
            }

            state.nextStationIndex = candidate;
        }

        private static bool IsValidIndex(int index, TrainStationData[] stations)
        {
            return stations != null && index >= 0 && index < stations.Length;
        }
    }
}
