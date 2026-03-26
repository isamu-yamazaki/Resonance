using System.Collections;
using PurrNet;
using Resonance.Helper;
using UnityEngine;

namespace Resonance.Abilities.SonarDisc
{
    /// <summary>
    /// Projectile component for the Sonar Disc ability.
    /// Travels in a straight line, sticks to the first surface or player it hits,
    /// and fires a sonar pulse if attached to a wall. Implements IDamageable so
    /// enemies can destroy it mid-air or before the pulse fires.
    /// Physics and hit detection run on server only. Visual effects broadcast to all clients.
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    [RequireComponent(typeof(SphereCollider))]
    public class SonarDiscProjectile : NetworkBehaviour, IDamageable
    {
        [Header("Travel")]
        [SerializeField] private float travelSpeed = 28f;
        [SerializeField] private float maxRange = 40f;

        [Header("VFX")]
        [SerializeField] private Material deathGlitchMaterial;
        [SerializeField] private float glitchEffectDuration = 0.5f;

        [Header("Combat")]
        [SerializeField] private float discDamage = 5f;
        [SerializeField] private DamageNumber damageNumberPrefab;

        [Header("Pulse")]
        [SerializeField] private float pulseDelay = 1f;
        [SerializeField] private float pulseRadius = 50f;
        [SerializeField] private LayerMask playerLayerMask;

        private Rigidbody _rigidbody;
        private bool _isAttached;
        private bool _isDestroyed;
        private float _distanceTravelled;
        private Vector3 _lastPosition;
        private Vector3 _lastVelocity;
        private GameObject _owner;

        private void Awake()
        {
            _rigidbody = GetComponent<Rigidbody>();
            _rigidbody.useGravity = false;
            _rigidbody.interpolation = RigidbodyInterpolation.Interpolate;
            _rigidbody.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        }

        #region Network

        protected override void OnSpawned()
        {
            base.OnSpawned();

            // Only server runs physics and hit detection
            _rigidbody.isKinematic = !isServer;
        }

        #endregion

        public void Launch(Vector3 direction, GameObject owner)
        {
            _owner = owner;
            _lastPosition = transform.position;
            _rigidbody.linearVelocity = direction.normalized * travelSpeed;
            transform.rotation = Quaternion.LookRotation(direction.normalized);
        }

        private void FixedUpdate()
        {
            if (!isServer) return;
            if (_isAttached) return;

            _lastVelocity = _rigidbody.linearVelocity;
            _distanceTravelled += Vector3.Distance(transform.position, _lastPosition);
            _lastPosition = transform.position;

            if (_distanceTravelled >= maxRange)
                DestroyDisc();
        }

        #region IDamageable

        public void TakeDamage(float damage, GameObject shooter)
        {
            if (!isServer) return;
            DestroyDisc();
        }

        #endregion

        #region Collision

        private void OnCollisionEnter(Collision collision)
        {
            if (!isServer) return;
            if (_isAttached) return;

            if (_owner != null && collision.collider.transform.IsChildOf(_owner.transform))
                return;

            ContactPoint contact = collision.GetContact(0);
            AttachToTarget(collision.collider, contact.point, contact.normal);
        }

        private void AttachToTarget(Collider hitCollider, Vector3 hitPoint, Vector3 hitNormal)
        {
            _isAttached = true;

            _rigidbody.linearVelocity = Vector3.zero;
            _rigidbody.angularVelocity = Vector3.zero;
            _rigidbody.isKinematic = true;

            Quaternion surfaceAlignment = Quaternion.LookRotation(-hitNormal);

            bool hitPlayer = hitCollider.CompareTag("Player");

            if (hitPlayer)
            {
                transform.SetParent(hitCollider.transform, worldPositionStays: true);
                transform.SetPositionAndRotation(hitPoint, surfaceAlignment);
                NotifyAttachedToPlayerObserversRpc(hitCollider.gameObject, hitPoint, surfaceAlignment);
                OnAttachedToPlayer(hitCollider);
            }
            else
            {
                transform.SetPositionAndRotation(hitPoint, surfaceAlignment);
                NotifyAttachedToWallObserversRpc(hitPoint, surfaceAlignment);
                OnAttachedToWall();
            }
        }

        #endregion

        #region Attachment Handlers

        private void OnAttachedToPlayer(Collider playerCollider)
        {
            ElectrocuteEffect electrocuteEffect = playerCollider.transform.root.GetComponentInChildren<ElectrocuteEffect>();
            if (electrocuteEffect != null)
                electrocuteEffect.Play();

            IDamageable damageable = playerCollider.GetComponent<IDamageable>() ?? playerCollider.GetComponentInParent<IDamageable>();
            if (damageable != null)
            {
                damageable.TakeDamage(discDamage, _owner);

                if (damageNumberPrefab != null && playerCollider.GetComponent<IDamageNumberTarget>() != null)
                {
                    DamageNumber number = Instantiate(damageNumberPrefab, playerCollider.transform.position, Quaternion.identity);
                    number.Initialize(discDamage);
                }
            }

            // TODO: start disorient coroutine on playerCollider's owner (phase 2)
            DestroyDisc();
        }

        private void OnAttachedToWall()
        {
            // TODO: play wall impact Wwise event here
            StartCoroutine(WallPulseSequence());
        }

        [ObserversRpc(runLocally: false)]
        private void NotifyAttachedToPlayerObserversRpc(GameObject playerObject, Vector3 hitPoint, Quaternion rotation)
        {
            transform.SetParent(playerObject.transform, worldPositionStays: true);
            transform.SetPositionAndRotation(hitPoint, rotation);

            ElectrocuteEffect electrocuteEffect = playerObject.transform.root.GetComponentInChildren<ElectrocuteEffect>();
            if (electrocuteEffect != null)
                electrocuteEffect.Play();
        }

        [ObserversRpc(runLocally: false)]
        private void NotifyAttachedToWallObserversRpc(Vector3 hitPoint, Quaternion rotation)
        {
            transform.SetPositionAndRotation(hitPoint, rotation);
        }

        #endregion

        #region Pulse

        private IEnumerator WallPulseSequence()
        {
            yield return new WaitForSeconds(pulseDelay);

            if (_isDestroyed)
                yield break;

            FirePulse();
        }

        private void FirePulse()
        {
            // TODO: play pulse activation Wwise event here

            Collider[] hitColliders = Physics.OverlapSphere(transform.position, pulseRadius, playerLayerMask);

            foreach (Collider hitCollider in hitColliders)
            {
                if (_owner != null && hitCollider.transform.IsChildOf(_owner.transform))
                    continue;

                // TODO: reveal detected player to owner (phase 2)
            }

            DestroyDisc();
        }

        #endregion

        #region Destruction

        private void DestroyDisc()
        {
            if (_isDestroyed) return;
            _isDestroyed = true;
            NotifyDestroyObserversRpc();
        }

        [ObserversRpc(runLocally: true)]
        private void NotifyDestroyObserversRpc()
        {
            StartCoroutine(GlitchAndDestroy());
        }

        private IEnumerator GlitchAndDestroy()
        {
            MeshRenderer meshRenderer = GetComponentInChildren<MeshRenderer>();

            if (deathGlitchMaterial != null && meshRenderer != null)
            {
                meshRenderer.material = deathGlitchMaterial;

                float elapsed = 0f;
                while (elapsed < glitchEffectDuration)
                {
                    elapsed += Time.deltaTime;
                    meshRenderer.material.SetFloat(Shader.PropertyToID("_GlitchTime"), elapsed / glitchEffectDuration);
                    yield return null;
                }
            }

            if (isServer)
                Destroy(gameObject);
        }

        #endregion
    }
}
