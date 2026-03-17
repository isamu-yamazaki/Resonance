using TMPro;
using UnityEngine;

namespace Resonance.Train
{
    public class TrainStationNameDisplay : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private TrainController _trainController;
        [SerializeField] private TMP_Text _displayText;

        [Header("Formatting")]
        [SerializeField] private string _prefix = "NEXT: ";
        [SerializeField] private string _noStationText = "---";

        private void Awake()
        {
            if (_trainController == null)
                _trainController = GetComponentInParent<TrainController>();

            if (_displayText == null)
                _displayText = GetComponent<TMP_Text>();

            if (_trainController == null)
            {
                Debug.LogWarning("[TrainStationNameDisplay] No TrainController found.", this);
                enabled = false;
                return;
            }

            _trainController.OnNextStationChanged += OnNextStationChanged;
            _trainController.OnArrivedAtStation += OnArrivedAtStation;
        }

        private void Start()
        {
            RefreshDisplay();
        }

        private void OnDestroy()
        {
            if (_trainController == null) return;
            _trainController.OnNextStationChanged -= OnNextStationChanged;
            _trainController.OnArrivedAtStation -= OnArrivedAtStation;
        }

        private void OnNextStationChanged(int index, TrainStation station) => SetText(station.DisplayName);

        private void OnArrivedAtStation(int index, TrainStation station) => RefreshDisplay();

        private void RefreshDisplay()
        {
            string displayName = _trainController.NextStationDisplayName;
            SetText(string.IsNullOrEmpty(displayName) ? _noStationText : displayName);
        }

        private void SetText(string stationName)
        {
            if (_displayText == null) return;
            _displayText.SetText(_prefix + stationName);
        }
    }
}