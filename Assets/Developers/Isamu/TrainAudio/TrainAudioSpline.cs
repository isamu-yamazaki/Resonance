using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Splines;

namespace Resonance.Train
{
    [RequireComponent(typeof(SplineContainer))]
    public class TrainAudioSpline : MonoBehaviour
    {
        [Header("Evaluation Resolution")]
        [Tooltip("Number of segments used when searching for the nearest point. Higher = more accurate, more expensive.")]
        [SerializeField] private int _resolution = 64;

        private SplineContainer _splineContainer;

        private void Awake()
        {
            _splineContainer = GetComponent<SplineContainer>();
        }

        public Vector3 FindNearestPoint(Vector3 worldPosition)
        {
            SplineUtility.GetNearestPoint(
                _splineContainer.Spline,
                _splineContainer.transform.InverseTransformPoint(worldPosition),
                out float3 nearestLocal,
                out _,
                _resolution
            );

            return _splineContainer.transform.TransformPoint(nearestLocal);
        }

        private void OnDrawGizmos()
        {
            if (_splineContainer == null) return;

            Gizmos.color = new Color(1f, 0.5f, 0f, 0.6f);

            Spline spline = _splineContainer.Spline;
            int drawSteps = 64;

            Vector3 previous = _splineContainer.transform.TransformPoint(spline.EvaluatePosition(0f));
            for (int i = 1; i <= drawSteps; i++)
            {
                float t = (float)i / drawSteps;
                Vector3 current = _splineContainer.transform.TransformPoint(spline.EvaluatePosition(t));
                Gizmos.DrawLine(previous, current);
                previous = current;
            }
        }
    }
}