using UnityEngine;

namespace Resonance.Train
{
    public class TrainDirectionalCollider : MonoBehaviour
    {
        [Header("Train Reference")]
        [SerializeField] private TrainController _trainController;

        [Header("Side Collider Objects")]
        [SerializeField] private GameObject _forwardSideCollider;
        [SerializeField] private GameObject _backwardSideCollider;

        private TrainDirection _lastDirection;

        private void Awake()
        {
            if (_trainController == null)
                _trainController = GetComponentInParent<TrainController>();

            if (_trainController == null)
            {
                Debug.LogError("[TrainDirectionalCollider] No TrainController found.", this);
                enabled = false;
                return;
            }

            _trainController.OnStateChanged += state => Refresh();
            _trainController.OnNextStationChanged += (index, station) => Refresh();
        }

        private void Start()
        {
            Refresh();
            _lastDirection = _trainController.Direction;
        }

        private void Update()
        {
            if (_trainController.Direction != _lastDirection)
            {
                _lastDirection = _trainController.Direction;
                Refresh();
            }
        }

        private void Refresh()
        {
            if (_trainController == null) return;

            bool movingForward = _trainController.Direction == TrainDirection.Forward
                                 && _trainController.CurrentState != TrainState.StoppedAtStation;

            SetActive(_forwardSideCollider, movingForward);
            SetActive(_backwardSideCollider, !movingForward);
        }

        private static void SetActive(GameObject obj, bool active)
        {
            if (obj != null && obj.activeSelf != active)
                obj.SetActive(active);
        }
    }
}