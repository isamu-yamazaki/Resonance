using System;
using Resonance.PlayerController;
using Resonance.UI;
using Resonance.Match;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using PurrNet;
using Resonance.Combat;
using Resonance.Helper;
using UnityEngine.Serialization;

namespace Resonance.Player
{
    public class PlayerStats : NetworkBehaviour, IDamageable, IDamageNumberTarget
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
        public SyncVar<float> CurrentHealth = new SyncVar<float>();
        public float MaxHealth => maxHealth;

        public float BaseHealthRegen { get => baseHealthRegen; set => baseHealthRegen = value; }
        public float CurrentHealthRegen => currentHealthRegen;

        //Damage Reduction
        public float DamageReduction { get => currentDamageReduction; }
        public float BaseDamageReduction { get => baseDamageReduction; set => baseDamageReduction = Mathf.Clamp(value, 0f, maxDamageReduction); }

        //Speed
        public float PlayerSpeed => (currentSpeed);
        public float BaseSpeed { get => playerBaseSpeed; set => playerBaseSpeed = value; }
        public bool IsDead { get; private set; }

        public IReadOnlyList<float> DamageReductionModifiers => damageReductionModifiers;
        public IReadOnlyList<float> SpeedModifiers => speedModifiers;
        public IReadOnlyList<float> RegenModifiers => regenModifiers;


        #endregion

        #region Events
        public event System.Action OnPlayerDeath;
        public event System.Action OnPlayerRespawn;
        #endregion

        #region Component References
        private PlayerState _playerState;
        private CharacterController _characterController;
        private PlayerController.PlayerController _playerController;
        private Animator _animator;
        #endregion

        #region Damage Tracking
        private GameObject lastAttacker;
        private float lastDamageTime;
        private float lastHealth;
        #endregion

        #region Startup

        protected override void OnSpawned()
        {
            base.OnSpawned();
            if (isServer)
            {
                CurrentHealth.value = maxHealth;
            }

            lastHealth = CurrentHealth.value;

            if (isOwner)
            {
                healthBar = FindObjectOfType<HealthBar>();
                Debug.Log("HealthBar Found: " + (healthBar != null));

                if (healthBar != null)
                {
                    playerViewModel = gameObject.GetComponent<PlayerViewModel>();
                    if (playerViewModel == null)
                    {
                        playerViewModel = gameObject.AddComponent<PlayerViewModel>();
                    }

                    playerViewModel.InitializeHealth(maxHealth);

                    healthBar.Bind(playerViewModel);
                }
            }

            // Register with match stat tracker
            var matchStats = MatchStatBridge.GetTemporaryReference();
            if (matchStats != null)
            {
                matchStats.RegisterPlayer(gameObject);
            }

            //Stats
            CalculateSpeed();
            CalculateDamageReduction();
            CalculateRegen();
        }

        public void Awake()
        {
            _playerState = GetComponent<PlayerState>();
            _characterController = GetComponent<CharacterController>();
            _playerController = GetComponent<PlayerController.PlayerController>();
            GiveOwnership(_playerController.owner);

            _animator = GetComponent<Animator>();
        }

        // private void Start()
        // {
        // }

        private void OnDestroy()
        {
            // Unregister from match stat tracker
            var matchStats = MatchStatBridge.GetTemporaryReference();
            if (matchStats != null)
            {
                matchStats.UnregisterPlayer(gameObject);
            }
        }
        #endregion

        #region Health Management

        private List<float> regenModifiers = new List<float>();
        private float currentHealthRegen;

        private void Update()
        {
            // Regen only on server
            if (isServer && currentHealthRegen > 0)
            {
                Heal(currentHealthRegen * Time.deltaTime);
            }

            if (!isOwner) return;

            if (Mathf.Abs(CurrentHealth.value - lastHealth) > 0.01f)
            {
                if (playerViewModel != null)
                {
                    playerViewModel.Health.Value = CurrentHealth.value;
                }

                lastHealth = CurrentHealth.value;
            }
        }
        public void TakeDamage(float amount)
        {
            TakeDamage(amount, null);
        }

        [ServerRpc(requireOwnership: false)]
        public void TakeDamage(float amount, GameObject attacker)
        {
            if (IsDead) return;

            var matchStats = MatchStatBridge.GetTemporaryReference();
            if (attacker != null && attacker != gameObject && matchStats != null)
            {
                matchStats.RecordDamage(attacker, gameObject, amount);
                lastAttacker = attacker;
                lastDamageTime = Time.time;
            }

            float finalDamageAmount = amount * (1f - currentDamageReduction);
            UpdatePlayerHealthRelativeToCurrentHealth(-finalDamageAmount);

            if (attacker != null && owner.HasValue)
                ShowDamageIndicatorRpc(owner.Value, attacker.transform.position);

            if (CurrentHealth.value <= 0)
                Die(attacker);
        }

        private void UpdatePlayerHealthRelativeToCurrentHealth(float amountToChange)
        {
            if (amountToChange < 0)
            {
                UpdatePlayerHealth(Mathf.Max(0, CurrentHealth.value + amountToChange));
            }
            else
            {
                UpdatePlayerHealth(Mathf.Min(CurrentHealth.value + amountToChange, maxHealth));
            }
        }

        private void UpdatePlayerHealth(float healthToSetTo)
        {
            CurrentHealth.value = healthToSetTo;
            PropagateHealthToObservers();
        }

        [ServerRpc(requireOwnership: true)]
        public void Heal(float amount)
        {
            if (IsDead) return;
            UpdatePlayerHealthRelativeToCurrentHealth(amount);
        }

        [ObserversRpc]
        private void PropagateHealthToObservers()
        {
            if (playerViewModel != null)
                playerViewModel.Health.Value = CurrentHealth.value;
        }

        [TargetRpc]
        private void ShowDamageIndicatorRpc(PlayerID target, Vector3 attackerPosition)
        {
            Resonance.UI.DamageIndicatorUI.Instance?.ShowIndicator(attackerPosition);
        }

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
            currentHealthRegen = baseHealthRegen + regenModifiers.Sum();
        }
        #endregion

        #region Death & Respawn
        private void Die(GameObject killer = null)
        {
            if (IsDead) return;

            IsDead = true;

#if UNITY_EDITOR
            Debug.Log($"[PlayerStats] {owner} died!");
#endif

            // Record kill/death in match stats (server-only, runs once here)
            var matchStats = MatchStatBridge.GetTemporaryReference();
            if (matchStats != null)
            {
                if (killer != null && killer != gameObject)
                {
                    matchStats.RecordKill(killer, gameObject);
                }
                else
                {
                    // Suicide or environmental death
                    matchStats.RecordDeath(gameObject);
                }
            }

            ApplyDeathEffectsRpc();
            if (respawnOnDeath)
            {
                StartRespawnCoroutineOnServer();
            }
        }

        [ObserversRpc]
        private void ApplyDeathEffectsRpc()
        {
#if UNITY_EDITOR
            Debug.Log($"[PlayerStats] ApplyDeathEffectsRpc - isOwner: {isOwner}");
#endif
            if (_playerController != null)
            {
                _playerController.IsPlayerDead = true;
            }

            if (_playerState != null)
            {
                _playerState.SetPlayerMovementState(PlayerMovementState.Dead);
                _playerState.SetWeaponState(WeaponState.Idle);
            }
            
            GetComponent<PlayerShooter>().CancelReloadAndRefill();

            if (_playerController != null)
            {
                _playerController.enabled = false;
            }

            if (_characterController != null)
            {
                _characterController.enabled = false;
            }

            if (_animator != null)
            {
                _animator.enabled = false;
            }
            
            GetComponent<PlayerActionsInput>()?.ResetAllInputs();

            OnPlayerDeath?.Invoke();
        }

        [ServerRpc]
        private void StartRespawnCoroutineOnServer()
        {
            StartCoroutine(RespawnCoroutine());
        }

        private IEnumerator RespawnCoroutine()
        {
            if (!isServer) yield break;

            float respawnDelay = Respawn.Instance != null ?
                                 Respawn.Instance.RespawnDelay : 3f;

#if UNITY_EDITOR
            Debug.Log($"[PlayerStats] {owner} respawning in {respawnDelay}s");
#endif
            yield return new WaitForSeconds(respawnDelay);

            ApplyRespawnEffectsRpc();
        }

        [ObserversRpc]
        private void ApplyRespawnEffectsRpc()
        {
            GetComponent<PlayerActionsInput>()?.ResetAllInputs();
            IsDead = false;

            // Clear damage tracking
            lastAttacker = null;

            if (_playerState != null)
            {
                _playerState.SetPlayerMovementState(PlayerMovementState.Idling);
                _playerState.SetWeaponState(WeaponState.Idle);
            }

            if (_playerController != null)
            {
                _playerController.ResetState();
            }

            Transform spawnPoint = Resonance.Player.Respawn.Instance?.GetSpawnPoint();

            if (spawnPoint != null && _characterController != null)
            {
                transform.position = spawnPoint.position;
                transform.rotation = spawnPoint.rotation;
            }

            if (_characterController != null)
            {
                if (_characterController.stepOffset <= 0)
                {
                    _characterController.stepOffset = 0.3f;
                }

                _characterController.enabled = true;
            }

            if (_playerController != null)
            {
                _playerController.enabled = isOwner;
            }

            if (_animator != null)
            {
                _animator.enabled = true;
            }

            if (isServer)
            {
                CurrentHealth.value = maxHealth;
            }

            StartCoroutine(FinishRespawn());
        }

        private IEnumerator FinishRespawn()
        {
            yield return null;

            if (_playerController != null)
            {
                _playerController.IsPlayerDead = false;
            }

#if UNITY_EDITOR
            Debug.Log($"[PlayerStats] {owner} respawned!");
#endif

            OnPlayerRespawn?.Invoke();
        }
        #endregion

        #region Speed Management
        //Speed Properties
        private List<float> speedModifiers = new List<float>();
        private float currentSpeed;

        public void AddSpeedModifier(float modifier)
        {
            speedModifiers.Add(modifier);
            CalculateSpeed();
        }

        public void RemoveSpeedModifier(float modifier)
        {
            speedModifiers.Remove(modifier);
            CalculateSpeed();
        }

        private void CalculateSpeed()
        {
            currentSpeed = (playerBaseSpeed * speedModifiers.Aggregate(1f, (combinedModifier, nextModifier) => combinedModifier * nextModifier));
        }

        #endregion

        #region Damage Reduction Management

        private List<float> damageReductionModifiers = new List<float>();
        private float currentDamageReduction;

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
            float damageReduction = damageReductionModifiers.Aggregate(baseDamageReduction, (combined, next) => combined + next);
            currentDamageReduction = Mathf.Clamp(damageReduction, 0f, maxDamageReduction);
        }

        #endregion
    }
}
