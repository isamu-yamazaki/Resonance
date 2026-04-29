using System;
using PurrNet;
using UnityEngine;

namespace Resonance.Assemblies.Train
{
    [DefaultExecutionOrder(-10)]
    public class TrainController : NetworkBehaviour
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

        public TrainMovementState CurrentState => _state.movementState;
        public TrainDirection Direction => _state.direction;
        public int CurrentStationIndex => _state.currentStationIndex;
        public int NextStationIndex => _state.nextStationIndex;
        public float CurrentSpeed => isServer ? _state.currentSpeed : Velocity.magnitude;
        public float NormalizedSpeed => _config.maxSpeed > 0f ? CurrentSpeed / _config.maxSpeed : 0f;
        public Vector3 Velocity { get; private set; }
        public Vector3 MoveDirection { get; private set; }
        public string NextStationDisplayName => IsValidIndex(NextStationIndex)
            ? _stations[NextStationIndex].DisplayName
            : string.Empty;

        private TrainState _state;
        private TrainStationData[] _stationData = Array.Empty<TrainStationData>();
        private TrainState _prevTickState;
        private bool _hasPrevTickState;
        private Vector3 _lastFramePosition;
        private bool _hasLastFramePosition;
        private bool _hasReceivedFirstSnapshot;

        private void Awake()
        {
            if (_stations == null || _stations.Length < 2)
            {
                Debug.LogWarning("[TrainController] Fewer than 2 stations assigned.", this);
            }

            BuildStationDataSnapshot();
            _state = BuildInitialState();
            transform.position = _state.position;
        }

        private TrainState BuildInitialState()
        {
            Vector3 startPos = IsValidIndex(0) ? _stations[0].StopPosition : transform.position;

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

        private void FixedUpdate()
        {
            if (!isServer) return;

            if (!_hasPrevTickState)
            {
                _prevTickState = _state;
                _hasPrevTickState = true;
            }

            TrainSimulation.Step(ref _state, _config, _stationData, Time.fixedDeltaTime);
            transform.position = _state.position;

            DetectAndBroadcastTransitions(_prevTickState, _state);
            _prevTickState = _state;
        }

        private void Update()
        {
            float dt = Time.deltaTime;
            if (_hasLastFramePosition && dt > 0f)
            {
                Vector3 displacement = transform.position - _lastFramePosition;
                Velocity = displacement / dt;
                MoveDirection = Velocity.sqrMagnitude > 1e-6f ? Velocity.normalized : Vector3.zero;
            }
            _lastFramePosition = transform.position;
            _hasLastFramePosition = true;
        }

        protected override void OnObserverAdded(PlayerID player)
        {
            base.OnObserverAdded(player);
            SendInitialSnapshotTargetRpc(
                player,
                _state.movementState,
                _state.direction,
                _state.currentStationIndex,
                _state.nextStationIndex,
                _state.preDepartFired);
        }

        private void DetectAndBroadcastTransitions(in TrainState prev, in TrainState next)
        {
            if (next.movementState != prev.movementState)
                BroadcastStateChangedRpc(next.movementState);

            if (next.currentStationIndex != prev.currentStationIndex
                && IsValidIndex(next.currentStationIndex))
                BroadcastArrivedRpc(next.currentStationIndex);

            if (prev.movementState == TrainMovementState.StoppedAtStation
                && next.movementState == TrainMovementState.Accelerating
                && IsValidIndex(next.currentStationIndex))
                BroadcastDepartedRpc(next.currentStationIndex);

            if (!prev.preDepartFired && next.preDepartFired
                && IsValidIndex(next.currentStationIndex))
                BroadcastPreDepartRpc(next.currentStationIndex);

            if (next.nextStationIndex != prev.nextStationIndex
                && IsValidIndex(next.nextStationIndex))
                BroadcastNextStationChangedRpc(next.nextStationIndex, next.direction);
        }

        [ObserversRpc(runLocally: true)]
        private void BroadcastStateChangedRpc(TrainMovementState movementState)
        {
            _state.movementState = movementState;
            OnStateChanged?.Invoke(movementState);
        }

        [ObserversRpc(runLocally: true)]
        private void BroadcastArrivedRpc(int stationIndex)
        {
            if (!IsValidIndex(stationIndex)) return;
            _state.currentStationIndex = stationIndex;
            OnArrivedAtStation?.Invoke(stationIndex, _stations[stationIndex]);
        }

        [ObserversRpc(runLocally: true)]
        private void BroadcastDepartedRpc(int stationIndex)
        {
            if (!IsValidIndex(stationIndex)) return;
            OnDepartedStation?.Invoke(stationIndex, _stations[stationIndex]);
        }

        [ObserversRpc(runLocally: true)]
        private void BroadcastPreDepartRpc(int stationIndex)
        {
            if (!IsValidIndex(stationIndex)) return;
            OnPreDepart?.Invoke(stationIndex, _stations[stationIndex]);
        }

        [ObserversRpc(runLocally: true)]
        private void BroadcastNextStationChangedRpc(int nextIndex, TrainDirection direction)
        {
            if (!IsValidIndex(nextIndex)) return;
            _state.nextStationIndex = nextIndex;
            _state.direction = direction;
            OnNextStationChanged?.Invoke(nextIndex, _stations[nextIndex]);
        }

        [TargetRpc]
        private void SendInitialSnapshotTargetRpc(
            PlayerID target,
            TrainMovementState movementState,
            TrainDirection direction,
            int currentStationIndex,
            int nextStationIndex,
            bool preDepartFired)
        {
            _state.movementState = movementState;
            _state.direction = direction;
            _state.currentStationIndex = currentStationIndex;
            _state.nextStationIndex = nextStationIndex;
            _state.preDepartFired = preDepartFired;

            if (_hasReceivedFirstSnapshot) return;
            _hasReceivedFirstSnapshot = true;

            if (movementState != TrainMovementState.StoppedAtStation)
            {
                OnFirstVerifiedViewStateIsAlreadyMoving?.Invoke();
            }
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
