using UnityEngine;

namespace Resonance.Train
{
    [RequireComponent(typeof(TrainAudioSpline))]
    public class TrainAudioEmitter : MonoBehaviour
    {
        [Header("Train Reference")]
        [SerializeField] private TrainController _trainController;

        [Header("Emitter")]
        [Tooltip("Child GameObject with AkGameObj attached. Created automatically if left empty.")]
        [SerializeField] private GameObject _emitterObject;

        [Header("Wwise Events")]
        [SerializeField] private AK.Wwise.Event _playTrainMovingEvent;
        [SerializeField] private AK.Wwise.Event _stopTrainMovingEvent;
        [SerializeField] private AK.Wwise.Event _playTrainDisembarkEvent;
        [SerializeField] private AK.Wwise.Event _playTrainArrivalEvent;

        [Header("Wwise RTPCs")]
        [SerializeField] private string _speedRtpc = "Train_Speed";

        [Header("Player Reference")]
        [Tooltip("Assign the local player transform. Falls back to Camera.main if empty.")]
        [SerializeField] private Transform _playerTransform;

        [Header("Distance Culling")]
        [Tooltip("Beyond this distance the emitter still tracks but RTPC updates pause.")]
        [SerializeField] private float _cullingDistance = 80f;

        private TrainAudioSpline _spline;
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
                _emitterObject = CreateEmitterObject();

            if (_playerTransform == null && Camera.main != null)
                _playerTransform = Camera.main.transform;
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
            if (_playerTransform == null) return;

            Vector3 nearestPoint = _spline.FindNearestPoint(_playerTransform.position);
            _emitterObject.transform.position = nearestPoint;

            float distance = Vector3.Distance(_playerTransform.position, nearestPoint);

            if (distance > _cullingDistance) return;

            AkSoundEngine.SetRTPCValue(_speedRtpc, _trainController.NormalizedSpeed, _emitterObject);
        }

        private void OnDestroy()
        {
            StopLoop();
        }

        private void OnDeparted(int index, TrainStation station)
        {
            if (_playTrainDisembarkEvent != null && _playTrainDisembarkEvent.IsValid())
                _playTrainDisembarkEvent.Post(_emitterObject);
        }

        private void OnArrived(int index, TrainStation station)
        {
            if (_playTrainArrivalEvent != null && _playTrainArrivalEvent.IsValid())
                _playTrainArrivalEvent.Post(_emitterObject);
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

            if (_stopTrainMovingEvent != null && _stopTrainMovingEvent.IsValid())
                _stopTrainMovingEvent.Post(_emitterObject);

            _isLoopPlaying = false;
        }

        private GameObject CreateEmitterObject()
        {
            GameObject emitter = new GameObject("TrainAudioEmitterPoint");
            emitter.transform.SetParent(transform);
            emitter.AddComponent<AkGameObj>();
            return emitter;
        }
    }
}