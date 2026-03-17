using UnityEngine;

namespace Resonance.Train
{
    public class TrainStation : MonoBehaviour
    {
        [Header("Station Info")]
        [SerializeField] private string _stationId = "station";
        [SerializeField] private string _displayName = "Station";

        [Header("Stop Point")]
        [SerializeField] private Transform _stopPoint;

        public string StationId => _stationId;
        public string DisplayName => _displayName;
        public Vector3 StopPosition => _stopPoint != null ? _stopPoint.position : transform.position;

        private void OnDrawGizmos()
        {
            Gizmos.color = new Color(0.2f, 0.8f, 1f, 0.9f);
            Gizmos.DrawSphere(StopPosition, 0.3f);

            Gizmos.color = Color.white;
#if UNITY_EDITOR
            UnityEditor.Handles.Label(StopPosition + Vector3.up * 0.6f, _displayName);
#endif
        }
    }
}
