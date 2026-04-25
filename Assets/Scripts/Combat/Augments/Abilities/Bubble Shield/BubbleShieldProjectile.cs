using System.Collections;
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
        [SerializeField] private float shieldDuration = 10f;

        [Header("References")]
        [SerializeField] private GameObject dome;
        [SerializeField] private BubbleShieldVisuals domeVisuals;

        [Header("Despawn Timing")]
        [SerializeField] private float despawnAnimDuration = 0.6f;

        private Rigidbody _rigidbody;
        private SphereCollider _projectileCollider;
        private bool _isLanded;
        private float _currentHealth;
        private float _aliveTime;

        private void Awake()
        {
            _rigidbody = GetComponent<Rigidbody>();
            _projectileCollider = GetComponent<SphereCollider>();
            _currentHealth = shieldHealth;

            if (dome != null)
            {
                dome.SetActive(false);

                if (domeVisuals == null)
                    domeVisuals = dome.GetComponent<BubbleShieldVisuals>();
            }
        }

        protected override void OnSpawned()
        {
            base.OnSpawned();
            _rigidbody.isKinematic = !isServer;
        }

        private void Update()
        {
            if (!isServer || !_isLanded) return;

            _aliveTime += Time.deltaTime;

            if (_aliveTime >= shieldDuration)
                DestroyShield();
        }

        public void Launch(Vector3 velocity)
        {
            _rigidbody.linearVelocity = velocity;
        }

        private void OnCollisionEnter(Collision collision)
        {
            if (!isServer || _isLanded) return;
            Land();
        }

        private void Land()
        {
            _isLanded = true;

            _rigidbody.linearVelocity = Vector3.zero;
            _rigidbody.angularVelocity = Vector3.zero;
            _rigidbody.isKinematic = true;

            if (_projectileCollider != null)
                _projectileCollider.enabled = false;

            ActivateDomeObserversRpc();
        }

        [ObserversRpc(runLocally: true)]
        private void ActivateDomeObserversRpc()
        {
            if (dome == null) return;
            // OnEnable in BubbleShieldVisuals fires PlaySpawnDissolve automatically
            dome.SetActive(true);
        }

        public void TakeDamage(float damage, GameObject shooter)
        {
            if (!isServer) return;

            _currentHealth -= damage;

            if (_currentHealth > 0f)
            {
                PlayHitFlashObserversRpc();
            }
            else
            {
                DestroyShield();
            }
        }

        [ObserversRpc(runLocally: true)]
        private void PlayHitFlashObserversRpc()
        {
            domeVisuals?.PlayHitFlash();
        }

        private void DestroyShield()
        {
            if (!isServer) return;
            PlayDespawnObserversRpc();
            StartCoroutine(DestroyAfterDelay(despawnAnimDuration));
        }

        [ObserversRpc(runLocally: true)]
        private void PlayDespawnObserversRpc()
        {
            domeVisuals?.PlayDespawnDissolve();
        }

        private IEnumerator DestroyAfterDelay(float delay)
        {
            yield return new WaitForSeconds(delay);
            Destroy(gameObject);
        }
    }
}
