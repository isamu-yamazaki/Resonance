using System.Collections;
using System.Collections.Generic;
using PurrNet;
using PurrNet.Prediction;
using Resonance.Helper;
using Resonance.PlayerController;
using UnityEngine;

namespace Resonance.Abilities.SonarDisc
{
    // Disc projectile — travels, sticks to surfaces/players, fires a sonar pulse on wall attach. Physics server-only, VFX broadcast to all clients.
    [RequireComponent(typeof(Rigidbody))]
    [RequireComponent(typeof(SphereCollider))]
    public class SonarDiscProjectile : PredictedIdentity<SonarDiscProjectileState>, IDamageable
    {
        [Header("Travel")] [SerializeField] private float travelSpeed = 28f;
        [SerializeField] private float maxRange = 40f;

        [Header("VFX")] [SerializeField] private Material deathGlitchMaterial;
        [SerializeField] private float glitchEffectDuration = 0.5f;

        [Header("Combat")] [SerializeField] private float discDamage = 5f;
        [SerializeField] private DamageNumber damageNumberPrefab;

        [Header("Pulse")] [SerializeField] private float pulseDelay = 1f;
        [SerializeField] private float pulseRadius = 30f;
        [SerializeField] private float pulseExpandDuration = 0.6f;
        [SerializeField] private LayerMask playerLayerMask;
        [SerializeField] private LayerMask occlusionLayerMask;

        [Header("Collision")] [SerializeField] private LayerMask discCollisionMask;

#if !UNITY_SERVER
        [Header("Wwise Events")]
        // TODO: Assign shoot event (Play_SD_Shoot) in inspector
        [SerializeField]
        private AK.Wwise.Event shootEvent;

        // TODO: Assign wall impact event (Play_SD_WallImpact) in inspector
        [SerializeField] private AK.Wwise.Event wallImpactEvent;

        // TODO: Assign pulse activation event (Play_SD_PulseActivate) in inspector
        [SerializeField] private AK.Wwise.Event pulseActivationEvent;

        // Plays spatially on the hit player for all clients (Play_SD_Distortion)
        [SerializeField] private AK.Wwise.Event hitPlayerEvent;

        // Plays only for the disc owner on a successful scan (Play_SD_Ping)
        [SerializeField] private AK.Wwise.Event scanConfirmedEvent;
#endif

        private PredictedRigidbody _predictedRigidbody;
        private PredictedTransform _predictedTransform;
        private SonarDiscProjectileState? _previousVerifiedState;

        // Reused by the pulse scan so the predicted tick doesn't allocate; sized for max players in range.
        private readonly Collider[] _pulseOverlapBuffer = new Collider[64];

        // Server-only dedup of players already reported this pulse. Deliberately NOT in predicted state:
        // it must not replicate (privacy) and the server never rolls back, so a plain field is correct.
        private readonly HashSet<PlayerID> _scannedThisPulse = new HashSet<PlayerID>();

        #region Lifecycle

        protected override void LateAwake()
        {
            _predictedRigidbody = GetComponent<PredictedRigidbody>();
            _predictedRigidbody.useGravity = false;
            _predictedRigidbody.isKinematic = false;

            _predictedTransform = GetComponent<PredictedTransform>();
        }

        #endregion

        #region Simulation loop

        [SimulationOnly]
        public void Launch(Vector3 direction)
        {
            currentState.LastPosition = transform.position;
            _predictedRigidbody.linearVelocity = direction.normalized * travelSpeed;
            _predictedTransform.currentState.unityRotation = Quaternion.LookRotation(direction.normalized);
        }

        protected override void Simulate(ref SonarDiscProjectileState state, float delta)
        {
            if (state.IsAttached)
            {
                FollowTarget(ref state);

                if (state.IsAttachedToPlayer)
                {
                }
                else
                {
                    WallPulseTick(ref state, delta);
                }

                return;
            }

            // previously in FixedUpdate, now runs by prediction tick.
            // Query the same PhysicsScene the prediction loop steps, not the global default scene,
            // so the swept hit-test replays identically on every peer.
            PhysicsScene physicsScene = gameObject.scene.GetPhysicsScene();
            Vector3 sweepStart = state.LastPosition;
            Vector3 sweepEnd = _predictedTransform.currentState.unityPosition;
            Vector3 sweep = sweepEnd - sweepStart;
            if (physicsScene.Raycast(sweepStart, sweep.normalized, out RaycastHit hit, sweep.magnitude,
                    discCollisionMask, QueryTriggerInteraction.Ignore))
            {
                if (!OwnerFinder.BelongsToOwner(hit.collider, owner))
                {
                    AttachToTarget(hit.collider, hit.point, hit.normal, ref state);
                    return;
                }
            }

            state.DistanceTravelled += Vector3.Distance(_predictedTransform.currentState.unityPosition, state.LastPosition);
            if (state.DistanceTravelled >= maxRange)
                DestroyDisc(ref state);

            // for the linecast in the next tick
            state.LastPosition = _predictedTransform.currentState.unityPosition;
        }


        #region IDamageable

        [SimulationOnly]
        public void TakeDamage(float damage, GameObject shooter)
        {
            // pretty sure this was the intention of disabling the collider
            // on attach in the old code; there might be more that i'm missing though
            if (!currentState.IsAttached)
            {
                DestroyDisc(ref currentState);
            }
        }

        #endregion

        #region Collision

        [SimulationOnly]
        private void AttachToTarget(
            Collider hitCollider,
            Vector3 hitPoint,
            Vector3 hitNormal,
            ref SonarDiscProjectileState state
        )
        {
            state.IsAttached = true;
            _scannedThisPulse.Clear();

            FreezeBody();

            Quaternion surfaceAlignment = Quaternion.LookRotation(-hitNormal);
            Vector3 attachPoint = hitPoint + hitNormal * 0.01f;

            bool hitPlayer = hitCollider.CompareTag("Player");

            // Record the attach in the target's local space so the disc rides moving surfaces (e.g. a train).
            // Reference the target by its PredictedObjectID (replay/pool-safe), not a Transform — FollowTarget
            // reconstructs the world pose from the target's reconciled transform each tick. Static geometry has
            // no predicted parent, so AttachTargetId stays null and the disc just holds this world pose.
            // .id is a PredictedComponentID (object + component); .objectId narrows it to the owning object.
            // Every predicted component on the target shares that objectId, so it's the same regardless of
            // which PredictedIdentity GetComponentInParent returned; TryGetComponent then fetches the transform.
            var targetIdentity = hitCollider.GetComponentInParent<PredictedIdentity>();
            if (targetIdentity != null && targetIdentity != this
                && hierarchy.TryGetComponent(targetIdentity.id.objectId, out PredictedTransform targetTransform))
            {
                Vector3 targetPos = targetTransform.currentState.unityPosition;
                Quaternion targetRot = targetTransform.currentState.unityRotation;
                Quaternion inverseTargetRot = Quaternion.Inverse(targetRot);

                state.AttachTargetId = targetIdentity.id.objectId;
                state.AttachLocalPos = inverseTargetRot * (attachPoint - targetPos);
                state.AttachLocalRot = inverseTargetRot * surfaceAlignment;
            }
            else
            {
                state.AttachTargetId = null;
            }

            _predictedTransform.currentState.SetPositionAndRotation(attachPoint, surfaceAlignment);
            if (hitPlayer)
            {
                OnAttachedToPlayer(hitCollider, ref state);
            }
        }

        [SimulationOnly]
        private void FreezeBody()
        {
            _predictedRigidbody.linearVelocity = Vector3.zero;
            _predictedRigidbody.angularVelocity = Vector3.zero;
            _predictedRigidbody.isKinematic = true;
        }

        #endregion

        #region Attachment Handlers

        [SimulationOnly]
        private void OnAttachedToPlayer(Collider playerCollider, ref SonarDiscProjectileState state)
        {
            IDamageable damageable = playerCollider.GetComponent<IDamageable>() ??
                                     playerCollider.GetComponentInParent<IDamageable>();
            if (damageable != null)
            {
                var ownerGameObject = OwnerFinder.FindPlayerGameObjectById(owner);
                damageable.TakeDamage(discDamage, ownerGameObject);

            }

            // TODO: start disorient coroutine on playerCollider's owner (phase 2)
            DestroyDisc(ref state);
        }

        #endregion

        #region Attach ticks

        // While attached, reconstruct the disc's world pose from the target's reconciled transform so it rides
        // moving surfaces. Pure function of the target's predicted state, so it replays identically once the
        // target is itself a PredictedIdentity. Static walls (null target) and a despawned target hold the
        // last pose written at attach.
        [SimulationOnly]
        private void FollowTarget(ref SonarDiscProjectileState state)
        {
            if (!state.AttachTargetId.HasValue)
                return;

            if (!hierarchy.TryGetComponent(state.AttachTargetId, out PredictedTransform targetTransform))
                return;

            Vector3 targetPos = targetTransform.currentState.unityPosition;
            Quaternion targetRot = targetTransform.currentState.unityRotation;

            _predictedTransform.currentState.SetPositionAndRotation(
                targetPos + targetRot * state.AttachLocalPos,
                targetRot * state.AttachLocalRot);
        }

        [SimulationOnly]
        private void WallPulseTick(ref SonarDiscProjectileState state, float delta)
        {
            if (state.IsDespawning)
                return;

            if (state.PulseElapsed >= pulseExpandDuration)
            {
                DestroyDisc(ref state);
                return;
            }

            switch (state.IsPulsing)
            {
                case false when state.PrePulseElapsed >= pulseDelay:
                    state.IsPulsing = true;
                    break;
                case false:
                    state.PrePulseElapsed += delta;
                    break;
                default:
                {
                    state.PulseElapsed += delta;
                    float currentRadius = Mathf.Lerp(0f, pulseRadius, state.PulseElapsed / pulseExpandDuration);

                    DetectAndNotifyOnServer(currentRadius);
                    break;
                }
            }
        }

        // Server-only: find players the expanding pulse newly reveals and notify the owner + each victim via
        // targeted RPCs. Nothing here touches predicted `state`, so it adds side effects without affecting
        // reconciliation on clients.
        [SimulationOnly]
        private void DetectAndNotifyOnServer(float currentRadius)
        {
            // Scan reveal is server-authoritative. The rollback/replay loop is client-only (PredictionManager
            // skips it when cachedIsServer), so the server runs this exactly once per tick — `isServer` alone
            // is a sufficient guard against double-firing, no isReplaying check needed.
            if (!isServer)
                return;

            if (!owner.HasValue)
                return; // no scanner to report to yet (owner is assigned by the ability)

            SonarScanNetworkAdapter adapter = SonarScanNetworkAdapter.Instance;
            if (adapter == null)
                return;

            Vector3 discPos = _predictedTransform.currentState.unityPosition;
            PhysicsScene physicsScene = gameObject.scene.GetPhysicsScene();
            int candidateCount = physicsScene.OverlapSphere(discPos, pulseRadius, _pulseOverlapBuffer,
                playerLayerMask, QueryTriggerInteraction.Ignore);

            for (int i = 0; i < candidateCount; i++)
            {
                Collider candidate = _pulseOverlapBuffer[i];

                if (OwnerFinder.BelongsToOwner(candidate, owner))
                    continue;

                Vector3 candidatePos = candidate.transform.position;
                if (Vector3.Distance(discPos, candidatePos) > currentRadius)
                    continue;

                // Occlusion: skip players behind walls.
                Vector3 directionToDisc = discPos - candidatePos;
                float distanceToDisc = directionToDisc.magnitude;
                if (physicsScene.Raycast(candidatePos, directionToDisc.normalized,
                        out RaycastHit occlusionHit, distanceToDisc, occlusionLayerMask,
                        QueryTriggerInteraction.Ignore))
                    continue;

                PlayerPredictedController victim = candidate.GetComponentInParent<PlayerPredictedController>();
                if (victim == null || !victim.owner.HasValue)
                    continue;

                PlayerID victimId = victim.owner.Value;

                // Dedup across the whole pulse so each player is reported at most once.
                if (!_scannedThisPulse.Add(victimId))
                    continue;

                adapter.NotifyOwnerOfDetection(owner.Value, victimId, candidatePos); // owner-only payload
                adapter.NotifyScannedSelf(victimId, discPos);                        // victim-only payload
            }
        }

        #endregion

        #region Destruction

        [SimulationOnly]
        private void DestroyDisc(ref SonarDiscProjectileState state)
        {
            if (state.IsDespawning) return;
            state.IsDespawning = true;

            // TODO: add a timeout for clients to play the glitch effect
            hierarchy.Delete(this);
        }
        #endregion

        #endregion

        #region Local view updates

        protected override void UpdateView(SonarDiscProjectileState viewState, SonarDiscProjectileState? verified)
        {
            if (!verified.HasValue) return;
            var v = verified.Value;

            var wasPreviouslyAttachedToPlayer = _previousVerifiedState?.IsAttachedToPlayer ?? false;
            var wasPreviouslyAttachedToWall = !wasPreviouslyAttachedToPlayer && (_previousVerifiedState?.IsAttached ?? false);
            if (v.AttachedPlayer.HasValue && !wasPreviouslyAttachedToPlayer)
            {
                var playerGameObject = OwnerFinder.FindPlayerGameObjectById(v.AttachedPlayer.Value);
                // TODO: double check where this exists
                var playerCollider = playerGameObject.GetComponentInChildren<Collider>();

                if (damageNumberPrefab != null && playerCollider.GetComponent<IDamageNumberTarget>() != null)
                {
                    DamageNumber number = Instantiate(damageNumberPrefab, playerCollider.transform.position,
                        Quaternion.identity);
                    number.Initialize(discDamage);
                }

                NotifyAttachedToPlayer(playerGameObject);
                BroadcastHitPlayerSound(playerGameObject);
            } else if (v.IsAttached && !wasPreviouslyAttachedToWall)
            {
                BroadcastWallImpact();
            }

            bool wasDespawning = _previousVerifiedState?.IsDespawning ?? false;
            if (v.IsDespawning && !wasDespawning)
            {
                NotifyDestroy();
            }

            _previousVerifiedState = v;
        }

        #region Audio

        public void BroadcastShootSound()
        {
#if !UNITY_SERVER
            if (shootEvent != null && shootEvent.IsValid())
                shootEvent.Post(gameObject);
#endif
        }

        private void BroadcastWallImpact()
        {
#if !UNITY_SERVER
            if (wallImpactEvent != null && wallImpactEvent.IsValid())
                wallImpactEvent.Post(gameObject);
#endif
        }

        private void BroadcastHitPlayerSound(GameObject hitPlayer)
        {
#if !UNITY_SERVER
            if (hitPlayerEvent != null && hitPlayerEvent.IsValid())
                hitPlayerEvent.Post(hitPlayer);
#endif
        }

        private void NotifyScanConfirmed(PlayerID target)
        {
#if !UNITY_SERVER
            if (scanConfirmedEvent != null && scanConfirmedEvent.IsValid())
                scanConfirmedEvent.Post(gameObject);
#endif
        }

        #endregion

        #region VFX

        private void NotifyPulseVFX()
        {
            SonarPulseEffect pulseEffect = GetComponent<SonarPulseEffect>();
            if (pulseEffect != null)
                pulseEffect.Play();

#if !UNITY_SERVER
            if (pulseActivationEvent != null && pulseActivationEvent.IsValid())
                pulseActivationEvent.Post(gameObject);
#endif
        }

        private void NotifyPlayerDetected(PlayerID target, GameObject detectedPlayer)
        {
            if (detectedPlayer == null)
                return;

            ScannedHighlight highlight = detectedPlayer.GetComponentInChildren<ScannedHighlight>();
            if (highlight != null)
                highlight.Play();
        }

        private void NotifyAttachedToPlayer(GameObject playerObject)
        {
            ElectrocuteEffect electrocuteEffect =
                playerObject.transform.root.GetComponentInChildren<ElectrocuteEffect>();
            if (electrocuteEffect != null)
                electrocuteEffect.Play();
        }

        #endregion


        #region Destruction

        private void NotifyDestroy()
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
        }

        #endregion

        #endregion

    }

    public struct SonarDiscProjectileState : IPredictedData<SonarDiscProjectileState>
    {
        public Vector3 LastPosition;

        public bool IsAttached;

        /// <summary>
        /// True if attached to a player, false if attached to another object.
        /// Use in combination with `IsAttached`.
        /// </summary>
        public bool IsAttachedToPlayer => AttachedPlayer.HasValue;

        /// <summary>
        /// Populated if attached to a player, null if attached to another object.
        /// Use in combination with `IsAttached`.
        /// </summary>
        public PlayerID? AttachedPlayer;

        /// <summary>
        /// The predicted object the disc is attached to (e.g. a moving train), or null for static world
        /// geometry. Referenced by id (not a Transform) so it survives rollback/replay and pooling.
        /// </summary>
        public PredictedObjectID? AttachTargetId;

        /// <summary>
        /// Attach pose stored in <see cref="AttachTargetId"/>'s local space; FollowTarget rebuilds the
        /// world pose from the target's reconciled transform each tick.
        /// </summary>
        public Vector3 AttachLocalPos;
        public Quaternion AttachLocalRot;

        public bool IsDespawning;

        /// <summary>
        /// For the view to fire the pulse VFX.
        /// This is fully controlled by the simulation and may run some time
        /// after attaching to a wall, which is why we need the separate property.
        /// </summary>
        public bool IsPulsing;

        public float PrePulseElapsed;
        public float PulseElapsed;
        public float DistanceTravelled;

        /// <summary>
        /// True for the first server tick.
        /// </summary>
        public bool JustSpawned;


        public void Dispose()
        {
        }
    }
}