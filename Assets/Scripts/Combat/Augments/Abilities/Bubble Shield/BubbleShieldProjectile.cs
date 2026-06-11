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
        private BubbleShieldProjectileConfig config;

        [Header("References")] [SerializeField]
        private GameObject dome;

        [SerializeField] private BubbleShieldVisuals domeVisuals;

        private PredictedRigidbody _predictedRigidbody;
        private BubbleShieldProjectileState? _previousVerifiedState;

        protected override BubbleShieldProjectileState GetInitialState()
        {
            return new BubbleShieldProjectileState()
            {
                Health = config.shieldHealth,
                AliveTime = config.shieldDuration,
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
        }


        #region Simulation
        protected override void Simulate(ref BubbleShieldProjectileState state, float delta)
        {
            if (state.IsDespawning) return;

            if (!state.IsLanded)
            {
                // Deterministic landing: sweep a sphere straight down against static ground.
                // The projectile's position is reconciled and the ground never moves, so this
                // resolves to the same tick on every rollback/replay — no physics event, and it
                // works against plain static geometry that isn't a predicted identity.
                bool descending = _predictedRigidbody.linearVelocity.y <= 0.01f;
                if (descending && IsGrounded(transform.position))
                    Land(ref state);
                return;
            }

            state.AliveTime += delta;

            if (state.AliveTime >= config.shieldDuration - config.despawnAnimDuration)
                DestroyShield(ref state);
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
            currentState.Health -= damage;

            if (currentState.Health <= 0f)
                DestroyShield(ref currentState);
        }

        [SimulationOnly]
        private void Land(ref BubbleShieldProjectileState state)
        {
            state.IsLanded = true;

            // PredictedRigidbody owns velocity/kinematic in its OWN reconciled state, so
            // freezing it here rolls back together with the IsLanded flag.
            _predictedRigidbody.linearVelocity = Vector3.zero;
            _predictedRigidbody.angularVelocity = Vector3.zero;
            _predictedRigidbody.isKinematic = true;
        }

        [SimulationOnly]
        private void DestroyShield(ref BubbleShieldProjectileState state)
        {
            if (state.IsDespawning) return;
            state.IsDespawning = true;

            StartCoroutine(DestroyAfterDelay(config.despawnAnimDuration));
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

            bool previouslyLanded = _previousVerifiedState?.IsLanded ?? false;
            if (v.IsLanded && !previouslyLanded)
                ActivateDomeObservers();

            bool previouslyDespawning = _previousVerifiedState?.IsDespawning ?? false;
            if (v.IsDespawning && !previouslyDespawning)
                PlayDespawnObservers();

            _previousVerifiedState = v;
        }


        private void ActivateDomeObservers()
        {
            if (dome != null && !dome.activeSelf)
                dome.SetActive(true);
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

    public struct BubbleShieldProjectileState : IPredictedData<BubbleShieldProjectileState>
    {
        public float AliveTime;
        public bool IsDespawning;
        public bool IsLanded;
        public float Health;

        public void Dispose()
        {
        }
    }
}