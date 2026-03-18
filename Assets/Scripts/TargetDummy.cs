using Resonance.Helper;
using UnityEngine;
using Resonance.Match;
using Resonance.Assemblies.MatchStat;

namespace Resonance.Entities
{
    public class TargetDummy : MonoBehaviour, IDamageable, IDamageNumberTarget
    {
        [SerializeField] private float maxHealth = 100f;
        [SerializeField] private float respawnDelay = 3f;
        [SerializeField] private bool countsAsKill = true; // Whether killing this dummy counts as a kill
        
        public float CurrentHealth { get; private set; }
        public bool IsDead { get; private set; }
        
        public event System.Action OnDeath;
        public event System.Action OnRespawn;
        
        private Vector3 _spawnPosition;
        private Quaternion _spawnRotation;
        private GameObject _lastAttacker;

        private void Start()
        {
            _spawnPosition = transform.position;
            _spawnRotation = transform.rotation;
            CurrentHealth = maxHealth;
        }

        public void TakeDamage(float amount)
        {
            TakeDamage(amount, null);
        }
        
        public void TakeDamage(float amount, GameObject attacker)
        {
            if (IsDead) return;
            
            // Track damage for assists
            if (attacker != null && MatchStatBridge.Instance != null && countsAsKill)
            {
                MatchStatBridge.Instance.RecordDamage(attacker, gameObject, amount);
                _lastAttacker = attacker;
            }
            
            CurrentHealth = Mathf.Max(0, CurrentHealth - amount);
            
            if (CurrentHealth <= 0)
            {
                Die();
            }
        }

        private void Die()
        {
            if (IsDead) return;
            IsDead = true;
            
            // Record kill in match stats
            if (_lastAttacker != null && MatchStatBridge.Instance != null && countsAsKill)
            {
                MatchStatBridge.Instance.RecordKill(_lastAttacker, gameObject);
                Debug.Log($"[TargetDummy] {gameObject.name} killed by {_lastAttacker.name}");
            }
            
            OnDeath?.Invoke();
            Invoke(nameof(Respawn), respawnDelay);
        }

        private void Respawn()
        {
            IsDead = false;
            transform.position = _spawnPosition;
            transform.rotation = _spawnRotation;
            CurrentHealth = maxHealth;
            _lastAttacker = null;
            
            OnRespawn?.Invoke();
        }
    }
}
