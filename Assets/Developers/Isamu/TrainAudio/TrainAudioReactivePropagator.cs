using Resonance.Audio;
using UnityEngine;

namespace Resonance.Train
{
    public class TrainAudioReactivePropagator : MonoBehaviour
    {
        [Header("Train Reference")]
        [SerializeField] private TrainController _trainController;

        [Header("Propagation Settings")]
        [Tooltip("How long each registered sound pulse lasts before expiring.")]
        [SerializeField] private float _pulseDuration = 0.5f;

        [Tooltip("How often a pulse is registered per second.")]
        [Range(1f, 30f)]
        [SerializeField] private float _pulseRate = 10f;

        private float _lastPulseTime = 0f;

        private void Awake()
        {
            if (_trainController == null)
                _trainController = GetComponentInParent<TrainController>();

            if (_trainController == null)
            {
                Debug.LogError("[TrainAudioReactivePropagator] No TrainController found.", this);
                enabled = false;
            }
        }

        private void Update()
        {
            if (_trainController.NormalizedSpeed <= 0f) return;
            if (AudioSourceTracker.Instance == null) return;

            if (Time.time - _lastPulseTime < 1f / _pulseRate) return;

            AudioSourceTracker.Instance.RegisterSound(transform.position, _trainController.NormalizedSpeed, _pulseDuration);

            _lastPulseTime = Time.time;
        }
    }
}
