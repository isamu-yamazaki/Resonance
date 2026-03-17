using PurrNet;
using UnityEngine;

namespace Resonance.Environment
{
    [RequireComponent(typeof(Collider))]
    public class BreakableGlass : NetworkBehaviour, Resonance.Helper.IDamageable
    {
        [Header("References")]
        [SerializeField] private GlassShatterEffect shatterEffect;

        [Header("Settings")]
        [SerializeField] private float health = 1f;
        [SerializeField] private bool destroyPaneOnShatter = true;

        private bool _broken;
        private Vector3 _pendingHitPoint;
        private Vector3 _pendingHitNormal;
        private Vector3 _pendingHitDirection;
        private bool _hasPendingHitData;

        public void TakeDamage(float damage, GameObject shooter)
        {
            if (_broken) return;

            health -= damage;
            if (health > 0f) return;

            if (_hasPendingHitData)
            {
                BreakNow(_pendingHitPoint, _pendingHitNormal, _pendingHitDirection);
            }
            else
            {
                Vector3 hitDirection = shooter != null
                    ? (transform.position - shooter.transform.position).normalized
                    : Vector3.zero;

                BreakNow(transform.position, transform.forward, hitDirection);
            }
        }

        private void OnCollisionEnter(Collision collision)
        {
            if (_broken) return;
            if (collision.collider.transform.IsChildOf(transform)) return;

            ContactPoint contact = collision.GetContact(0);
            _pendingHitPoint = contact.point;
            _pendingHitNormal = contact.normal;
            _pendingHitDirection = collision.relativeVelocity.normalized;
            _hasPendingHitData = true;
        }

        [ObserversRpc(runLocally: true)]
        private void BreakNow(Vector3 hitPoint, Vector3 hitNormal, Vector3 hitDirection)
        {
            _broken = true;

            AkSoundEngine.PostEvent("Play_GlassShatter", gameObject);

            if (shatterEffect != null)
                shatterEffect.Shatter(hitPoint, hitNormal, hitDirection);

            if (destroyPaneOnShatter)
            {
                if (TryGetComponent(out MeshRenderer mr)) mr.enabled = false;
                if (TryGetComponent(out Collider col)) col.enabled = false;
                Destroy(gameObject, 3f);
            }
        }
    }
}
