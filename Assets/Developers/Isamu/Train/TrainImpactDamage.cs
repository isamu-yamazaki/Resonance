using System.Collections.Generic;
using PurrNet.Prediction;
using Resonance.Assemblies.Train;
using Resonance.Helper;
using UnityEngine;

namespace Resonance.Train
{
    [RequireComponent(typeof(Collider))]
    public class TrainImpactDamage : PredictedIdentity<TrainImpactDamageState>
    {
        [Header("Train Reference")]
        [SerializeField] private TrainController _trainController;

        [Header("Damage Scaling")]
        [SerializeField] private float _minDamage = 5f;
        [SerializeField] private float _maxDamage = 80f;
        [SerializeField] private float _speedForMaxDamage = 12f;
        [SerializeField] private float _minimumSpeedThreshold = 1f;

        [Header("Knockback")]
        [SerializeField] private float _knockbackForce = 40f;
        [SerializeField] private float _knockbackUpward = 25f;

        [Header("Cooldown")]
        [SerializeField] private float _damageCooldown = 1f;

        [Header("Detection")]
        [SerializeField] private LayerMask _passengerLayerMask = ~0;

        // Server-only dedup of recently-hit colliders, gating repeat damage. Deliberately NOT in predicted
        // state: it doesn't need to roll back or replicate, matching SonarDiscProjectile._scannedThisPulse.
        private readonly Dictionary<Collider, float> _cooldowns = new Dictionary<Collider, float>();

        // Reused by the overlap query each tick so it doesn't allocate, matching SonarDiscProjectile's
        // _pulseOverlapBuffer pattern.
        private readonly Collider[] _overlapBuffer = new Collider[16];

        private Collider _collider;
        private float _overlapRadius;

        protected override void LateAwake()
        {
            if (_trainController == null)
                _trainController = GetComponentInParent<TrainController>();

            _collider = GetComponent<Collider>();
            _overlapRadius = _collider.bounds.extents.magnitude;
        }

        protected override void Simulate(ref TrainImpactDamageState state, float delta)
        {
            if (_trainController == null) return;
            if (_trainController.CurrentSpeed < _minimumSpeedThreshold) return;

            // previously OnTriggerEnter, now a per-tick overlap query so hit detection runs inside the
            // simulation loop. Query the same PhysicsScene the prediction loop steps (see
            // SonarDiscProjectile.Simulate) so the query replays identically on every peer.
            PhysicsScene physicsScene = gameObject.scene.GetPhysicsScene();
            int hitCount = physicsScene.OverlapSphere(
                _collider.bounds.center,
                _overlapRadius,
                _overlapBuffer,
                _passengerLayerMask,
                QueryTriggerInteraction.Ignore);

            for (int i = 0; i < hitCount; i++)
            {
                ProcessHit(_overlapBuffer[i]);
            }
        }

        [SimulationOnly]
        private void ProcessHit(Collider other)
        {
            ApplyKnockback(other);

            float now = Time.time;
            if (_cooldowns.TryGetValue(other, out float lastHit) && now - lastHit < _damageCooldown)
                return;

            IDamageable target = other.GetComponentInParent<IDamageable>();
            if (target == null) return;

            _cooldowns[other] = now;
            ApplyDamage(target);
        }

        [SimulationOnly]
        private void ApplyDamage(IDamageable target)
        {
            float normalizedSpeed = Mathf.Clamp01(_trainController.CurrentSpeed / _speedForMaxDamage);
            float damage = Mathf.Lerp(_minDamage, _maxDamage, normalizedSpeed);
            target.TakeDamage(damage, gameObject);
        }

        [SimulationOnly]
        private void ApplyKnockback(Collider other)
        {
            TrainPassengerPhysics passengerPhysics = other.GetComponentInParent<TrainPassengerPhysics>();
            if (passengerPhysics == null) return;

            Vector3 trainTravel = _trainController.MoveDirection;
            trainTravel.y = 0f;
            trainTravel.Normalize();

            Vector3 toPlayer = other.transform.position - transform.position;
            toPlayer.y = 0f;

            Vector3 trackPerp = Vector3.Cross(trainTravel, Vector3.up);
            float side = Mathf.Sign(Vector3.Dot(toPlayer, trackPerp));
            Vector3 pushDirection = trackPerp * side;

            Vector3 knockbackDirection = pushDirection + Vector3.up * _knockbackUpward;
            passengerPhysics.SimulateApplyKnockback(knockbackDirection.normalized * _knockbackForce);
        }
    }

    public struct TrainImpactDamageState : IPredictedData<TrainImpactDamageState>
    {
        public void Dispose() { }
    }
}
