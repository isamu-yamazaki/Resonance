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

        public TrainMovementState CurrentState => viewState.movementState;
        public TrainDirection Direction => viewState.direction;
        public int CurrentStationIndex => viewState.currentStationIndex;
        public int NextStationIndex => viewState.nextStationIndex;
        public float CurrentSpeed => viewState.currentSpeed;
        public float NormalizedSpeed => _config.maxSpeed > 0f ? CurrentSpeed / _config.maxSpeed : 0f;
        public Vector3 Velocity { get; private set; }
        public Vector3 MoveDirection { get; private set; }
        public string NextStationDisplayName => IsValidIndex(NextStationIndex)
            ? _stations[NextStationIndex].DisplayName
            : string.Empty;

        private TrainStationData[] _stationData = Array.Empty<TrainStationData>();
        private TrainState _previousViewState;
        private bool _hasPreviousViewState;
        private Vector3 _lastViewPosition;
        private bool _hasLastViewPosition;

        protected override void LateAwake()
        {
            if (_stations == null || _stations.Length < 2)
            {
                Debug.LogWarning("[TrainController] Fewer than 2 stations assigned.", this);
            }
            BuildStationDataSnapshot();
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

        protected override TrainState GetInitialState()
        {
            Vector3 startPos = IsValidIndex(0)
                ? _stations[0].StopPosition
                : transform.position;

            return new TrainState
            {
                position = startPos,
                currentSpeed = 0f,
                movementState = TrainMovementState.StoppedAtStation,
                direction = TrainDirection.Forward,
                currentStationIndex = 0,
                nextStationIndex = (_stations != null && _stations.Length > 1) ? 1 : 0,
                stopTimer = _config.stationStopDuration + _config.preDepartWarningTime,
                preDepartFired = false,
            };
        }

        protected override void Simulate(ref TrainState state, float delta)
        {
            TrainSimulation.Step(ref state, _config, _stationData, delta);
        }

        // No GetUnityState override: the simulation is authoritative over state.position.
        // Reading transform.position back into state every tick would undo Simulate's
        // advance — the transform only catches up at frame rate via UpdateView, so it
        // always lags the authoritative state.

        protected override void SetUnityState(TrainState state)
        {
            transform.position = state.position;
        }

        protected override void UpdateView(TrainState viewState, TrainState? verified)
        {
            transform.position = viewState.position;

            float tickDelta = predictionManager != null ? predictionManager.tickDelta : 0f;
            if (_hasLastViewPosition && tickDelta > 0f)
            {
                Vector3 displacement = viewState.position - _lastViewPosition;
                Velocity = displacement / tickDelta;
                MoveDirection = Velocity.sqrMagnitude > 1e-6f ? Velocity.normalized : Vector3.zero;
            }
            _lastViewPosition = viewState.position;
            _hasLastViewPosition = true;

            if (_hasPreviousViewState)
            {
                FireTransitionEvents(_previousViewState, viewState);
            }
            _previousViewState = viewState;
            _hasPreviousViewState = true;
        }

        private void FireTransitionEvents(TrainState prev, TrainState next)
        {
            if (next.movementState != prev.movementState)
            {
                OnStateChanged?.Invoke(next.movementState);
            }

            if (next.currentStationIndex != prev.currentStationIndex
                && IsValidIndex(next.currentStationIndex))
            {
                OnArrivedAtStation?.Invoke(next.currentStationIndex, _stations[next.currentStationIndex]);
            }

            if (prev.movementState == TrainMovementState.StoppedAtStation
                && next.movementState == TrainMovementState.Accelerating
                && IsValidIndex(next.currentStationIndex))
            {
                OnDepartedStation?.Invoke(next.currentStationIndex, _stations[next.currentStationIndex]);
            }

            if (!prev.preDepartFired && next.preDepartFired
                && IsValidIndex(next.currentStationIndex))
            {
                OnPreDepart?.Invoke(next.currentStationIndex, _stations[next.currentStationIndex]);
            }

            if (next.nextStationIndex != prev.nextStationIndex
                && IsValidIndex(next.nextStationIndex))
            {
                OnNextStationChanged?.Invoke(next.nextStationIndex, _stations[next.nextStationIndex]);
            }
        }

        protected override TrainState Interpolate(TrainState from, TrainState to, float t)
        {
            return new TrainState
            {
                position = Vector3.Lerp(from.position, to.position, t),
                currentSpeed = Mathf.Lerp(from.currentSpeed, to.currentSpeed, t),
                stopTimer = Mathf.Lerp(from.stopTimer, to.stopTimer, t),
                movementState = to.movementState,
                direction = to.direction,
                currentStationIndex = to.currentStationIndex,
                nextStationIndex = to.nextStationIndex,
                preDepartFired = to.preDepartFired,
            };
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
