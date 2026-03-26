using Resonance.PlayerController;
using UnityEngine;

namespace Resonance.Abilities.SonarDisc
{
    /// <summary>
    /// Projectile component for the Sonar Disc ability.
    /// Travels in a straight line and sticks to the first surface or player it hits.
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    [RequireComponent(typeof(SphereCollider))]
    public class SonarDiscProjectile : MonoBehaviour
    {
        [Header("Travel")]
        [SerializeField] private float travelSpeed = 28f;
        [SerializeField] private float maxRange = 40f;

        [Header("Attachment")]
        [SerializeField] private float attachRotationSmoothing = 12f;

        private Rigidbody _rigidbody;
        private bool _isAttached;
        private float _distanceTravelled;
        private Vector3 _lastPosition;
        private GameObject _owner;

        private void Awake()
        {
            _rigidbody = GetComponent<Rigidbody>();
            _rigidbody.useGravity = false;
            _rigidbody.interpolation = RigidbodyInterpolation.Interpolate;
            _rigidbody.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        }

        public void Launch(Vector3 direction, GameObject owner)
        {
            _owner = owner;
            _lastPosition = transform.position;
            _rigidbody.linearVelocity = direction.normalized * travelSpeed;

            // Orient disc face-forward along travel direction
            transform.rotation = Quaternion.LookRotation(direction.normalized);
        }

        private void FixedUpdate()
        {
            if (_isAttached)
                return;

            _distanceTravelled += Vector3.Distance(transform.position, _lastPosition);
            _lastPosition = transform.position;

            if (_distanceTravelled >= maxRange)
                Destroy(gameObject);
        }

        private void OnCollisionEnter(Collision collision)
        {
            if (_isAttached)
                return;

            // Ignore collision with the player who fired this disc
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

            // Align disc flat against the surface it hit — disc forward axis points away from the wall
            Quaternion surfaceAlignment = Quaternion.LookRotation(-hitNormal, Vector3.up);

            bool hitPlayer = hitCollider.CompareTag("Player");

            if (hitPlayer)
            {
                // Parent to player so disc rides with them
                transform.SetParent(hitCollider.transform, worldPositionStays: true);
                transform.SetPositionAndRotation(hitPoint, surfaceAlignment);
                OnAttachedToPlayer(hitCollider);
            }
            else
            {
                // Stick to world geometry — no parent needed
                transform.SetPositionAndRotation(hitPoint, surfaceAlignment);
                OnAttachedToWall();
            }
        }

        private void OnAttachedToPlayer(Collider playerCollider)
        {
            // TODO: trigger disorient effect on playerCollider's owner (phase 2)
            Debug.Log($"[SonarDisc] Attached to player: {playerCollider.transform.root.name}");
        }

        private void OnAttachedToWall()
        {
            // TODO: begin sonar pulse scan (phase 2)
            Debug.Log("[SonarDisc] Attached to wall.");
        }
    }
}
