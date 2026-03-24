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

        public Vector3 FindNearestLocalPoint(Vector3 worldPosition)
        {
            float3 localPosition = _splineContainer.transform.InverseTransformPoint(worldPosition);
            float3 bestPoint = float3.zero;
            float bestDistance = float.MaxValue;

            foreach (Spline spline in _splineContainer.Splines)
            {
                SplineUtility.GetNearestPoint(spline, localPosition, out float3 nearest, out float distance, _resolution);

                if (distance < bestDistance)
                {
                    bestDistance = distance;
                    bestPoint = nearest;
                }
            }

            return bestPoint;
        }

        private void OnDrawGizmos()
        {
            SplineContainer container = _splineContainer != null
                ? _splineContainer
                : GetComponent<SplineContainer>();

            if (container == null) return;

            Gizmos.color = new Color(1f, 0.5f, 0f, 0.6f);

            foreach (Spline spline in container.Splines)
            {
                int drawSteps = 64;
                Vector3 previous = container.transform.TransformPoint(spline.EvaluatePosition(0f));

                for (int i = 1; i <= drawSteps; i++)
                {
                    float t = (float)i / drawSteps;
                    Vector3 current = container.transform.TransformPoint(spline.EvaluatePosition(t));
                    Gizmos.DrawLine(previous, current);
                    previous = current;
                }
            }
        }
    }
}
