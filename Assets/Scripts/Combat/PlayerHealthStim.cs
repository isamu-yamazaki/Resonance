using Resonance.Helper;
using Resonance.Player;
using Resonance.PlayerController;
using UnityEngine;

namespace Resonance.Combat
{
    public class PlayerHealthStim : MonoBehaviour
    {
        #region Class Variables
        [Header("Health Stim Settings")]
        [SerializeField] private float stimCooldown = 15f;
        [SerializeField] private int maxCharges = 2;

        public ObservableValue<int> CurrentCharges { get; private set; }
        public ObservableValue<float> ChargeCooldownRemaining { get; private set; }
        public ObservableValue<float> ChargeCooldownFill { get; private set; }

        public int MaxCharges => maxCharges;
        public float CooldownDuration => stimCooldown;
        public bool HasCharges => currentCharges > 0;
        public bool IsRecharging => currentCharges < maxCharges;

        private int currentCharges;
        private float cooldownTimeRemaining = 0f;

        private PlayerStats playerStats;
        private PlayerActionsInput actions;
        #endregion

        #region Startup
        private void Awake()
        {
            CurrentCharges = new ObservableValue<int>(maxCharges);
            ChargeCooldownRemaining = new ObservableValue<float>(0f);
            ChargeCooldownFill = new ObservableValue<float>(0f);
        }

        private void Start()
        {
            actions = PlayerActionsInput.Instance;
            playerStats = GetComponent<PlayerStats>();

            currentCharges = maxCharges;
            CurrentCharges.Value = currentCharges;
        }
        #endregion

        #region Update Logic
        private void Update()
        {
            UpdateCooldown();
        }

        private void UpdateCooldown()
        {
            if (!IsRecharging)
            {
                return;
            }

            cooldownTimeRemaining -= Time.deltaTime;

            ChargeCooldownRemaining.Value = cooldownTimeRemaining;
            ChargeCooldownFill.Value = cooldownTimeRemaining / stimCooldown;

            if (cooldownTimeRemaining <= 0f)
            {
                RestoreCharge();
            }
        }
        #endregion

        #region Private Methods
        private void UseHealthStim()
        {
            currentCharges--;
            CurrentCharges.Value = currentCharges;

            float healAmount = playerStats.MaxHealth / 4f;
            playerStats.Heal(healAmount);

            Debug.Log($"[HealthStim] Stim used. Healed {healAmount} HP. Charges remaining: {currentCharges}/{maxCharges}.");

            if (IsRecharging && cooldownTimeRemaining <= 0f)
            {
                cooldownTimeRemaining = stimCooldown;
                Debug.Log($"[HealthStim] Cooldown started. Recharging in {stimCooldown}s.");
            }
        }

        private void RestoreCharge()
        {
            currentCharges++;
            CurrentCharges.Value = currentCharges;

            Debug.Log($"[HealthStim] Charge restored. Charges: {currentCharges}/{maxCharges}.");

            if (IsRecharging)
            {
                cooldownTimeRemaining = stimCooldown;
                Debug.Log("[HealthStim] Still missing a charge. Cooldown restarted.");
            }
            else
            {
                cooldownTimeRemaining = 0f;
                ChargeCooldownRemaining.Value = 0f;
                ChargeCooldownFill.Value = 0f;
                Debug.Log("[HealthStim] Fully recharged.");
            }
        }
        #endregion
        
        public void ActivateStim()
        {
            if (HasCharges)
            {
                UseHealthStim();
            }
        }
    }
}