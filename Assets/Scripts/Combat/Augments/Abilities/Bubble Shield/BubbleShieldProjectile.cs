using PurrNet;
using Resonance.Helper;
using UnityEngine;

namespace Resonance.Abilities.BubbleShield
{
    [RequireComponent(typeof(Rigidbody))]
    public class BubbleShieldProjectile : NetworkBehaviour, IDamageable
    {
        [Header("Shield Settings")]
        [SerializeField] private float shieldHealth = 100f;
        [SerializeField] private GameObject dome;
        [SerializeField] private float shieldDuration = 10f;
        private float _aliveTime;

        private Rigidbody _rigidbody;
        private SphereCollider _projectileCollider;
        private bool _isLanded;
        private float _currentHealth;

        private void Awake()
        {
            _rigidbody = GetComponent<Rigidbody>();
            _projectileCollider = GetComponent<SphereCollider>();
            _currentHealth = shieldHealth;

            if (dome != null)
            {
                dome.SetActive(false);
            }
        }
        
        private void Update()
        {
            if (!isServer || !_isLanded)
            {
                return;
            }

            _aliveTime += Time.deltaTime;

            if (_aliveTime >= shieldDuration)
            {
                DestroyShield();
            }
        }

        protected override void OnSpawned()
        {
            base.OnSpawned();
            _rigidbody.isKinematic = !isServer;
        }

        public void Launch(Vector3 velocity)
        {
            _rigidbody.linearVelocity = velocity;
        }

        private void OnCollisionEnter(Collision collision)
        {
            if (!isServer)
            {
                return;
            }

            if (_isLanded)
            {
                return;
            }

            Land();
        }

        private void Land()
        {
            _isLanded = true;

            _rigidbody.linearVelocity = Vector3.zero;
            _rigidbody.angularVelocity = Vector3.zero;
            _rigidbody.isKinematic = true;

            if (_projectileCollider != null)
            {
                _projectileCollider.enabled = false;
            }

            ActivateDomeObserversRpc();
        }

        [ObserversRpc(runLocally: true)]
        private void ActivateDomeObserversRpc()
        {
            if (dome != null)
            {
                dome.SetActive(true);
            }
        }

        public void TakeDamage(float damage, GameObject shooter)
        {
            if (!isServer)
            {
                return;
            }

            _currentHealth -= damage;

            if (_currentHealth <= 0f)
            {
                DestroyShield();
            }
        }

        private void DestroyShield()
        {
            if (isServer)
            {
                Destroy(gameObject);
            }
        }
    }
}