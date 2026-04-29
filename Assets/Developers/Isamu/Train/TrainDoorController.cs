using Resonance.Assemblies.Train;
using UnityEngine;

namespace Resonance.Train
{
    public class TrainDoorController : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private Animator[] _doorAnimators = { };
        [SerializeField] private TrainController _trainController;

        [Header("Animation Parameters")]
        [SerializeField] private string _openTriggerName = "OpenDoors";
        [SerializeField] private string _closeTriggerName = "CloseDoors";

        private int _openTriggerHash;
        private int _closeTriggerHash;

        private void Awake()
        {
            _openTriggerHash = Animator.StringToHash(_openTriggerName);
            _closeTriggerHash = Animator.StringToHash(_closeTriggerName);

            if (_doorAnimators.Length == 0)
                Debug.LogError("[TrainDoorController] No Animator assigned.", this);

            if (_trainController == null)
                Debug.LogError("[TrainDoorController] No TrainController assigned.", this);
        }

        private void OnEnable()
        {
            if (_trainController == null) return;
            _trainController.OnArrivedAtStation += HandleArrived;
            _trainController.OnPreDepart += HandlePreDepart;
            _trainController.OnFirstVerifiedViewStateIsAlreadyMoving += HandleFirstVerifiedViewStateIsAlreadyMoving;
        }

        private void OnDisable()
        {
            if (_trainController == null) return;
            _trainController.OnArrivedAtStation -= HandleArrived;
            _trainController.OnPreDepart -= HandlePreDepart;
            _trainController.OnFirstVerifiedViewStateIsAlreadyMoving -= HandleFirstVerifiedViewStateIsAlreadyMoving;
        }

        private void HandleArrived(int stationIndex, TrainStation station)
        {
            foreach (var doorAnimator in _doorAnimators)
            {
                doorAnimator.ResetTrigger(_closeTriggerHash);
                doorAnimator.SetTrigger(_openTriggerHash);
            }
        }

        private void HandlePreDepart(int stationIndex, TrainStation station)
        {
            foreach (var doorAnimator in _doorAnimators)
            {
                doorAnimator.ResetTrigger(_openTriggerHash);
                doorAnimator.SetTrigger(_closeTriggerHash);
            }
        }

        private void HandleFirstVerifiedViewStateIsAlreadyMoving()
        {
            foreach (var doorAnimator in _doorAnimators)
            {
                doorAnimator.ResetTrigger(_openTriggerHash);
                doorAnimator.SetTrigger(_closeTriggerHash);
            }
        }
    }
}
