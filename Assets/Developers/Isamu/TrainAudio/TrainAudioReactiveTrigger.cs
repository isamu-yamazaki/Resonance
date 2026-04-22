using Resonance.Assemblies.Train;
using Resonance.Audio;
using System.Collections.Generic;
using UnityEngine;

namespace Resonance.Train
{
    public class TrainAudioReactiveTrigger : MonoBehaviour
    {
        [Header("Train Reference")]
        [SerializeField] private TrainController _trainController;

        [Header("Trigger Settings")]
        [Tooltip("Base half-extents of the trigger box in local space.")]
        [SerializeField] private Vector3 _baseHalfExtents = new Vector3(5f, 3f, 15f);

        [Tooltip("How much to expand the half-extents at full speed.")]
        [SerializeField] private Vector3 _expansionAtFullSpeed = new Vector3(5f, 2f, 5f);

        [Tooltip("Layer mask for AudioReactiveObject colliders.")]
        [SerializeField] private LayerMask _reactiveLayer;

        private readonly HashSet<AudioReactiveObject> _previousOverlapping = new HashSet<AudioReactiveObject>();

        private void Awake()
        {
            if (_trainController == null)
                _trainController = GetComponentInParent<TrainController>();

            if (_trainController == null)
            {
                Debug.LogError("[TrainAudioReactiveTrigger] No TrainController found.", this);
                enabled = false;
            }
        }

        private void Update()
        {
            float normalizedSpeed = _trainController.NormalizedSpeed;
            Vector3 halfExtents = _baseHalfExtents + _expansionAtFullSpeed * normalizedSpeed;

            Collider[] hits = Physics.OverlapBox(
                transform.position,
                halfExtents,
                transform.rotation,
                _reactiveLayer
            );

            HashSet<AudioReactiveObject> currentOverlapping = new HashSet<AudioReactiveObject>();

            foreach (Collider hit in hits)
            {
                AudioReactiveObject reactive = hit.GetComponentInParent<AudioReactiveObject>();
                if (reactive == null) continue;

                currentOverlapping.Add(reactive);
                reactive.SetExternalIntensity(normalizedSpeed);
            }

            foreach (AudioReactiveObject reactive in _previousOverlapping)
            {
                if (!currentOverlapping.Contains(reactive))
                    reactive.SetExternalIntensity(0f);
            }

            _previousOverlapping.Clear();
            foreach (AudioReactiveObject reactive in currentOverlapping)
                _previousOverlapping.Add(reactive);
        }

        private void OnDrawGizmos()
        {
            float normalizedSpeed = Application.isPlaying && _trainController != null
                ? _trainController.NormalizedSpeed
                : 0f;

            Vector3 halfExtents = _baseHalfExtents + _expansionAtFullSpeed * normalizedSpeed;

            Gizmos.color = new Color(0.5f, 0f, 1f, 0.3f);
            Gizmos.matrix = Matrix4x4.TRS(transform.position, transform.rotation, Vector3.one);
            Gizmos.DrawCube(Vector3.zero, halfExtents * 2f);

            Gizmos.color = new Color(0.5f, 0f, 1f, 0.6f);
            Gizmos.DrawWireCube(Vector3.zero, halfExtents * 2f);
        }
    }
}
