using System.Collections;
using PurrNet.Prediction;
using Resonance.Assemblies.AbilitySimulation.BubbleShield;
using Resonance.Helper;
using UnityEngine;

namespace Resonance.Abilities.BubbleShield
{
    [RequireComponent(typeof(Rigidbody))]
    public class BubbleShieldProjectile : PredictedIdentity<BubbleShieldProjectileState>, IDamageable
    {
        [SerializeField] private BubbleShieldProjectileConfig config;

        [Header("References")] [SerializeField]
        private GameObject dome;

        [SerializeField] private BubbleShieldVisuals domeVisuals;

        private PredictedTransform _predictedTransform;
        private PredictedRigidbody _predictedRigidbody;
        private BubbleShieldProjectileState? _previousVerifiedState;

        protected override BubbleShieldProjectileState GetInitialState()
        {
            return new BubbleShieldProjectileState()
            {
                Health = config.shieldHealth,
                AliveTime = 0f,
                IsDespawning = false,
                IsLanded = false
            };
        }

        protected override void LateAwake()
        {
            if (dome != null && domeVisuals == null)
                domeVisuals = dome.GetComponent<BubbleShieldVisuals>();

            if (dome != null)
                dome.SetActive(false);

            // Non-kinematic on every predicting peer (not just the server) so the lob arc
            // is simulated locally and rolls back / reconciles like any predicted body.
            _predictedRigidbody = GetComponent<PredictedRigidbody>();
            if (_predictedRigidbody != null)
                _predictedRigidbody.isKinematic = false;

            _predictedTransform = GetComponent<PredictedTransform>();
        }


        #region Simulation

        protected override void Simulate(ref BubbleShieldProjectileState state, float delta)
        {
            // Dome holds bullet-blocking collider, so must run on both client + server
            if (dome != null && dome.activeSelf != state.IsLanded)
                dome.SetActive(state.IsLanded);

            // Resolve the physics dependencies the simulation needs and pass them through the
            // context. Deterministic landing: sweep a sphere straight down against static ground.
            // The projectile's position is reconciled and the ground never moves, so this resolves
            // to the same tick on every rollback/replay — no physics event, and it works against
            // plain static geometry that isn't a predicted identity. The ground is only probed when
            // a landing can actually happen this tick (the simulation re-derives the descent check
            // from the same velocity + threshold).
            Vector3 velocity = _predictedRigidbody.linearVelocity;
            bool descending = velocity.y <= BubbleShieldProjectileSimulation.DescendVelocityThreshold;
            bool grounded = !state.IsDespawning && !state.IsLanded && descending
                            && IsGrounded(_predictedTransform.currentState.unityPosition);

            var ctx = new BubbleShieldProjectileSimulationContext(config, delta, velocity, grounded);
            BubbleShieldProjectileSimulation.Step(ctx, ref state);

            if (state.ShouldFreezeBody)
                FreezeBody();

            if (state.ShouldBeginDespawn)
                StartCoroutine(DestroyAfterDelay(config.despawnAnimDuration));
        }

        // Pure function of (reconciled) position + immovable static colliders → replay-safe.
        private bool IsGrounded(Vector3 position)
        {
            // Query the SAME PhysicsScene that the prediction loop steps in DoPhysicsPass,
            // rather than the global default scene.
            var physicsScene = gameObject.scene.GetPhysicsScene();

            // Lift the origin so a sphere already resting on the ground still registers —
            // SphereCast ignores colliders it starts overlapping.
            float radius = config.groundProbeRadius;
            Vector3 origin = position + Vector3.up * radius;
            float distance = radius + config.groundProbeDistance;

            return physicsScene.SphereCast(origin, radius, Vector3.down, out _,
                distance, config.groundMask, QueryTriggerInteraction.Ignore);
        }

        [SimulationOnly]
        public void Launch(Vector3 velocity)
        {
            _predictedRigidbody.linearVelocity = velocity;
        }

        [SimulationOnly]
        public void TakeDamage(float damage, GameObject shooter)
        {
            BubbleShieldProjectileSimulation.ApplyDamage(ref currentState, damage);

            if (currentState.ShouldBeginDespawn)
                StartCoroutine(DestroyAfterDelay(config.despawnAnimDuration));
        }

        // Freezes the body on the tick the simulation reports a landing. PredictedRigidbody owns
        // velocity/kinematic in its OWN reconciled state, so freezing it here rolls back together
        // with the IsLanded flag.
        [SimulationOnly]
        private void FreezeBody()
        {
            _predictedRigidbody.linearVelocity = Vector3.zero;
            _predictedRigidbody.angularVelocity = Vector3.zero;
            _predictedRigidbody.isKinematic = true;
        }

        private IEnumerator DestroyAfterDelay(float delay)
        {
            yield return new WaitForSeconds(delay);
            hierarchy.Delete(this);
        }

        #endregion

        #region UpdateView

        protected override void UpdateView(BubbleShieldProjectileState viewState, BubbleShieldProjectileState? verified)
        {
            if (!verified.HasValue) return;
            var v = verified.Value;

            float previousHealth = _previousVerifiedState?.Health ?? config.shieldHealth;
            if (v.Health < previousHealth)
                PlayHitFlashObservers();

            bool previouslyDespawning = _previousVerifiedState?.IsDespawning ?? false;
            if (v.IsDespawning && !previouslyDespawning)
                PlayDespawnObservers();

            _previousVerifiedState = v;
        }


        private void PlayHitFlashObservers()
        {
            domeVisuals?.PlayHitFlash();
        }


        private void PlayDespawnObservers()
        {
            domeVisuals?.PlayDespawnDissolve();
        }

        #endregion
    }
}