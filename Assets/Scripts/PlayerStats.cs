using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using PurrNet.Pooling;
using PurrNet.Prediction;
using Resonance.Assemblies.Player;
using Resonance.Combat;
using Resonance.Helper;
using Resonance.Match;
using Resonance.PlayerController;
using Resonance.UI;
using UnityEngine;

namespace Resonance.Player
{
    [RequireComponent(typeof(PlayerPredictedController))]
    public class PlayerStats : PredictedIdentity<PlayerStatsInputData, PlayerStatsDataState>,
                               IDamageable, IDamageNumberTarget
    {
        #region Inspector Fields

        [SerializeField] private float maxHealth = 100f;
        [SerializeField] private float baseHealthRegen = 0f;
        [SerializeField] private float maxDamageReduction = 0.75f;
        [SerializeField] private float baseDamageReduction = 0f;
        [SerializeField] private float playerBaseSpeed = 1f;
        [SerializeField] private bool respawnOnDeath = true;

        private HealthBar healthBar;
        private PlayerViewModel playerViewModel;

        #endregion

        #region Properties

        public float MaxHealth => maxHealth;
        public float CurrentHealth => currentState.CurrentHealth;
        public float BaseHealthRegen { get => baseHealthRegen; set => baseHealthRegen = value; }
        public float CurrentHealthRegen => currentState.CurrentHealthRegen;
        public float DamageReduction => currentState.CurrentDamageReduction;
        public float BaseDamageReduction { get => baseDamageReduction; set => baseDamageReduction = Mathf.Clamp(value, 0f, maxDamageReduction); }
        public float PlayerSpeed => currentState.CurrentSpeed;
        public float BaseSpeed { get => playerBaseSpeed; set => playerBaseSpeed = value; }
        public bool IsDead => currentState.IsDead;

        public IReadOnlyList<float> DamageReductionModifiers => damageReductionModifiers;
        public IReadOnlyList<float> SpeedModifiers => currentState.SpeedModifiers;
        public IReadOnlyList<float> RegenModifiers => regenModifiers;

        #endregion

        #region Events

        public event Action OnPlayerDeath;
        public event Action OnPlayerRespawn;

        #endregion

        #region Component References

        private PlayerState _playerState;
        private CharacterController _characterController;
        private PlayerPredictedController _playerController;
        private Animator _animator;

        #endregion

        #region Damage Tracking

        private GameObject lastAttacker;
        private float lastDamageTime;

        #endregion

        #region External input accumulators

        private float _pendingExternalDamage;
        private float _pendingExternalHeal;
        private Vector3 _pendingAttackerPos;


        #endregion

        #region External speed modifiers

        private float? _pendingSpeedModifierToAdd;
        private float? _pendingSpeedModifierToRemove;

        #endregion

        #region UpdateView tracking

        private bool _lastVerifiedIsDead;
        private float _lastVerifiedHealth;

        #endregion

        #region Lifecycle

        protected override void LateAwake()
        {
            _playerState = GetComponent<PlayerState>();
            _characterController = GetComponent<CharacterController>();
            _playerController = GetComponent<PlayerPredictedController>();
            _animator = GetComponent<Animator>();

            if (isOwner)
            {
                healthBar = FindFirstObjectByType<HealthBar>();

                if (healthBar != null)
                {
                    playerViewModel = GetComponent<PlayerViewModel>();
                    playerViewModel.InitializeHealth(maxHealth);
                    healthBar.Bind(playerViewModel);
                }

                var matchStats = MatchStatBridge.GetTemporaryReference();
                if (matchStats != null)
                    matchStats.RegisterPlayer(gameObject);
            }
        }

        private void OnDestroy()
        {
            var matchStats = MatchStatBridge.GetTemporaryReference();
            if (matchStats != null)
                matchStats.UnregisterPlayer(gameObject);
        }


        protected override PlayerStatsDataState GetInitialState()
        {
            return new PlayerStatsDataState
            {
                CurrentHealth = maxHealth,
                IsDead = false,
                CurrentSpeed = playerBaseSpeed,
                CurrentDamageReduction = baseDamageReduction,
                CurrentHealthRegen = baseHealthRegen,
                SpeedModifiers = new DisposableList<float>(),
            };
        }

        #endregion

        #region Input

        protected override void GetFinalInput(ref PlayerStatsInputData input)
        {
            input.ExternalHealAmount = _pendingExternalHeal;
            input.ExternalDamageAmount = _pendingExternalDamage;
            input.ExternalAttackerPosition = _pendingAttackerPos;
            input.ExternalSpeedModifierToAdd = _pendingSpeedModifierToAdd;
            input.ExternalSpeedModifierToRemove = _pendingSpeedModifierToRemove;

            _pendingExternalHeal = 0f;
            _pendingExternalDamage = 0f;
            _pendingSpeedModifierToAdd = null;
            _pendingSpeedModifierToRemove = null;
        }

        public void AddSpeedModifierExternal(float modifier)
        {
            _pendingSpeedModifierToAdd = modifier;
        }

        public void RemoveSpeedModifierExternal(float modifier)
        {
            _pendingSpeedModifierToRemove = modifier;
        }

        #endregion

        protected override void Simulate(PlayerStatsInputData input, ref PlayerStatsDataState state, float delta)
        {
            // Health regen
            if (!state.IsDead && state.CurrentHealthRegen > 0f)
                state.CurrentHealth = Mathf.Min(state.CurrentHealth + state.CurrentHealthRegen * delta, maxHealth);

            // External damage (owner-queued)
            if (input.ExternalDamageAmount > 0f && !state.IsDead)
            {
                float finalDamage = input.ExternalDamageAmount * (1f - state.CurrentDamageReduction);
                state.CurrentHealth = Mathf.Max(0f, state.CurrentHealth - finalDamage);
                state.LastDamageAttackerPos = input.ExternalAttackerPosition;
            }

            // External heal (owner-queued)
            if (input.ExternalHealAmount > 0f && !state.IsDead)
                state.CurrentHealth = Mathf.Min(state.CurrentHealth + input.ExternalHealAmount, maxHealth);

            // External modifiers (owner-queued)
            if (input.ExternalSpeedModifierToAdd.HasValue)
                SimulateAddSpeedModifier(ref state, input.ExternalSpeedModifierToAdd.Value);
            if (input.ExternalSpeedModifierToRemove.HasValue)
                SimulateRemoveSpeedModifier(ref state, input.ExternalSpeedModifierToRemove.Value);

            // Death check
            if (state.CurrentHealth <= 0f && !state.IsDead)
            {
                state.IsDead = true;

                if (respawnOnDeath)
                {
                    state.RespawnTimer = Respawn.Instance != null ? Respawn.Instance.RespawnDelay : 3f;
                    Transform sp = Respawn.Instance?.GetSpawnPoint();
                    state.SpawnPosition = sp != null ? sp.position : Vector3.zero;
                    state.SpawnRotation = sp != null ? sp.rotation : Quaternion.identity;
                }
            }

            // Respawn timer
            if (state.IsDead && state.RespawnTimer > 0f)
            {
                state.RespawnTimer -= delta;
                if (state.RespawnTimer <= 0f)
                {
                    state.CurrentHealth = maxHealth;
                    state.IsDead = false;

                    _playerController.SimulatePlaceAtRespawnPoint(
                        state.SpawnPosition,
                        state.SpawnRotation
                    );
                }
            }
        }

        [SimulationOnly]
        public void SimulateAddSpeedModifier(float modifier)
        {
            SimulateAddSpeedModifier(ref currentState, modifier);
        }

        [SimulationOnly]
        public void SimulateRemoveSpeedModifier(float modifier)
        {
            SimulateRemoveSpeedModifier(ref currentState, modifier);
        }

        private void SimulateAddSpeedModifier(ref PlayerStatsDataState state, float modifier)
        {
            Debug.Log($"[SimulateAddSpeedModifier] {modifier}");
            state.SpeedModifiers.Add(modifier);
            CalculateSpeed(ref state);
        }

        private void SimulateRemoveSpeedModifier(ref PlayerStatsDataState state, float modifier)
        {
            Debug.Log($"[SimulateRemoveSpeedModifier] {modifier}");
            state.SpeedModifiers.Remove(modifier);
            CalculateSpeed(ref state);
        }

        private void CalculateSpeed(ref PlayerStatsDataState state)
        {
            state.CurrentSpeed = playerBaseSpeed * state.SpeedModifiers.Aggregate(1f, (combined, next) => combined * next);
        }

        protected override PlayerStatsDataState Interpolate(PlayerStatsDataState from, PlayerStatsDataState to, float t)
        {
            return to;
        }

        #region Health Management

        [SimulationOnly]
        public void TakeDamage(float amount)
        {
            TakeDamage(amount, null);
        }

        [SimulationOnly]
        public void TakeDamage(float amount, GameObject attacker)
        {
            if (currentState.IsDead) return;

            if (isServer && attacker != null && attacker != gameObject)
            {
                var matchStats = MatchStatBridge.GetTemporaryReference();
                if (matchStats != null)
                {
                    matchStats.RecordDamage(attacker, gameObject, amount);
                    lastAttacker = attacker;
                    lastDamageTime = Time.time;
                }
            }

            float finalDamage = amount * (1f - currentState.CurrentDamageReduction);
            currentState.CurrentHealth = Mathf.Max(0f, currentState.CurrentHealth - finalDamage);

            if (attacker != null)
                currentState.LastDamageAttackerPos = attacker.transform.position;
        }

        #region Local view updates

        protected override void UpdateView(PlayerStatsDataState viewState, PlayerStatsDataState? verified)
        {
            // Health UI always reflects predicted state (owner only)
            if (isOwner && playerViewModel != null)
                playerViewModel.Health.Value = viewState.CurrentHealth;

            if (!verified.HasValue) return;
            var v = verified.Value;

            // Damage indicator: server confirmed health dropped
            if (isOwner && v.CurrentHealth < _lastVerifiedHealth)
                DamageIndicatorUI.Instance?.ShowIndicator(v.LastDamageAttackerPos);
            _lastVerifiedHealth = v.CurrentHealth;

            // Death effects: IsDead transition false → true
            if (v.IsDead && !_lastVerifiedIsDead)
            {
                if (_playerController != null)
                    _playerController.IsPlayerDead = true;

                _playerState?.SetExternalPlayerMovementState(PlayerMovementState.Dead);
                _playerState?.SetExternalWeaponState(WeaponState.Idle);
                GetComponent<PlayerShooter>()?.CancelReloadAndRefill();

                if (_playerController != null)
                    _playerController.enabled = false;
                if (_characterController != null)
                    _characterController.enabled = false;
                if (_animator != null)
                    _animator.enabled = false;

                if (isOwner)
                    PlayerActionsInput.Instance?.ResetAllInputs();

                OnPlayerDeath?.Invoke();
            }

            // Respawn effects: IsDead transition true → false
            if (!v.IsDead && _lastVerifiedIsDead)
            {
                if (isOwner)
                    PlayerActionsInput.Instance?.ResetAllInputs();

                lastAttacker = null;

                _playerState?.SetExternalPlayerMovementState(PlayerMovementState.Idling);
                _playerState?.SetExternalWeaponState(WeaponState.Idle);
                _playerController?.ResetState();

                if (_characterController != null)
                {
                    if (_characterController.stepOffset <= 0)
                        _characterController.stepOffset = 0.3f;

                    _characterController.enabled = true;
                }

                if (_playerController != null)
                    _playerController.enabled = isOwner;
                if (_animator != null)
                    _animator.enabled = true;

                StartCoroutine(FinishRespawn());
            }

            _lastVerifiedIsDead = v.IsDead;
        }

        #endregion


        public void TakeExternalDamage(float amount, GameObject attacker = null)
        {
            if (isServer && !isOwner)
            {
                if (currentState.IsDead) return;

                if (attacker != null && attacker != gameObject)
                {
                    var matchStats = MatchStatBridge.GetTemporaryReference();
                    if (matchStats != null)
                    {
                        matchStats.RecordDamage(attacker, gameObject, amount);
                        lastAttacker = attacker;
                        lastDamageTime = Time.time;
                    }
                }

                float finalDamage = amount * (1f - currentState.CurrentDamageReduction);
                currentState.CurrentHealth = Mathf.Max(0f, currentState.CurrentHealth - finalDamage);
                if (attacker != null)
                    currentState.LastDamageAttackerPos = attacker.transform.position;
            }
            else
            {
                _pendingExternalDamage += amount;
                if (attacker != null)
                    _pendingAttackerPos = attacker.transform.position;
            }
        }

        public void Heal(float amount)
        {
            if (currentState.IsDead) return;

            if (isServer && !isOwner)
                currentState.CurrentHealth = Mathf.Min(currentState.CurrentHealth + amount, maxHealth);
            else
                _pendingExternalHeal += amount;
        }

        private IEnumerator FinishRespawn()
        {
            yield return null;

            if (_playerController != null)
                _playerController.IsPlayerDead = false;

#if UNITY_EDITOR
            Debug.Log($"[PlayerStats] {owner} respawned!");
#endif
            OnPlayerRespawn?.Invoke();
        }

        #endregion

        #region Speed Management


        #endregion

        #region Damage Reduction Management

        private List<float> damageReductionModifiers = new List<float>();

        public void AddDamageReductionModifier(float modifier)
        {
#if UNITY_EDITOR
            Debug.Log($"[PlayerStats] AddDamageReductionModifier called with: {modifier}");
#endif
            damageReductionModifiers.Add(modifier);
            CalculateDamageReduction();
        }

        public void RemoveDamageReductionModifier(float modifier)
        {
            damageReductionModifiers.Remove(modifier);
            CalculateDamageReduction();
        }

        private void CalculateDamageReduction()
        {
            float reduction = damageReductionModifiers.Aggregate(baseDamageReduction, (combined, next) => combined + next);
            currentState.CurrentDamageReduction = Mathf.Clamp(reduction, 0f, maxDamageReduction);
        }

        #endregion

        #region Regen Management

        private List<float> regenModifiers = new List<float>();

        public void AddRegenModifier(float modifier)
        {
            regenModifiers.Add(modifier);
            CalculateRegen();
        }

        public void RemoveRegenModifier(float modifier)
        {
            regenModifiers.Remove(modifier);
            CalculateRegen();
        }

        private void CalculateRegen()
        {
            currentState.CurrentHealthRegen = baseHealthRegen + regenModifiers.Sum();
        }

        #endregion
    }
}
