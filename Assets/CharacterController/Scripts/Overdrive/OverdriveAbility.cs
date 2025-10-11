using Unity.VisualScripting;
using UnityEngine;

namespace Resonance.PlayerController
{
    public class OverdriveAbility : MonoBehaviour
    {
        #region Class Variables
        [Header("Overdrive Settings")]
        [SerializeField] private float overdriveDuration = 8f;
        [SerializeField] private float overdriveCooldown = 30f;
        [SerializeField] private float overdriveSpeedMultiplier = 2f;
        
        public bool IsInOverdrive { get; private set; } = false;
        public bool IsOnCooldown { get; private set; } = false;
        public bool IsReady => !IsInOverdrive && !IsOnCooldown;
        public OverdriveState CurrentState { get; private set; } = OverdriveState.Ready;
        
        public float DurationTimeRemaining { get; private set; } = 0f;
        public float CooldownTimeRemaining { get; private set; } = 0f;
        
        public float SpeedMultiplier => overdriveSpeedMultiplier;

        private PlayerState _playerState;
        #endregion

        #region Startup
        private void Awake()
        {
            _playerState = GetComponent<PlayerState>();
        }
        #endregion
        
        #region Update Logic
        private void Update()
        {
            UpdateOverdriveState();
        }

        private void UpdateOverdriveState()
        {
            switch (CurrentState)
            {
                case OverdriveState.Ready:
                    IsInOverdrive = false;
                    IsOnCooldown = false;
                    break;
                
                case OverdriveState.Active:
                    IsInOverdrive = true;
                    IsOnCooldown = false;
                    
                    DurationTimeRemaining -= Time.deltaTime;

                    if (DurationTimeRemaining <= 0f)
                    {
                        DeactivateOverdrive();
                    }
                    break;
                
                case OverdriveState.Cooldown:
                    IsInOverdrive = false;
                    IsOnCooldown = true;
                    
                    CooldownTimeRemaining -= Time.deltaTime;

                    if (CooldownTimeRemaining <= 0f)
                    {
                        SetState(OverdriveState.Ready);
                    }
                    break;
            }
        }
        #endregion
        
        #region Public Methods
        public bool TryActivateOverdrive()
        {
            if (CurrentState != OverdriveState.Ready)
            {
                Debug.Log("Overdrive not ready - currently in state: " +  CurrentState);
                return false;
            }

            ActivateOverdrive();
            return true;
        }
        #endregion
        
        #region Private Methods
        private void ActivateOverdrive()
        {
            SetState(OverdriveState.Active);
            DurationTimeRemaining = overdriveDuration;
            
            Debug.Log("Overdrive ACTIVATED!");
        }

        private void DeactivateOverdrive()
        {
            SetState(OverdriveState.Cooldown);
            CooldownTimeRemaining = overdriveCooldown;
            
            Debug.Log("Overdrive DEACTIVATED - Starting cooldown");
        }

        private void SetState(OverdriveState newState)
        {
            if (CurrentState == newState) return;

            CurrentState = newState;

            IsInOverdrive = (newState == OverdriveState.Active);
            IsOnCooldown = (newState == OverdriveState.Cooldown);
        }
        #endregion

        public enum OverdriveState
        {
            Ready = 0,
            Active = 1,
            Cooldown = 2
        }
    }
}

