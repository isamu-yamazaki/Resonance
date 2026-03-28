using System.Collections;
using System.Collections.Generic;
using PurrNet;
using Resonance.Helper;
using UnityEngine;

namespace Resonance.Abilities.SonarDisc
{
    // Disc projectile — travels, sticks to surfaces/players, fires a sonar pulse on wall attach. Physics server-only, VFX broadcast to all clients.
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
        [SerializeField] private float pulseRadius = 30f;
        [SerializeField] private float pulseExpandDuration = 0.6f;
        [SerializeField] private LayerMask playerLayerMask;
        [SerializeField] private LayerMask occlusionLayerMask;

        [Header("Collision")]
        [SerializeField] private LayerMask discCollisionMask;

        [Header("Wwise Events")]
        // TODO: Assign shoot event (Play_SD_Shoot) in inspector
        [SerializeField] private AK.Wwise.Event shootEvent;
        // TODO: Assign wall impact event (Play_SD_WallImpact) in inspector
        [SerializeField] private AK.Wwise.Event wallImpactEvent;
        // TODO: Assign pulse activation event (Play_SD_PulseActivate) in inspector
        [SerializeField] private AK.Wwise.Event pulseActivationEvent;
        // Plays spatially on the hit player for all clients (Play_SD_Distortion)
        [SerializeField] private AK.Wwise.Event hitPlayerEvent;
        // Plays only for the disc owner on a successful scan (Play_SD_Ping)
        [SerializeField] private AK.Wwise.Event scanConfirmedEvent;

        private Rigidbody _rigidbody;
        private bool _isAttached;
        private bool _isDestroyed;
        private float _distanceTravelled;
        private Vector3 _lastPosition;
        private GameObject _owner;
        private PlayerID _ownerPlayerID;

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
            _rigidbody.isKinematic = !isServer;
        }

        #endregion

        public void Launch(Vector3 direction, GameObject owner, PlayerID ownerPlayerID)
        {
            _owner = owner;
            _ownerPlayerID = ownerPlayerID;
            _lastPosition = transform.position;
            _rigidbody.linearVelocity = direction.normalized * travelSpeed;
            transform.rotation = Quaternion.LookRotation(direction.normalized);
        }

        private void Update()
        {
            if (!isServer) return;
            if (_isAttached) return;

            _lastPosition = transform.position;
        }

        private void FixedUpdate()
        {
            if (!isServer) return;
            if (_isAttached) return;

            if (Physics.Linecast(_lastPosition, transform.position, out RaycastHit hit, discCollisionMask, QueryTriggerInteraction.Ignore))
            {
                AttachToTarget(hit.collider, hit.point, hit.normal);
                return;
            }

            _distanceTravelled += Vector3.Distance(transform.position, _lastPosition);

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
            GetComponent<SphereCollider>().enabled = false;
            _rigidbody.isKinematic = true;

            Quaternion surfaceAlignment = Quaternion.LookRotation(-hitNormal);
            Vector3 attachPoint = hitPoint + hitNormal * 0.01f;

            bool hitPlayer = hitCollider.CompareTag("Player");

            if (hitPlayer)
            {
                transform.SetParent(hitCollider.transform, worldPositionStays: true);
                transform.SetPositionAndRotation(attachPoint, surfaceAlignment);
                NotifyAttachedToPlayerObserversRpc(hitCollider.gameObject, attachPoint, surfaceAlignment);
                OnAttachedToPlayer(hitCollider);
            }
            else
            {
                transform.SetParent(hitCollider.transform, worldPositionStays: true);
                StartCoroutine(RepositionAfterPhysics(attachPoint, surfaceAlignment));
                NotifyAttachedToWallObserversRpc(hitCollider.gameObject, attachPoint, surfaceAlignment);
                OnAttachedToWall();
            }
        }

        private IEnumerator RepositionAfterPhysics(Vector3 position, Quaternion rotation)
        {
            yield return new WaitForFixedUpdate();
            transform.SetPositionAndRotation(position, rotation);
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

            BroadcastHitPlayerSoundObserversRpc(playerCollider.gameObject);

            // TODO: start disorient coroutine on playerCollider's owner (phase 2)
            DestroyDisc();
        }

        private void OnAttachedToWall()
        {
            BroadcastWallImpactObserversRpc();
            StartCoroutine(WallPulseSequence());
        }

        #endregion

        #region Audio RPCs

        [ObserversRpc(runLocally: true)]
        public void BroadcastShootSoundObserversRpc()
        {
#if !UNITY_SERVER
            if (shootEvent != null && shootEvent.IsValid())
                shootEvent.Post(gameObject);
#endif
        }

        [ObserversRpc(runLocally: true)]
        private void BroadcastWallImpactObserversRpc()
        {
#if !UNITY_SERVER
            if (wallImpactEvent != null && wallImpactEvent.IsValid())
                wallImpactEvent.Post(gameObject);
#endif
        }

        [ObserversRpc(runLocally: true)]
        private void BroadcastHitPlayerSoundObserversRpc(GameObject hitPlayer)
        {
#if !UNITY_SERVER
            if (hitPlayerEvent != null && hitPlayerEvent.IsValid())
                hitPlayerEvent.Post(hitPlayer);
#endif
        }

        [TargetRpc]
        private void NotifyScanConfirmedOwnerRpc(PlayerID target)
        {
#if !UNITY_SERVER
            if (scanConfirmedEvent != null && scanConfirmedEvent.IsValid())
                scanConfirmedEvent.Post(gameObject);
#endif
        }

        #endregion

        #region VFX RPCs

        [ObserversRpc(runLocally: true)]
        private void NotifyPulseVFXObserversRpc()
        {
            SonarPulseEffect pulseEffect = GetComponent<SonarPulseEffect>();
            if (pulseEffect != null)
                pulseEffect.Play();

#if !UNITY_SERVER
            if (pulseActivationEvent != null && pulseActivationEvent.IsValid())
                pulseActivationEvent.Post(gameObject);
#endif
        }

        [TargetRpc]
        private void NotifyPlayerDetectedOwnerRpc(PlayerID target, GameObject detectedPlayer)
        {
            if (detectedPlayer == null)
                return;

            ScannedHighlight highlight = detectedPlayer.GetComponentInChildren<ScannedHighlight>();
            if (highlight != null)
                highlight.Play();
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
        private void NotifyAttachedToWallObserversRpc(GameObject hitObject, Vector3 hitPoint, Quaternion rotation)
        {
            transform.SetParent(hitObject.transform, worldPositionStays: true);
            transform.SetPositionAndRotation(hitPoint, rotation);
        }

        #endregion

        #region Pulse

        private IEnumerator WallPulseSequence()
        {
            yield return new WaitForSeconds(pulseDelay);

            if (_isDestroyed)
                yield break;

            NotifyPulseVFXObserversRpc();

            Collider[] candidates = Physics.OverlapSphere(transform.position, pulseRadius, playerLayerMask);
            HashSet<Collider> detected = new HashSet<Collider>();

            float elapsed = 0f;
            while (elapsed < pulseExpandDuration)
            {
                elapsed += Time.deltaTime;
                float currentRadius = Mathf.Lerp(0f, pulseRadius, elapsed / pulseExpandDuration);

                foreach (Collider candidate in candidates)
                {
                    if (detected.Contains(candidate))
                        continue;

                    if (_owner != null && candidate.transform.IsChildOf(_owner.transform))
                        continue;

                    if (Vector3.Distance(transform.position, candidate.transform.position) > currentRadius)
                        continue;

                    detected.Add(candidate);

                    Vector3 directionToDisc = transform.position - candidate.transform.position;
                    float distanceToDisc = directionToDisc.magnitude;
                    if (Physics.Raycast(candidate.transform.position, directionToDisc.normalized, out RaycastHit occlusionHit, distanceToDisc, occlusionLayerMask, QueryTriggerInteraction.Ignore))
                        continue;

                    NotifyPlayerDetectedOwnerRpc(_ownerPlayerID, candidate.gameObject);

                    ScannedHighlight scannedHighlight = candidate.GetComponentInChildren<ScannedHighlight>();
                    if (scannedHighlight != null && scannedHighlight.owner.HasValue)
                        NotifyScanConfirmedOwnerRpc(_ownerPlayerID);
                }

                yield return null;
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
