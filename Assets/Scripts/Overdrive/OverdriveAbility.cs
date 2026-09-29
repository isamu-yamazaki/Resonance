using System.Collections;
using PurrNet.Prediction;
using Resonance.Helper;
using UnityEngine;
using Resonance.Player;

namespace Resonance.PlayerController
{
    public class OverdriveAbility : PredictedIdentity<OverdriveAbilityInput, OverdriveAbilityState>
    {
        public OverdriveAbility(OverdriveAbilityState state)
        {
            _state = state;
        }

        #region Class Variables

        [Header("Audio")]
#if !UNITY_SERVER
        [SerializeField]
        private AK.Wwise.Event activateEvent;
#endif

        [Header("Overdrive Settings")] [SerializeField]
        private float overdriveDuration = 8f;

        [SerializeField] private float overdriveCooldown = 30f;
        [SerializeField] private float overdriveSpeedMultiplier = 2f;
        [SerializeField] private float overdriveHealAmount = 50f;
        [SerializeField] private float overdriveRegenAmount = 2f;
        [SerializeField] private float overdriveDamageReductionAmount = 0.25f;


        public bool IsInOverdrive => currentState.State == OverdriveState.Active;
        public bool IsOnCooldown => currentState.State == OverdriveState.Cooldown;
        public bool IsReady => !IsInOverdrive && !IsOnCooldown;
        public OverdriveState CurrentState => currentState.State;

        public float DurationTimeRemaining => currentState.DurationRemaining;
        public float CooldownTimeRemaining => currentState.CooldownRemaining;

        public float SpeedMultiplier => overdriveSpeedMultiplier;
        public float CooldownDuration => overdriveCooldown;

        private PlayerState _playerState;
        private PlayerStats _playerStats;
        private OverdriveWorldActivateBroadcast _audioBroadcast;
        private OverdriveAbilityState _state;

        private PlayerActionsInput _playerActionsInput;

        #endregion

        #region Lifecycle

        protected override void LateAwake()
        {
            // TODO: implement death and respawn polling
            _playerState = GetComponent<PlayerState>();
            _playerStats = GetComponent<PlayerStats>();

            // TODO: migrate audio broadcasts to this script
            _audioBroadcast = GetComponent<OverdriveWorldActivateBroadcast>();

            if (!isOwner) return;

            _playerActionsInput = PlayerActionsInput.Instance;

            OverdriveHUD hud = FindFirstObjectByType<OverdriveHUD>();
            if (hud == null) return;

            hud.SetOverdriveAbility(this);
#if UNITY_EDITOR
            Debug.Log("[OverdriveAbility] Registered with OverdriveHUD");
#endif
        }

        protected override OverdriveAbilityState GetInitialState()
        {
            return new OverdriveAbilityState()
            {
                State = OverdriveState.Ready,
                CooldownRemaining = 0f,
                DurationRemaining = 0f,
                CooldownFill = 0f,
            };
        }

        #endregion

        #region Input

        protected override void GetFinalInput(ref OverdriveAbilityInput input)
        {
            if (!isOwner) return;

            input.OverdriveKeyPressed = _playerActionsInput.OverdrivePressed;
        }

        #endregion

        #region Simulation

        protected override void Simulate(
            OverdriveAbilityInput input,
            ref OverdriveAbilityState state,
            float delta
        )
        {
            switch (state.State)
            {
                case OverdriveState.Ready:
                    if (input.OverdriveKeyPressed)
                    {
                        ActivateOverdrive(ref state);
                    }

                    break;

                case OverdriveState.Active:
                    state.DurationRemaining -= Time.deltaTime;
                    if (state.DurationRemaining <= 0f)
                    {
                        DeactivateOverdrive(ref state);
                    }

                    break;

                case OverdriveState.Cooldown:
                    state.CooldownRemaining -= Time.deltaTime;
                    state.CooldownFill = CooldownTimeRemaining / overdriveCooldown;

                    if (state.CooldownRemaining <= 0f)
                    {
                        state.State = OverdriveState.Ready;
                    }

                    break;
            }

            if (_playerState.IsDead())
            {
                HandlePlayerDeath(ref state);
            }
        }

        [SimulationOnly]
        private void ActivateOverdrive(ref OverdriveAbilityState state)
        {
            state.DurationRemaining = overdriveDuration;

            // TODO: post activate event? unless this is what posts it
            _audioBroadcast.RequestAudioBroadcastNextTick();

            if (_playerStats != null)
            {
                _playerStats.SimulateAddSpeedModifier(overdriveSpeedMultiplier);
                _playerStats.SimulateAddRegenModifier(overdriveRegenAmount);
                _playerStats.SimulateAddDamageReductionModifier(overdriveDamageReductionAmount);
                _playerStats.SimulateHeal(overdriveHealAmount);
#if UNITY_EDITOR
                Debug.Log($"Overdrive ACTIVATED! Healed {overdriveHealAmount} HP");
#endif
            }
            else
            {
#if UNITY_EDITOR
                Debug.Log("Overdrive ACTIVATED!");
#endif
            }
        }

        [SimulationOnly]
        private void DeactivateOverdrive(ref OverdriveAbilityState state)
        {
            state.State = OverdriveState.Cooldown;
            state.CooldownRemaining = overdriveCooldown;

            _playerStats.SimulateRemoveSpeedModifier(overdriveSpeedMultiplier);
            _playerStats.SimulateRemoveRegenModifier(overdriveRegenAmount);
            _playerStats.SimulateRemoveDamageReductionModifier(overdriveDamageReductionAmount);
#if UNITY_EDITOR
            Debug.Log("Overdrive DEACTIVATED - Starting cooldown");
#endif
        }

        [SimulationOnly]
        private void HandlePlayerDeath(ref OverdriveAbilityState state)
        {
            if (state.State != OverdriveState.Active) return;

            DeactivateOverdrive(ref state);
#if UNITY_EDITOR
            Debug.Log("[OverdriveAbility] Overdrive interrupted by death");
#endif
        }

        [SimulationOnly]
        private void HandlePlayerRespawn()
        {
#if UNITY_EDITOR
            Debug.Log("[OverdriveAbility] Component resumed after respawn");
#endif
        }

        #endregion

        #region View updates

        protected override void UpdateView(OverdriveAbilityState viewState, OverdriveAbilityState? verified)
        {

        }

        #endregion


#if !UNITY_SERVER
        // TODO: call this again from the UpdateView
        private IEnumerator LerpLowPassOut(float duration)
        {
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float value = Mathf.Lerp(70f, 0f, elapsed / duration);
                AkUnitySoundEngine.SetRTPCValue("Overdrive_LowPass", value);
                yield return null;
            }

            AkUnitySoundEngine.SetRTPCValue("Overdrive_LowPass", 0f);
        }
#endif
    }

    public struct OverdriveAbilityInput : IPredictedData
    {
        public void Dispose()
        {
        }

        public bool OverdriveKeyPressed;
    }

    public struct OverdriveAbilityState : IPredictedData<OverdriveAbilityState>
    {
        public OverdriveState State;
        public float CooldownRemaining;
        public float DurationRemaining;
        public float CooldownFill;

        public void Dispose()
        {
        }
    }

    public enum OverdriveState
    {
        Ready = 0,
        PendingWithDelay = 1,
        Active = 2,
        Cooldown = 3
    }
}