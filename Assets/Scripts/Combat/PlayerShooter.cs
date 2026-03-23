using System.Collections;
using System.Collections.Generic;
using PurrNet;
using Resonance.Combat.Weapons;
using Resonance.Combat.Weapons.Enums;
using Resonance.Helper;
using Resonance.Match;
using Resonance.PlayerController;
using UnityEngine;

namespace Resonance.Combat
{
    [RequireComponent(typeof(PlayerActionsInput))]
    public class PlayerShooter : NetworkBehaviour
    {
        #region Fields

        [Header("References")]
        [SerializeField] private PlayerEquip playerEquip;
        [SerializeField] private PlayerActionsInput playerActionsInput;
        [SerializeField] private Camera playerCamera;

        [SerializeField] private DamageNumber damageNumberPrefab;

        [Header("Debug")]
        [SerializeField] private bool debugAimRays;
        [SerializeField] private bool debugAmmoLogs;

        private float nextFireTime;
        private float reloadEndTime;
        private float currentSpread;

        private WeaponProperties lastWeapon;

        private Dictionary<WeaponProperties, int> ammoByWeapon = new Dictionary<WeaponProperties, int>();
        private Dictionary<GameObject, DamageNumber> pendingBatchedNumbers = new Dictionary<GameObject, DamageNumber>();

        private int currentAmmo
        {
            get
            {
                WeaponProperties weapon = playerEquip != null ? playerEquip.EquippedWeapon : null;
                if (weapon == null)
                {
                    return 0;
                }

                if (ammoByWeapon.TryGetValue(weapon, out int ammo))
                {
                    return ammo;
                }

                return weaponStatManager.MagazineSize;
            }
            set
            {
                WeaponProperties weapon = playerEquip != null ? playerEquip.EquippedWeapon : null;
                if (weapon == null)
                {
                    return;
                }

                ammoByWeapon[weapon] = value;
            }
        }

        [SerializeField] private LayerMask hitscanLayerMask;

        private PlayerViewModel viewModel;
        private WeaponStatManager weaponStatManager;
        private PlayerState playerState;

        public int CurrentAmmo => currentAmmo;

        private BulletProperties[] bulletProperties;

        #endregion

        #region Network

        protected override void OnSpawned()
        {
            base.OnSpawned();
            enabled = isOwner;
        }

        #endregion

        #region Startup

        private void Awake()
        {
            viewModel = GetComponent<PlayerViewModel>();
            playerState = GetComponent<PlayerState>();

            if (playerCamera == null)
            {
                playerCamera = Camera.main;
            }

            hitscanLayerMask = (1 << LayerMask.NameToLayer("Player")) | (1 << LayerMask.NameToLayer("Environment"));

            bulletProperties = Resources.LoadAll<BulletProperties>("Content/Bullets");
        }

        private void Start()
        {
            weaponStatManager = GetComponent<WeaponStatManager>();

            RefreshAmmoFromEquippedWeapon(true);
            viewModel.InitializeAmmo(MagazineSize);
        }

        #endregion

        #region Update

        private void Update()
        {
            if (playerActionsInput == null)
            {
                return;
            }

            RefreshAmmoFromEquippedWeapon(false);
            TickReload();

            if (!playerActionsInput.AttackHeld && !playerActionsInput.AttackPressed)
            {
                playerState.SetAttacking(false);
                currentSpread = Mathf.Max(
                    weaponStatManager.Spread,
                    currentSpread - weaponStatManager.SpreadRecoveryRate * Time.deltaTime
                );
            }

            if (playerActionsInput.ReloadPressed)
            {
                TryStartReload();
                playerActionsInput.SetReloadPressedFalse();
                return;
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

        #endregion

        #region Shooting

        private void TryShoot()
        {
            if (playerState.IsReloading) return;
            if (playerEquip == null) return;

            WeaponProperties weapon = playerEquip.EquippedWeapon;
            if (weapon == null) return;

            WeaponView view = playerEquip.CurrentWeaponView;
            if (view == null || view.Muzzle == null) return;

            float fireRate = weaponStatManager.FireRate;
            if (fireRate > 0f)
            {
                if (Time.time < nextFireTime) return;
                nextFireTime = Time.time + (1f / fireRate);
            }

            if (weaponStatManager.MagazineSize > 0)
            {
                if (currentAmmo <= 0)
                {
                    playerActionsInput.RequestReload();
                    return;
                }

                currentAmmo -= 1;
                viewModel.SetAmmo(currentAmmo, MagazineSize);
                playerState.SetAttacking(true);

                currentSpread += weaponStatManager.SpreadPerShot;
                currentSpread = Mathf.Min(currentSpread, weaponStatManager.MaxSpread);

                Debug.Log($"[Shooter] Current Spread: {currentSpread:0.000}");

                if (debugAmmoLogs)
                {
                    Debug.Log($"[Shooter] Fired. Ammo: {currentAmmo}/{weaponStatManager.MagazineSize}", this);
                }
            }

            int count = weaponStatManager.ProjectilesPerShot;
            if (count < 1) count = 1;

            WeaponPayload payload = BuildBasePayload(weapon);

            if (weapon.FiringType == WeaponFiringType.Hitscan)
            {
                Vector3 baseDirection = GetAimDirection(view.Muzzle);
                FireHitscan(weapon, view, payload, baseDirection, count);
            }
            else
            {
                Vector3 projectileDirection = GetProjectileAimDirection(view.Muzzle);
                FireProjectile(weapon, view, payload, projectileDirection, count);
            }
        }

        private WeaponPayload BuildBasePayload(WeaponProperties weapon)
        {
            WeaponPayload payload = new WeaponPayload();
            payload.Shooter = gameObject;
            payload.Damage = weaponStatManager.Damage;
            return payload;
        }

        private void FireProjectile(WeaponProperties weapon, WeaponView view, WeaponPayload payload, Vector3 baseDirection, int count)
        {
            BulletProperties bullet = weaponStatManager.GetBulletProperties();
            if (bullet == null || bullet.BulletPrefab == null) return;

            float speedMultiplier = weaponStatManager.MuzzleVelocity;
            float finalBulletSpeed = bullet.BulletBaseSpeed * speedMultiplier;

            payload.BulletSpeed = finalBulletSpeed;
            payload.BulletGravity = bullet.BulletGravity;

            for (int i = 0; i < count; i++)
            {
                Vector3 direction = ApplySpread(baseDirection, currentSpread);
                SpawnProjectile(bullet.BulletPrefab, view.Muzzle, payload, direction);
            }
        }

        private void FireHitscan(WeaponProperties weapon, WeaponView view, WeaponPayload payload, Vector3 baseDirection, int count)
        {
            if (playerCamera == null) return;

            Vector3 rayOrigin = playerCamera.transform.position;
            float hitscanMaxDistance = weaponStatManager.Range;
            bool hitPlayer = false;

            for (int i = 0; i < count; i++)
            {
                Vector3 dir = ApplySpread(baseDirection, currentSpread);
                Vector3 endPoint = rayOrigin + dir * hitscanMaxDistance;

                if (Physics.Raycast(rayOrigin, dir, out RaycastHit hit, hitscanMaxDistance, hitscanLayerMask, QueryTriggerInteraction.Ignore))
                {
                    float distance = hit.distance;
                    float finalDamage = ComputeDamageWithFalloff(payload.Damage, distance, weapon);

                    IDamageable target = hit.collider.GetComponent<IDamageable>() ?? hit.collider.GetComponentInParent<IDamageable>();
                    if (target != null && hit.collider.gameObject != gameObject && !hit.collider.transform.IsChildOf(transform))
                    {
                        target.TakeDamage(finalDamage, payload.Shooter);
                        hitPlayer = true;
                        if (damageNumberPrefab != null && hit.collider.GetComponent<IDamageNumberTarget>() != null)
                        {
                            if (count > 1)
                            {
                                GameObject hitRoot = hit.collider.transform.root.gameObject;

                                if (pendingBatchedNumbers.TryGetValue(hitRoot, out DamageNumber existing) && existing != null && !existing.IsBatchExpired)
                                {
                                    existing.AddDamage(finalDamage);
                                }
                                else
                                {
                                    DamageNumber number = Instantiate(damageNumberPrefab, hit.point, Quaternion.identity);
                                    number.InitializeBatched(finalDamage);
                                    pendingBatchedNumbers[hitRoot] = number;
                                }
                            }
                            else
                            {
                                DamageNumber number = Instantiate(damageNumberPrefab, hit.point, Quaternion.identity);
                                number.Initialize(finalDamage);
                            }
                        }
                    }
                    else
                    {
                        SpawnImpactDecal(hit);
                    }

                    endPoint = hit.point;

                    if (debugAimRays)
                    {
                        Debug.DrawLine(rayOrigin, hit.point, Color.yellow, 0.5f);
                        Debug.DrawRay(hit.point, hit.normal * 0.3f, Color.cyan, 0.5f);
                    }
                }

                BulletProperties hitscanBullet = weaponStatManager.GetBulletProperties();
                if (hitscanBullet?.BulletTrailPrefab != null && view.Muzzle != null)
                {
                    SpawnTrailOnAllClients(view.Muzzle.position, endPoint, hitscanBullet.Key);
                }
            }

            if (!hitPlayer)
                NotifyMissOnServer();
        }

        [ObserversRpc(runLocally: true)]
        private void SpawnTrailOnAllClients(Vector3 start, Vector3 end, string bulletPropertyKey)
        {
            BulletProperties properties = System.Array.Find(bulletProperties, w => w.Key == bulletPropertyKey);
            if (properties != null && properties.BulletTrailPrefab != null)
            {
                StartCoroutine(SpawnTrail(start, end, properties.BulletTrailPrefab));
            }
        }

        private IEnumerator SpawnTrail(Vector3 start, Vector3 end, TrailRenderer trailPrefab)
        {
            TrailRenderer trail = Instantiate(trailPrefab, start, Quaternion.identity);
            float duration = trail.time;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                trail.transform.position = Vector3.Lerp(start, end, elapsed / duration);
                elapsed += Time.deltaTime;
                yield return null;
            }

            trail.transform.position = end;
            Destroy(trail.gameObject, trail.time);
        }

        private void SpawnImpactDecal(RaycastHit hitInfo)
        {
            Debug.Log($"Spawn decal at {hitInfo.point}");
        }
        
        [ServerRpc]
        private void NotifyMissOnServer()
        {
            if (MatchStatBridge.Instance != null && owner.HasValue)
                MatchStatBridge.Instance.RecordMiss(gameObject);
        }

        private float ComputeDamageWithFalloff(float payloadDamage, float distance, WeaponProperties weapon)
        {
            if (distance > weaponStatManager.Range / 2)
            {
                return payloadDamage / 2;
            }

            return payloadDamage;
        }

        private void SpawnProjectile(GameObject prefab, Transform muzzle, WeaponPayload payload, Vector3 direction)
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

        #endregion

        #region Reload

        private void TickReload()
        {
            if (!playerState.IsReloading)
            {
                return;
            }

            float reloadDuration = weaponStatManager.ReloadTime;
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

        private void TryStartReload()
        {
            if (playerState.IsReloading)
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

            if (weaponStatManager.MagazineSize <= 0)
            {
                return;
            }

            if (currentAmmo >= weaponStatManager.MagazineSize)
            {
                return;
            }

            float reloadTime = weaponStatManager.ReloadTime;
            if (reloadTime <= 0f)
            {
                currentAmmo = weaponStatManager.MagazineSize;

                viewModel.SetReloadState(false);
                viewModel.SetReloadProgress(1f);
                viewModel.SetAmmo(currentAmmo, MagazineSize);

                if (debugAmmoLogs)
                {
                    Debug.Log($"[Shooter] Reload complete (instant). Ammo: {currentAmmo}/{weaponStatManager.MagazineSize}", this);
                }

                return;
            }

            playerState.SetReloading(true);
            reloadEndTime = Time.time + reloadTime;

            viewModel.SetReloadState(true);
            viewModel.SetReloadProgress(0f);

            if (debugAmmoLogs)
            {
                Debug.Log($"[Shooter] Reloading... {reloadTime:0.00}s", this);
            }
        }

        private void FinishReload()
        {
            playerState.SetReloading(false);

            if (playerEquip == null)
            {
                return;
            }

            WeaponProperties weapon = playerEquip.EquippedWeapon;
            if (weapon == null)
            {
                return;
            }

            currentAmmo = weaponStatManager.MagazineSize;

            viewModel.SetReloadState(false);
            viewModel.SetReloadProgress(1f);
            viewModel.SetAmmo(currentAmmo, MagazineSize);

            if (debugAmmoLogs)
            {
                Debug.Log($"[Shooter] Reload complete. Ammo: {currentAmmo}/{weaponStatManager.MagazineSize}", this);
            }
        }

        #endregion

        #region Weapon Refresh

        private void RefreshAmmoFromEquippedWeapon(bool force)
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
            playerState.SetReloading(false);

            currentSpread = weaponStatManager.Spread;

            viewModel.SetReloadState(false);
            viewModel.SetReloadProgress(0f);

            if (weaponStatManager.MagazineSize > 0)
            {
                if (!ammoByWeapon.ContainsKey(weapon))
                {
                    ammoByWeapon[weapon] = weaponStatManager.MagazineSize;
                }
                else if (force)
                {
                    ammoByWeapon[weapon] = Mathf.Min(ammoByWeapon[weapon], weaponStatManager.MagazineSize);
                }
            }

            viewModel.SetAmmo(currentAmmo, MagazineSize);

            if (debugAmmoLogs && weaponStatManager.MagazineSize > 0)
            {
                Debug.Log($"[Shooter] Equipped {weapon.name}. Ammo: {currentAmmo}/{weaponStatManager.MagazineSize}", this);
            }
        }

        public void RefreshWeaponStats()
        {
            RefreshAmmoFromEquippedWeapon(true);
            viewModel.SetAmmo(currentAmmo, MagazineSize);
        }

        #endregion

        #region Aim

        private Vector3 GetAimDirection(Transform muzzle)
        {
            if (playerCamera == null)
            {
                return muzzle.forward;
            }

            return playerCamera.transform.forward.normalized;
        }

        private Vector3 GetProjectileAimDirection(Transform muzzle)
        {
            if (playerCamera == null)
            {
                return muzzle.forward;
            }

            Vector3 cameraOrigin = playerCamera.transform.position;
            Vector3 cameraForward = playerCamera.transform.forward;

            Vector3 targetPoint;
            if (Physics.Raycast(cameraOrigin, cameraForward, out RaycastHit hit, weaponStatManager.Range, hitscanLayerMask, QueryTriggerInteraction.Ignore))
            {
                targetPoint = hit.point;
            }
            else
            {
                targetPoint = cameraOrigin + cameraForward * weaponStatManager.Range;
            }

            return (targetPoint - muzzle.position).normalized;
        }

        private Vector3 ApplySpread(Vector3 dir, float spreadDegrees)
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

        #endregion

        #region Properties

        public int MagazineSize
        {
            get
            {
                if (playerEquip == null) return 0;
                if (playerEquip.EquippedWeapon == null) return 0;
                return weaponStatManager.MagazineSize;
            }
        }

        public float ReloadProgress01
        {
            get
            {
                if (!playerState.IsReloading) return 0f;

                float reloadDuration = weaponStatManager.ReloadTime;
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
                return weaponStatManager.ReloadTime;
            }
        }

        #endregion
    }
}
