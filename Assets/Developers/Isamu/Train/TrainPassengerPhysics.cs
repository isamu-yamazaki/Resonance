using PurrNet;
using PurrNet.Prediction;
using Resonance.Assemblies.Train;
using UnityEngine;

namespace Resonance.Train
{
    [RequireComponent(typeof(CharacterController))]
    public class TrainPassengerPhysics : PredictedIdentity<TrainPassengerPhysicsState>
    {
        [Header("Train Reference")]
        [SerializeField] private TrainController _trainController;

        [Header("Boarding Detection")]
        [SerializeField] private string _trainFloorTag = "Train";

        [Header("Inertia")]
        [SerializeField] private float _inertiaDecay = 4f;
        [SerializeField] private float _maxInertiaSpeed = 18f;

        private CharacterController _characterController;

        public Vector3 GetTickVelocityOffset() => currentState.TickOffset;
        public float GetKnockbackVertical() => currentState.KnockbackVertical;

        #region Lifecycle
        protected override void LateAwake()
        {
            if (_trainController == null)
                _trainController = FindFirstObjectByType<TrainController>();

            _characterController = GetComponent<CharacterController>();
        }
        #endregion

        #region Simulation
        [SimulationOnly]
        public void SimulateClearInertia()
        {
            currentState.InertiaVelocity = Vector3.zero;
            currentState.TickOffset = Vector3.zero;
            currentState.IsKnockedBack = false;
        }

        [SimulationOnly]
        public void SimulateApplyKnockback(Vector3 force)
        {
            currentState.KnockbackVelocity = new Vector3(force.x, 0f, force.z);
            currentState.KnockbackVertical = force.y;
            currentState.IsKnockedBack = true;
        }

        [SimulationOnly]
        public void SimulateClearKnockbackVertical()
        {
            currentState.KnockbackVertical = 0f;
        }

        protected override void Simulate(ref TrainPassengerPhysicsState state, float delta)
        {
            UpdateBoardingState(ref state);
            ComputeTickOffset(ref state, delta);
        }

        private void UpdateBoardingState(ref TrainPassengerPhysicsState state)
        {
            Vector3 feetPos = transform.position + _characterController.center - Vector3.up * (_characterController.height * 0.5f - _characterController.radius);
            Collider[] hits = Physics.OverlapSphere(feetPos, _characterController.radius + 0.05f);

            bool onTrain = false;
            foreach (var collider in hits)
            {
                if (collider.CompareTag(_trainFloorTag))
                {
                    onTrain = true;
                    break;
                }
            }

            if (state.WasOnTrainLastTick && !onTrain && _trainController != null)
            {
                Vector3 trainVelocity = _trainController.Velocity;
                trainVelocity.y = 0f;
                state.InertiaVelocity = Vector3.ClampMagnitude(trainVelocity, _maxInertiaSpeed);
            }

            state.IsOnTrain = onTrain;
            state.WasOnTrainLastTick = onTrain;
        }

        private void ComputeTickOffset(ref TrainPassengerPhysicsState state, float delta)
        {
            state.TickOffset = Vector3.zero;

            if (state.IsKnockedBack)
            {
                state.TickOffset = state.KnockbackVelocity;
                state.KnockbackVelocity = Vector3.MoveTowards(state.KnockbackVelocity, Vector3.zero, _inertiaDecay * delta);

                if (state.KnockbackVelocity.sqrMagnitude <= 0.001f)
                    state.IsKnockedBack = false;
            }
            else if (state.IsOnTrain && _trainController != null)
            {
                Vector3 trainVelocity = _trainController.Velocity;
                trainVelocity.y = 0f;
                state.TickOffset = trainVelocity;
            }
            else if (state.InertiaVelocity.sqrMagnitude > 0.001f)
            {
                state.TickOffset = state.InertiaVelocity;
                state.InertiaVelocity = Vector3.MoveTowards(state.InertiaVelocity, Vector3.zero, _inertiaDecay * delta);
            }
        }
        #endregion
    }

    public struct TrainPassengerPhysicsState : IPredictedData<TrainPassengerPhysicsState>
    {
        public void Dispose()
        {
        }

        public bool IsOnTrain;
        public bool WasOnTrainLastTick;
        public Vector3 InertiaVelocity;
        public Vector3 KnockbackVelocity;
        public float KnockbackVertical;
        public Vector3 TickOffset;
        public bool IsKnockedBack;
    }
}