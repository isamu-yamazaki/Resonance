using System;
using PurrNet.Prediction;
using UnityEngine;

namespace Resonance.Assemblies.Train
{
    [DefaultExecutionOrder(-10)]
    public class TrainController : PredictedIdentity<TrainState>
    {
        [Header("Stations")]
        [SerializeField] private TrainStation[] _stations;

        [Header("Config")]
        [SerializeField] private TrainConfig _config = TrainConfig.Default;

        public event Action<int, TrainStation> OnArrivedAtStation;
        public event Action<int, TrainStation> OnPreDepart;
        public event Action<int, TrainStation> OnDepartedStation;
        public event Action<int, TrainStation> OnNextStationChanged;
        public event Action<TrainMovementState> OnStateChanged;
        public event Action OnFirstVerifiedViewStateIsAlreadyMoving;

        public TrainMovementState CurrentState => currentState.movementState;
        public TrainDirection Direction => currentState.direction;
        public int CurrentStationIndex => currentState.currentStationIndex;
        public int NextStationIndex => currentState.nextStationIndex;
        public float CurrentSpeed => Velocity.magnitude;
        public float NormalizedSpeed => _config.maxSpeed > 0f ? CurrentSpeed / _config.maxSpeed : 0f;
        public Vector3 Velocity => currentState.velocity;
        public Vector3 MoveDirection => Velocity.sqrMagnitude > 1e-6f ? Velocity.normalized : Vector3.zero;
        public string NextStationDisplayName => IsValidIndex(NextStationIndex)
            ? _stations[NextStationIndex].DisplayName
            : string.Empty;

        private TrainStationData[] _stationData = Array.Empty<TrainStationData>();
        private TrainState? _previousVerifiedState;

        protected override void LateAwake()
        {
            if (_stations == null || _stations.Length < 2)
            {
                Debug.LogWarning("[TrainController] Fewer than 2 stations assigned.", this);
            }

            BuildStationDataSnapshot();
        }

        protected override TrainState GetInitialState()
        {
            Vector3 startPos = IsValidIndex(0) ? _stations[0].StopPosition : transform.position;

            return new TrainState
            {
                position = startPos,
                velocity = Vector3.zero,
                currentSpeed = 0f,
                movementState = TrainMovementState.StoppedAtStation,
                direction = TrainDirection.Forward,
                currentStationIndex = 0,
                nextStationIndex = (_stations != null && _stations.Length > 1) ? 1 : 0,
                stopTimer = _config.stationStopDuration + _config.preDepartWarningTime,
                preDepartFired = false,
            };
        }

        private void BuildStationDataSnapshot()
        {
            if (_stations == null)
            {
                _stationData = Array.Empty<TrainStationData>();
                return;
            }
            _stationData = new TrainStationData[_stations.Length];
            for (int i = 0; i < _stations.Length; i++)
            {
                if (_stations[i] == null)
                {
                    continue;
                }
                _stationData[i] = new TrainStationData(_stations[i].StopPosition, _stations[i].DisplayName);
            }
        }

        protected override void Simulate(ref TrainState state, float delta)
        {
            Vector3 previousPosition = state.position;

            TrainSimulation.Step(ref state, _config, _stationData, delta);
            transform.position = state.position;  // syncs to predicted transform

            // updates once per simulation tick
            state.velocity = delta > 0f
                ? (state.position - previousPosition) / delta
                : Vector3.zero;
        }

        protected override void UpdateView(TrainState viewState, TrainState? verified)
        {
            if (!verified.HasValue) return;
            var v = verified.Value;

            if (!_previousVerifiedState.HasValue)
            {
                _previousVerifiedState = v;
                if (v.movementState != TrainMovementState.StoppedAtStation)
                {
                    OnFirstVerifiedViewStateIsAlreadyMoving?.Invoke();
                }
                return;
            }

            DetectAndBroadcastTransitions(_previousVerifiedState.Value, v);
            _previousVerifiedState = v;
        }

        private void DetectAndBroadcastTransitions(in TrainState prev, in TrainState next)
        {
            if (next.movementState != prev.movementState)
                OnStateChanged?.Invoke(next.movementState);

            if (next.currentStationIndex != prev.currentStationIndex
                && IsValidIndex(next.currentStationIndex))
                OnArrivedAtStation?.Invoke(next.currentStationIndex, _stations[next.currentStationIndex]);

            if (prev.movementState == TrainMovementState.StoppedAtStation
                && next.movementState == TrainMovementState.Accelerating
                && IsValidIndex(next.currentStationIndex))
                OnDepartedStation?.Invoke(next.currentStationIndex, _stations[next.currentStationIndex]);

            if (!prev.preDepartFired && next.preDepartFired
                && IsValidIndex(next.currentStationIndex))
                OnPreDepart?.Invoke(next.currentStationIndex, _stations[next.currentStationIndex]);

            if (next.nextStationIndex != prev.nextStationIndex
                && IsValidIndex(next.nextStationIndex))
                OnNextStationChanged?.Invoke(next.nextStationIndex, _stations[next.nextStationIndex]);
        }

        private bool IsValidIndex(int index)
        {
            return _stations != null && index >= 0 && index < _stations.Length;
        }

        private void OnDrawGizmos()
        {
            if (_stations == null || _stations.Length == 0)
            {
                return;
            }

            Gizmos.color = new Color(0.3f, 1f, 0.5f, 0.6f);
            for (int i = 0; i < _stations.Length - 1; i++)
            {
                if (_stations[i] == null || _stations[i + 1] == null)
                {
                    continue;
                }
                Gizmos.DrawLine(_stations[i].StopPosition, _stations[i + 1].StopPosition);
            }

            if (Application.isPlaying && IsValidIndex(NextStationIndex))
            {
                Gizmos.color = Color.yellow;
                Gizmos.DrawWireSphere(_stations[NextStationIndex].StopPosition, 0.5f);
                Gizmos.DrawLine(transform.position, _stations[NextStationIndex].StopPosition);
            }
        }
    }
}
