using System;
using Resonance.Combat.Weapons;
using Resonance.Entities;
using Resonance.Helper;
using Resonance.Player;
using UnityEngine;

namespace Resonance.Combat.Weapons
{
    public class WeaponProjectile : MonoBehaviour
    {
        [Header("Projectile Settings")]
        [SerializeField] float maxLifetime = 20f;
        private float spawnTime;
        
        Rigidbody projectileRigidbody;
        WeaponPayload payload;
        private bool projectileInitialized;

        private void Awake()
        {
            projectileRigidbody = GetComponent<Rigidbody>();
        }

        public void Initialize(WeaponPayload weaponPayload, Vector3 direction)
        {
            payload = weaponPayload;
            projectileInitialized = true;
            spawnTime = Time.time;
            
            direction.Normalize();
            transform.forward = direction;
            
            projectileRigidbody.useGravity = payload.BulletGravity;
            projectileRigidbody.linearVelocity = direction * payload.BulletSpeed;
        }

        private void Update()
        {
            if (!projectileInitialized)
            {
                Debug.LogError("WeaponProjectile was spawned but never initialized", this);
                Destroy(gameObject);
                return;
            }

            if (Time.time - spawnTime >= maxLifetime) 
            {
                Destroy(gameObject);
            }
        }

        void OnCollisionEnter(Collision collision)
        {
            if (payload.Shooter != null && collision.transform.IsChildOf(payload.Shooter.transform))
            {
                return;
            }

            IDamageable damageable = collision.collider.GetComponent<IDamageable>();
            if (damageable == null)
            {
                damageable = collision.collider.GetComponentInParent<IDamageable>();
            }

            if (damageable != null)
            {
                damageable.TakeDamage(payload.Damage, payload.Shooter);
                Destroy(gameObject);
                return;
            }
            
            Destroy(gameObject);
        }
    }
}