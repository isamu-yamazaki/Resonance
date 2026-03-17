using System;
using UnityEngine;

namespace Resonance.Train
{
    [DefaultExecutionOrder(-10)]
    public class TrainController : MonoBehaviour
    {
        [Header("Stations")]
        [SerializeField] private TrainStation[] _stations;

        [Header("Movement")]
        [SerializeField] private float _maxSpeed = 14f;
        [SerializeField] private float _accelerationTime = 4f;
        [SerializeField] private float _decelerationTime = 3f;
        [SerializeField] private float _arrivalTolerance = 0.15f;

        [Header("Station Behaviour")]
        [SerializeField] private float _stationStopDuration = 15f;

        public event Action<int, TrainStation> OnArrivedAtStation;
        public event Action<int, TrainStation> OnDepartedStation;
        public event Action<int, TrainStation> OnNextStationChanged;
        public event Action<TrainState> OnStateChanged;

        public TrainState CurrentState { get; private set; } = TrainState.StoppedAtStation;
        public TrainDirection Direction { get; private set; } = TrainDirection.Forward;
        public int CurrentStationIndex { get; private set; } = 0;
        public int NextStationIndex { get; private set; } = 1;
        public float CurrentSpeed { get; private set; } = 0f;
        public float NormalizedSpeed => _maxSpeed > 0f ? CurrentSpeed / _maxSpeed : 0f;
        public Vector3 Velocity { get; private set; } = Vector3.zero;
        public Vector3 MoveDirection { get; private set; } = Vector3.zero;
        public string NextStationDisplayName => IsValidIndex(NextStationIndex)
            ? _stations[NextStationIndex].DisplayName
            : string.Empty;

        private float _stopTimer = 0f;
        private Vector3 _lastPosition = Vector3.zero;

        private float Acceleration => _accelerationTime > 0f ? _maxSpeed / _accelerationTime : _maxSpeed;
        private float Deceleration => _decelerationTime > 0f ? _maxSpeed / _decelerationTime : _maxSpeed;

        private void Awake()
        {
            if (_stations == null || _stations.Length < 2)
                Debug.LogWarning("[TrainController] Fewer than 2 stations assigned.", this);

            if (IsValidIndex(0))
                transform.position = _stations[0].StopPosition;

            _lastPosition = transform.position;
            CurrentStationIndex = 0;
            NextStationIndex = 1;
            Direction = TrainDirection.Forward;

            SetState(TrainState.StoppedAtStation);
            _stopTimer = _stationStopDuration;
        }

        private void FixedUpdate()
        {
            _lastPosition = transform.position;

            switch (CurrentState)
            {
                case TrainState.StoppedAtStation: TickStopped(); break;
                case TrainState.Accelerating: TickMovement(); break;
                case TrainState.Cruising: TickMovement(); break;
                case TrainState.Braking: TickMovement(); break;
            }

            Velocity = (transform.position - _lastPosition) / Time.fixedDeltaTime;
        }

        private void TickStopped()
        {
            CurrentSpeed = 0f;
            MoveDirection = Vector3.zero;

            _stopTimer -= Time.fixedDeltaTime;
            if (_stopTimer <= 0f)
                Depart();
        }

        private void TickMovement()
        {
            if (!IsValidIndex(NextStationIndex)) return;

            Vector3 targetPos = _stations[NextStationIndex].StopPosition;
            Vector3 toTarget = targetPos - transform.position;
            float distance = toTarget.magnitude;
            Vector3 moveDirection = toTarget.normalized;
            MoveDirection = moveDirection;

            float brakeDist = (CurrentSpeed * CurrentSpeed) / (2f * Deceleration);

            if (distance <= _arrivalTolerance)
            {
                Arrive();
                return;
            }
            else if (distance <= brakeDist + _arrivalTolerance)
            {
                SetState(TrainState.Braking);
            }
            else if (CurrentSpeed >= _maxSpeed)
            {
                SetState(TrainState.Cruising);
            }
            else if (CurrentState != TrainState.Accelerating)
            {
                SetState(TrainState.Accelerating);
            }

            switch (CurrentState)
            {
                case TrainState.Accelerating:
                    CurrentSpeed = Mathf.MoveTowards(CurrentSpeed, _maxSpeed, Acceleration * Time.fixedDeltaTime);
                    break;

                case TrainState.Cruising:
                    CurrentSpeed = _maxSpeed;
                    break;

                case TrainState.Braking:
                    float minApproachSpeed = _arrivalTolerance / Time.fixedDeltaTime;
                    CurrentSpeed = Mathf.Max(
                        Mathf.MoveTowards(CurrentSpeed, 0f, Deceleration * Time.fixedDeltaTime),
                        minApproachSpeed * 0.5f
                    );
                    break;
            }

            float step = CurrentSpeed * Time.fixedDeltaTime;
            transform.position += moveDirection * Mathf.Min(step, distance);
        }

        private void Arrive()
        {
            CurrentSpeed = 0f;
            transform.position = _stations[NextStationIndex].StopPosition;
            CurrentStationIndex = NextStationIndex;

            SetState(TrainState.StoppedAtStation);
            _stopTimer = _stationStopDuration;

            OnArrivedAtStation?.Invoke(CurrentStationIndex, _stations[CurrentStationIndex]);
            AdvanceTarget();
        }

        private void Depart()
        {
            SetState(TrainState.Accelerating);
            OnDepartedStation?.Invoke(CurrentStationIndex, _stations[CurrentStationIndex]);
        }

        private void AdvanceTarget()
        {
            int candidate = CurrentStationIndex + (int)Direction;

            if (candidate < 0 || candidate >= _stations.Length)
            {
                Direction = Direction == TrainDirection.Forward
                    ? TrainDirection.Backward
                    : TrainDirection.Forward;
                candidate = CurrentStationIndex + (int)Direction;
            }

            int prevNext = NextStationIndex;
            NextStationIndex = candidate;

            if (prevNext != NextStationIndex)
                OnNextStationChanged?.Invoke(NextStationIndex, _stations[NextStationIndex]);
        }

        private void SetState(TrainState newState)
        {
            if (CurrentState == newState) return;
            CurrentState = newState;
            OnStateChanged?.Invoke(newState);
        }

        private bool IsValidIndex(int index)
        {
            return _stations != null && index >= 0 && index < _stations.Length;
        }

        private void OnDrawGizmos()
        {
            if (_stations == null || _stations.Length == 0) return;

            Gizmos.color = new Color(0.3f, 1f, 0.5f, 0.6f);
            for (int i = 0; i < _stations.Length - 1; i++)
            {
                if (_stations[i] == null || _stations[i + 1] == null) continue;
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