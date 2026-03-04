using Resonance.Helper;
using UnityEngine;
using Resonance.Player;

namespace Resonance.PlayerController
{
    public class OverdriveAbility : MonoBehaviour
    {
        #region Class Variables
        [Header("Overdrive Settings")]
        [SerializeField] private float overdriveDuration = 8f;
        [SerializeField] private float overdriveCooldown = 30f;
        [SerializeField] private float overdriveSpeedMultiplier = 2f;
        [SerializeField] private float overdriveHealAmount = 50f;
        [SerializeField] private float overdriveRegenAmount = 5f;
        
        
        public ObservableValue<OverdriveState> State { get; private set; }
        public ObservableValue<float> CooldownRemaining { get; private set; }
        public ObservableValue<float> DurationRemaining { get; private set; }
        public ObservableValue<float> CooldownFill { get; private set; }
        
        public bool IsInOverdrive { get; private set; } = false;
        public bool IsOnCooldown { get; private set; } = false;
        public bool IsReady => !IsInOverdrive && !IsOnCooldown;
        public OverdriveState CurrentState { get; private set; } = OverdriveState.Ready;
        
        public float DurationTimeRemaining { get; private set; } = 0f;
        public float CooldownTimeRemaining { get; private set; } = 0f;
        
        public float SpeedMultiplier => overdriveSpeedMultiplier;
        public float CooldownDuration => overdriveCooldown;

        private PlayerState _playerState;
        private PlayerStats _playerStats;
        #endregion

        #region Startup
        private void Awake()
        {
            _playerState = GetComponent<PlayerState>();
            _playerStats = GetComponent<PlayerStats>();
            
            State = new ObservableValue<OverdriveState>(OverdriveState.Ready);
            CooldownRemaining = new ObservableValue<float>(0f);
            DurationRemaining = new ObservableValue<float>(0f);
            CooldownFill = new ObservableValue<float>(0f);
        }
        
        private void Start()
        {
            if (_playerStats != null)
            {
                _playerStats.OnPlayerDeath += HandlePlayerDeath;
                _playerStats.OnPlayerRespawn += HandlePlayerRespawn;
            }
            
            OverdriveHUD hud = FindObjectOfType<OverdriveHUD>();
            if (hud != null)
            {
                hud.SetOverdriveAbility(this);
                Debug.Log("[OverdriveAbility] Registered with OverdriveHUD");
            }
        }
        
        private void OnDestroy()
        {
            if (_playerStats != null)
            {
                _playerStats.OnPlayerDeath -= HandlePlayerDeath;
                _playerStats.OnPlayerRespawn -= HandlePlayerRespawn;
            }
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
                    
                    DurationRemaining.Value = DurationTimeRemaining;

                    if (DurationTimeRemaining <= 0f)
                    {
                        DeactivateOverdrive();
                    }
                    break;
                
                case OverdriveState.Cooldown:
                    IsInOverdrive = false;
                    IsOnCooldown = true;
                    
                    CooldownTimeRemaining -= Time.deltaTime;
                    
                    CooldownRemaining.Value = CooldownTimeRemaining;
                    CooldownFill.Value = CooldownTimeRemaining / overdriveCooldown;

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
            
            if (_playerStats != null)
            {
                
                _playerStats.AddSpeedModifier(overdriveSpeedMultiplier);
                _playerStats.AddRegenModifier(overdriveRegenAmount);
                _playerStats.Heal(overdriveHealAmount);
                Debug.Log($"Overdrive ACTIVATED! Healed {overdriveHealAmount} HP");
            }
            else
            {
                Debug.Log("Overdrive ACTIVATED!");
            }
        }

        private void DeactivateOverdrive()
        {
            SetState(OverdriveState.Cooldown);
            CooldownTimeRemaining = overdriveCooldown;
            
            _playerStats.RemoveSpeedModifier(overdriveSpeedMultiplier);
            _playerStats.RemoveRegenModifier(overdriveRegenAmount);
            Debug.Log("Overdrive DEACTIVATED - Starting cooldown");
        }

        private void SetState(OverdriveState newState)
        {
            if (CurrentState == newState) return;

            CurrentState = newState;
            State.Value = newState;

            IsInOverdrive = (newState == OverdriveState.Active);
            IsOnCooldown = (newState == OverdriveState.Cooldown);
        }
        
        private void HandlePlayerDeath()
        {
            if (CurrentState == OverdriveState.Active)
            {
                DeactivateOverdrive();
                Debug.Log("[OverdriveAbility] Overdrive interrupted by death");
            }
            
            enabled = false;
        }
        
        private void HandlePlayerRespawn()
        {
            enabled = true;
            Debug.Log("[OverdriveAbility] Component resumed after respawn");
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