using System;
using System.Collections;
using PurrNet.Prediction;
using Resonance.Combat;
using Resonance.Helper;
using UnityEngine;
using Resonance.Player;

namespace Resonance.PlayerController
{
    public class OverdriveAbility : PredictedIdentity<OverdriveAbilityInput, OverdriveAbilityState>
    {
        private const float EventInvokeThresholdForFloats = 0.01f;
        private const float AnimationDelaySeconds = 0.67f;



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
        private FPArmsAnimator _fpArmsAnimator;

        private PlayerActionsInput _playerActionsInput;
        private OverdriveAbilityState? _previousVerifiedViewState;

        // current, previous
        public event Action<OverdriveState, OverdriveState?> OnOverdriveStateChanged;
        public event Action<float> OnCooldownChanged;
        public event Action<float> OnDurationChanged;
        public event Action<float> OnCooldownFillChanged;

        #endregion

        #region Lifecycle

        protected override void LateAwake()
        {
            _playerState = GetComponent<PlayerState>();
            _playerStats = GetComponent<PlayerStats>();
            _fpArmsAnimator = GetComponent<FPArmsAnimator>();

            // TODO: migrate audio broadcasts to this script
            _audioBroadcast = GetComponent<OverdriveWorldActivateBroadcast>();

            if (!isOwner) return;

            _playerActionsInput = PlayerActionsInput.Instance;
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

        protected override void UpdateInput(ref OverdriveAbilityInput input)
        {
            if (!isOwner) return;
            input.OverdriveKeyPressed |= _playerActionsInput.OverdrivePressed;
        }

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
                        state.State = OverdriveState.PendingWithDelay;
                    }

                    break;

                case OverdriveState.Active:
                    state.DurationRemaining -= delta;
                    if (state.DurationRemaining <= 0f)
                    {
                        DeactivateOverdrive(ref state);
                    }

                    break;

                case OverdriveState.Cooldown:
                    state.CooldownRemaining -= delta;
                    state.CooldownFill = CooldownTimeRemaining / overdriveCooldown;

                    if (state.CooldownRemaining <= 0f)
                    {
                        state.State = OverdriveState.Ready;
                    }

                    break;
                case OverdriveState.PendingWithDelay:
                    state.PendingTime += delta;
                    if (state.PendingTime >= AnimationDelaySeconds)
                    {
                        ActivateOverdrive(ref state);
                        state.PendingTime = 0;
                    }

                    break;

                default:
                    throw new ArgumentOutOfRangeException();
            }

            if (_playerState.IsDead())
            {
                HandlePlayerDeath(ref state);
            }
        }

        [SimulationOnly]
        private void ActivateOverdrive(ref OverdriveAbilityState state)
        {
            state.State = OverdriveState.Active;
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

        protected override void UpdateView(OverdriveAbilityState interpolatedState, OverdriveAbilityState? verified)
        {
            if (!verified.HasValue) return;
            var v = verified.Value;

            if (v.State != _previousVerifiedViewState?.State)
            {
                OnOverdriveStateChanged?.Invoke(v.State, _previousVerifiedViewState?.State);
            }

            if (v.State == OverdriveState.PendingWithDelay && _previousVerifiedViewState?.State == OverdriveState.Ready)
            {
                _fpArmsAnimator.RequestOverdriveActivation();
            }

            if (v.State == OverdriveState.Cooldown && _previousVerifiedViewState?.State == OverdriveState.Active)
            {
                StartCoroutine(LerpLowPassOut(1f));
            }

            if (Mathf.Abs(v.CooldownRemaining - (_previousVerifiedViewState?.CooldownRemaining ?? 0f)) >
                EventInvokeThresholdForFloats)
            {
                OnCooldownChanged?.Invoke(v.CooldownRemaining);
            }

            if (Mathf.Abs(v.CooldownFill - (_previousVerifiedViewState?.CooldownFill ?? 0f)) >
                EventInvokeThresholdForFloats)
            {
                OnCooldownFillChanged?.Invoke(v.CooldownFill);
            }

            if (Mathf.Abs(v.DurationRemaining - (_previousVerifiedViewState?.DurationRemaining ?? 0f)) >
                EventInvokeThresholdForFloats)
            {
                OnDurationChanged?.Invoke(v.DurationRemaining);
            }

            _previousVerifiedViewState = v;
        }

        #endregion


        private IEnumerator LerpLowPassOut(float duration)
        {
#if !UNITY_SERVER
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float value = Mathf.Lerp(70f, 0f, elapsed / duration);
                AkUnitySoundEngine.SetRTPCValue("Overdrive_LowPass", value);
                yield return null;
            }

            AkUnitySoundEngine.SetRTPCValue("Overdrive_LowPass", 0f);
#endif
        }
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
        public float PendingTime;

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