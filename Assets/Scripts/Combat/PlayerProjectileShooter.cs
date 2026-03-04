using PurrNet;
using Resonance.Combat.Weapons;
using Resonance.Combat.Weapons.Enums;
using Resonance.Helper;
using Resonance.PlayerController;
using UnityEngine;

namespace Resonance.Combat
{
    [RequireComponent(typeof(PlayerActionsInput))]
    public class PlayerProjectileShooter : NetworkBehaviour
    {
        [Header("References")]
        [SerializeField] PlayerEquip playerEquip;
        [SerializeField] PlayerActionsInput playerActionsInput;
        [SerializeField] Camera playerCamera;

        [Header("Debug")]
        [SerializeField] bool debugAimRays;
        [SerializeField] bool debugAmmoLogs;

        float nextFireTime;
        int currentAmmo;
        bool isReloading;
        float reloadEndTime;

        WeaponProperties lastWeapon;

        [SerializeField] private LayerMask hitscanLayerMask;
        
        private PlayerViewModel viewModel;
        
        public int CurrentAmmo => currentAmmo;
        public bool IsReloading => isReloading;

        protected override void OnSpawned()
        {
            base.OnSpawned();
            enabled = isOwner;

            var behaviour = GetComponent<NetworkBehaviour>();
            GiveOwnership(behaviour.owner);

            if (isOwner)
            {
                PlayerInputManager.Instance.PlayerControls.PlayerActionMap.Enable();
                PlayerInputManager.Instance.PlayerControls.PlayerActionMap.SetCallbacks(playerActionsInput);
            }
        }

        protected override void OnDespawned()
        {
            base.OnDespawned();

            if (isOwner)
            {
                PlayerInputManager.Instance.PlayerControls.PlayerActionMap.Disable();
                PlayerInputManager.Instance.PlayerControls.PlayerActionMap.RemoveCallbacks(playerActionsInput);
            }
        }

        private void Awake()
        {
            viewModel = GetComponent<PlayerViewModel>();

            if (playerCamera == null)
            {
                playerCamera = Camera.main;
            }

            hitscanLayerMask = (1 << LayerMask.NameToLayer("Player")) | (1 << LayerMask.NameToLayer("Environment"));
        }

        void Start()
        {
            RefreshAmmoFromEquippedWeapon(force: true);
            viewModel.InitializeAmmo(MagazineSize);
        }

        void Update()
        {
            if (playerActionsInput == null)
            {
                return;
            }

            RefreshAmmoFromEquippedWeapon(force: false);
            TickReload();

            if (playerActionsInput.ReloadPressed)
            {
                TryStartReload();
                playerActionsInput.SetReloadPressedFalse();
            }

            if (playerActionsInput.AttackPressed)
            {
                TryShoot();
                playerActionsInput.SetAttackPressedFalse();
            }

            if (playerActionsInput.AttackHeld)
            {
                TryShoot();
            }

        }

        void TryShoot()
        {
            if (isReloading) return;
            if (playerEquip == null) return;

            WeaponProperties weapon = playerEquip.EquippedWeapon;
            if (weapon == null) return;

            WeaponView view = playerEquip.CurrentWeaponView;
            if (view == null || view.Muzzle == null) return;

            // Fire rate gate
            float fireRate = weapon.FireRate;
            if (fireRate > 0f)
            {
                if (Time.time < nextFireTime) return;
                nextFireTime = Time.time + (1f / fireRate);
            }

            // Ammo gate
            if (weapon.MagazineSize > 0)
            {
                if (currentAmmo <= 0)
                {
                    playerActionsInput.RequestReload();
                    return;
                }

                currentAmmo -= 1;
                viewModel.SetAmmo(currentAmmo, MagazineSize);

                if (debugAmmoLogs)
                {
                    Debug.Log($"[Shooter] Fired. Ammo: {currentAmmo}/{weapon.MagazineSize}", this);
                }
            }

            // Universal: direction, spread, pellets, payload base
            Vector3 baseDirection = GetAimDirection(view.Muzzle);

            int count = weapon.ProjectilesPerShot;
            if (count < 1) count = 1;

            WeaponPayload payload = BuildBasePayload(weapon);

            if (weapon.FiringType == WeaponFiringType.Hitscan)
            {
                FireHitscan(weapon, view, payload, baseDirection, count);
            }
            else
            {
                FireProjectile(weapon, view, payload, baseDirection, count);
            }
        }

        private WeaponPayload BuildBasePayload(WeaponProperties weapon)
        {
            Debug.Log($"[PlayerProjectileShooter] Constructing base payload with owner {gameObject.GetComponent<NetworkBehaviour>().owner}");
            WeaponPayload payload = new WeaponPayload();
            payload.Shooter = gameObject;
            payload.Damage = weapon.Damage;
            return payload;
        }

        private void FireProjectile(WeaponProperties weapon, WeaponView view, WeaponPayload payload, Vector3 baseDirection, int count)
        {
            BulletProperties bullet = weapon.BulletProperties;
            if (bullet == null || bullet.BulletPrefab == null) return;

            float speedMultiplier = weapon.MuzzleVelocity;
            float finalBulletSpeed = bullet.BulletBaseSpeed * speedMultiplier;

            payload.BulletSpeed = finalBulletSpeed;
            payload.BulletGravity = bullet.BulletGravity;
            payload.DamageOverTime = bullet.DamageOverTime;
            payload.SpeedReduction = bullet.SpeedReduction;
            payload.Lifesteal = bullet.Lifesteal;

            for (int i = 0; i < count; i++)
            {
                Vector3 direction = ApplySpread(baseDirection, weapon.Spread);
                SpawnProjectile(bullet.BulletPrefab, view.Muzzle, payload, direction);
            }
        }

        void FireHitscan(WeaponProperties weapon, WeaponView view, WeaponPayload payload, Vector3 baseDirection, int count)
        {
            if (playerCamera == null) return;

            Vector3 rayOrigin = playerCamera.transform.position;
            float hitscanMaxDistance = weapon.Range;

            for (int i = 0; i < count; i++)
            {
                Vector3 dir = ApplySpread(baseDirection, weapon.Spread);

                if (Physics.Raycast(rayOrigin, dir, out RaycastHit hit, hitscanMaxDistance, hitscanLayerMask, QueryTriggerInteraction.Ignore))
                {
                    float distance = hit.distance;

                    float finalDamage = ComputeDamageWithFalloff(payload.Damage, distance, weapon);

                    if (hit.collider.TryGetComponent<IDamageable>(out var damageable) ||
                        hit.collider.GetComponentInParent<IDamageable>() != null)
                    {
                        IDamageable target = hit.collider.GetComponent<IDamageable>() ?? hit.collider.GetComponentInParent<IDamageable>();

                        target.TakeDamage(finalDamage, payload.Shooter);
                    }
                    else
                    {
                        SpawnImpactDecal(hit);
                    }

                    if (debugAimRays)
                    {
                        Debug.DrawLine(rayOrigin, hit.point, Color.yellow, 0.5f);
                        Debug.DrawRay(hit.point, hit.normal * 0.3f, Color.cyan, 0.5f);
                    }
                }
            }
        }

        private void SpawnImpactDecal(RaycastHit hitInfo)
        {
            Debug.Log($"Spawn decal at {hitInfo.point}");
        }


        void TryApplyDamage(Collider hitCollider, WeaponPayload payload)
        {
            if (hitCollider == null) return;

            IDamageable damageable = hitCollider.GetComponentInParent<IDamageable>();
            if (damageable == null) return;

            damageable.TakeDamage(payload.Damage, payload.Shooter);
        }

        private float ComputeDamageWithFalloff(float payloadDamage, float distance, WeaponProperties weapon)
        {
            if (distance > (weapon.Range / 2))
            {
                return (payloadDamage / 2);
            }

            return payloadDamage;
        }


        void TickReload()
        {
            if (!isReloading)
                return;

            float reloadDuration = playerEquip.EquippedWeapon.ReloadTime;
            float timeRemaining = reloadEndTime - Time.time;

            if (reloadDuration > 0f)
            {
                float progress = 1f - (timeRemaining / reloadDuration);
                viewModel.SetReloadProgress(Mathf.Clamp01(progress));
            }

            if (Time.time >= reloadEndTime)
            {
                FinishReload();
            }
        }

        void TryStartReload()
        {
            if (isReloading)
            {
                return;
            }

            if (playerEquip == null)
            {
                return;
            }

            WeaponProperties weapon = playerEquip.EquippedWeapon;
            if (weapon == null)
            {
                return;
            }

            if (weapon.MagazineSize <= 0)
            {
                return;
            }

            if (currentAmmo >= weapon.MagazineSize)
            {
                return;
            }

            float reloadTime = weapon.ReloadTime;
            if (reloadTime <= 0f)
            {
                currentAmmo = weapon.MagazineSize;

                viewModel.SetReloadState(false);
                viewModel.SetReloadProgress(1f);
                viewModel.SetAmmo(currentAmmo, MagazineSize);
                
                if (debugAmmoLogs)
                {
                    Debug.Log($"[Shooter] Reload complete (instant). Ammo: {currentAmmo}/{weapon.MagazineSize}", this);
                }

                return;
            }

            isReloading = true;
            reloadEndTime = Time.time + reloadTime;

            viewModel.SetReloadState(true);
            viewModel.SetReloadProgress(0f);

            if (debugAmmoLogs)
            {
                Debug.Log($"[Shooter] Reloading... {reloadTime:0.00}s", this);
            }
        }

        void FinishReload()
        {
            isReloading = false;

            if (playerEquip == null)
            {
                return;
            }

            WeaponProperties weapon = playerEquip.EquippedWeapon;
            if (weapon == null)
            {
                return;
            }

            currentAmmo = weapon.MagazineSize;

            viewModel.SetReloadState(false);
            viewModel.SetReloadProgress(1f);
            viewModel.SetAmmo(currentAmmo, MagazineSize);
            
            if (debugAmmoLogs)
            {
                Debug.Log($"[Shooter] Reload complete. Ammo: {currentAmmo}/{weapon.MagazineSize}", this);
            }
        }

        void RefreshAmmoFromEquippedWeapon(bool force)
        {
            if (playerEquip == null)
            {
                return;
            }

            WeaponProperties weapon = playerEquip.EquippedWeapon;
            if (weapon == null)
            {
                return;
            }

            if (!force && weapon == lastWeapon)
            {
                return;
            }

            lastWeapon = weapon;
            isReloading = false;

            viewModel.SetReloadState(false);
            viewModel.SetReloadProgress(0f);

            if (weapon.MagazineSize > 0)
            {
                currentAmmo = weapon.MagazineSize;
            }
            else
            {
                currentAmmo = 0;
            }

            viewModel.SetAmmo(currentAmmo, MagazineSize);

            if (debugAmmoLogs && weapon.MagazineSize > 0)
            {
                Debug.Log($"[Shooter] Equipped {weapon.name}. Ammo: {currentAmmo}/{weapon.MagazineSize}", this);
            }
        }

        Vector3 GetAimDirection(Transform muzzle)
        {
            if (playerCamera == null)
            {
                return muzzle.forward;
            }

            return playerCamera.transform.forward.normalized;
        }

        Vector3 ApplySpread(Vector3 dir, float spreadDegrees)
        {
            if (spreadDegrees <= 0f)
            {
                return dir;
            }

            float yaw = Random.Range(-spreadDegrees, spreadDegrees);
            float pitch = Random.Range(-spreadDegrees, spreadDegrees);

            Vector3 result = Quaternion.Euler(pitch, yaw, 0f) * dir;
            if (result.sqrMagnitude < 0.0001f)
            {
                return dir;
            }

            return result.normalized;
        }

        void SpawnProjectile(GameObject prefab, Transform muzzle, WeaponPayload payload, Vector3 direction)
        {
            GameObject go = Instantiate(prefab, muzzle.position, Quaternion.LookRotation(direction));

            WeaponProjectile projectile = go.GetComponent<WeaponProjectile>();
            if (projectile == null)
            {
                Debug.LogError("BulletPrefab is missing WeaponProjectile component.", go);
                return;
            }

            projectile.Initialize(payload, direction);
        }
        
        public int MagazineSize
        {
            get
            {
                if (playerEquip == null) return 0;
                if (playerEquip.EquippedWeapon == null) return 0;
                return playerEquip.EquippedWeapon.MagazineSize;
            }
        }
    
        public float ReloadProgress01
        {
            get
            {
                if (!isReloading) return 0f;

                float reloadDuration = playerEquip.EquippedWeapon.ReloadTime;
                float timeRemaining = reloadEndTime - Time.time;
                return Mathf.Clamp01(1f - (timeRemaining / reloadDuration));
            }
        }
        
        public float ReloadDuration
        {
            get
            {
                if (playerEquip == null) return 0f;
                if (playerEquip.EquippedWeapon == null) return 0f;
                return playerEquip.EquippedWeapon.ReloadTime;
            }
        }
    }
}
