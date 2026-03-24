using UnityEngine;

namespace Resonance.Train
{
    [RequireComponent(typeof(TrainAudioSpline))]
    public class TrainAudioEmitter : MonoBehaviour
    {
        [Header("Train Reference")]
        [SerializeField] private TrainController _trainController;

        [Header("Emitter")]
        [SerializeField] private GameObject _emitterObject;

        [Header("Wwise Events")]
        [SerializeField] private AK.Wwise.Event _playTrainMovingEvent;
        [SerializeField] private AK.Wwise.Event _stopTrainMovingEvent;
        [SerializeField] private AK.Wwise.Event _playTrainDisembarkEvent;
        [SerializeField] private AK.Wwise.Event _playTrainArrivalEvent;

        [Header("Wwise RTPCs")]
        [SerializeField] private string _speedRtpc = "Train_Speed";

        [Header("Distance Culling")]
        [Tooltip("Beyond this distance the emitter still tracks but RTPC updates pause.")]
        [SerializeField] private float _cullingDistance = 80f;

        [Header("Gizmos")]
        [SerializeField] private float _gizmoRadius = 100f;

        private TrainAudioSpline _spline;
        private Transform _playerTransform;
        private bool _isLoopPlaying = false;

        private void Awake()
        {
            _spline = GetComponent<TrainAudioSpline>();

            if (_trainController == null)
                _trainController = GetComponentInParent<TrainController>();

            if (_trainController == null)
            {
                Debug.LogError("[TrainAudioEmitter] No TrainController found.", this);
                enabled = false;
                return;
            }

            if (_emitterObject == null)
            {
                Debug.LogError("[TrainAudioEmitter] Emitter Object not assigned.", this);
                enabled = false;
                return;
            }
        }

        private void OnEnable()
        {
            _trainController.OnDepartedStation += OnDeparted;
            _trainController.OnArrivedAtStation += OnArrived;
        }

        private void OnDisable()
        {
            _trainController.OnDepartedStation -= OnDeparted;
            _trainController.OnArrivedAtStation -= OnArrived;
        }

        private void Start()
        {
            StartLoop();
        }

        private void Update()
        {
            if (Resonance.PlayerController.PlayerController.LocalPlayer != null)
                _playerTransform = Resonance.PlayerController.PlayerController.LocalPlayer.transform;

            if (_playerTransform == null) return;

            _emitterObject.transform.localPosition = _spline.FindNearestLocalPoint(_playerTransform.position);

            float distance = Vector3.Distance(_playerTransform.position, _emitterObject.transform.position);

            if (distance > _cullingDistance) return;

            AkUnitySoundEngine.SetRTPCValue(_speedRtpc, _trainController.NormalizedSpeed, _emitterObject);
        }

        private void OnDestroy()
        {
            StopLoop();
        }

        private void OnDeparted(int index, TrainStation station)
        {
            _playTrainDisembarkEvent?.Post(_emitterObject);
        }

        private void OnArrived(int index, TrainStation station)
        {
            _playTrainArrivalEvent?.Post(_emitterObject);
        }

        private void StartLoop()
        {
            if (_isLoopPlaying) return;

            if (_playTrainMovingEvent == null || !_playTrainMovingEvent.IsValid())
            {
                Debug.LogWarning("[TrainAudioEmitter] Play Train Moving event not assigned or invalid.", this);
                return;
            }

            _playTrainMovingEvent.Post(_emitterObject);
            _isLoopPlaying = true;
        }

        private void StopLoop()
        {
            if (!_isLoopPlaying) return;
            _stopTrainMovingEvent?.Post(_emitterObject);
            _isLoopPlaying = false;
        }

        private void OnDrawGizmos()
        {
            if (_emitterObject == null) return;

            Gizmos.color = new Color(1f, 0.5f, 0f, 0.8f);
            Gizmos.DrawWireSphere(_emitterObject.transform.position, _gizmoRadius);
        }
    }
}
