using System.Collections;
using PurrNet;
using PurrNet.Prediction;
using Resonance.Audio;
using Resonance.Combat.Weapons;
using Resonance.Combat.Weapons.Enums;
using Resonance.Helper;
using Resonance.Match;
using Resonance.PlayerController;
using UnityEngine;

namespace Resonance.Combat
{
    public class PlayerShooter : PredictedIdentity<PlayerShooterInputData, PlayerShooterDataState>
    {
        #region Fields

        [Header("References")]
        private PlayerEquip playerEquip;
        private FPArmsManager fpArmsManager;
        private PlayerActionsInput playerActionsInput;
        [SerializeField] private Camera playerCamera;

        [SerializeField] private DamageNumber damageNumberPrefab;

        [Header("Debug")]
        [SerializeField] private bool debugAimRays;
        [SerializeField] private bool debugAmmoLogs;

        [SerializeField] private LayerMask hitscanLayerMask;

        private PlayerViewModel viewModel;
        private WeaponStatManager weaponStatManager;
        private PlayerState playerState;

        public int CurrentAmmo
        {
            get
            {
                if (playerEquip == null) return 0;
                int slot = playerEquip.currentState.CurrentSlot;
                return slot == 0 ? currentState.AmmoSlot0 : currentState.AmmoSlot1;
            }
        }

        private int _lastViewedShotCount;
        private int _lastViewedReloadStartCount;
        private int _lastViewedReloadEndCount;
        private int _lastViewedEmptyTriggerCount;

        #endregion

        #region Startup

        protected override void LateAwake()
        {
            viewModel = GetComponent<PlayerViewModel>();
            playerState = GetComponent<PlayerState>();
            playerActionsInput = PlayerActionsInput.Instance;
            playerEquip = GetComponent<PlayerEquip>();
            fpArmsManager = GetComponent<FPArmsManager>();

            if (playerCamera == null)
                playerCamera = Camera.main;

            hitscanLayerMask = (1 << LayerMask.NameToLayer("Player")) | (1 << LayerMask.NameToLayer("Environment"));

            weaponStatManager = GetComponent<WeaponStatManager>();

            if (viewModel != null)
                viewModel.InitializeAmmo(MagazineSize);
        }

        #endregion

        #region PredictedIdentity overrides

        protected override PlayerShooterDataState GetInitialState()
        {
            return new PlayerShooterDataState
            {
                LastEquippedSlot = -1,
                AmmoSlot0 = 0,
                AmmoSlot1 = 0,
            };
        }

        protected override void GetFinalInput(ref PlayerShooterInputData input)
        {
            if (playerActionsInput == null) return;

            if (playerActionsInput.AttackPressed)
            {
                input.AttackPressed = true;
                playerActionsInput.SetAttackPressedFalse();
            }

            input.AttackHeld = playerActionsInput.AttackHeld;

            if (playerActionsInput.ReloadPressed)
            {
                input.ReloadPressed = true;
                playerActionsInput.SetReloadPressedFalse();
            }
        }

        protected override void Simulate(PlayerShooterInputData input, ref PlayerShooterDataState state, float delta)
        {
            if (playerEquip == null || weaponStatManager == null) return;

            // 1. Decrement timers
            state.FireCooldown = Mathf.Max(0f, state.FireCooldown - delta);

            bool wasReloading = state.ReloadTimer > 0f;
            if (wasReloading)
            {
                state.ReloadTimer -= delta;
                if (state.ReloadTimer < 0f) state.ReloadTimer = 0f;
            }

            // 2. Detect weapon change
            int currentSlot = playerEquip.currentState.CurrentSlot;
            if (state.LastEquippedSlot != currentSlot)
            {
                state.LastEquippedSlot = currentSlot;
                state.CurrentSpread = weaponStatManager.Spread;
                state.ReloadTimer = 0f;
                ref int slotAmmo = ref (currentSlot == 0 ? ref state.AmmoSlot0 : ref state.AmmoSlot1);
                if (slotAmmo == 0)
                    slotAmmo = weaponStatManager.MagazineSize;
            }

            // 3. Finish reload when timer expires
            if (wasReloading && state.ReloadTimer <= 0f)
            {
                ref int reloadedAmmo = ref (currentSlot == 0 ? ref state.AmmoSlot0 : ref state.AmmoSlot1);
                reloadedAmmo = weaponStatManager.MagazineSize;
                state.CurrentSpread = weaponStatManager.Spread;
                state.ReloadEndCount++;
            }

            // 4. Spread recovery when not firing
            if (!input.AttackHeld && !input.AttackPressed)
            {
                state.CurrentSpread = Mathf.Max(
                    weaponStatManager.Spread,
                    state.CurrentSpread - weaponStatManager.SpreadRecoveryRate * delta
                );
            }

            // 5. Reload input
            if (input.ReloadPressed && state.ReloadTimer <= 0f)
            {
                TrySimulateReload(ref state, currentSlot);
                return;
            }

            // 6. Shoot input
            if ((input.AttackPressed || input.AttackHeld) && state.ReloadTimer <= 0f && state.FireCooldown <= 0f)
            {
                TrySimulateShoot(ref state, currentSlot);
            }
        }

        private void TrySimulateReload(ref PlayerShooterDataState state, int currentSlot)
        {
            WeaponProperties weapon = playerEquip.EquippedWeapon;
            if (weapon == null) return;
            if (weaponStatManager.MagazineSize <= 0) return;

            int currentAmmo = currentSlot == 0 ? state.AmmoSlot0 : state.AmmoSlot1;
            if (currentAmmo >= weaponStatManager.MagazineSize) return;

            bool isEmpty = currentAmmo <= 0;
            AnimationClip clip = isEmpty ? weapon.EmptyReloadClip : weapon.ReloadClip;
            float reloadTime = clip != null ? clip.length : weaponStatManager.ReloadTime;

            if (reloadTime <= 0f)
            {
                ref int ammoRef = ref (currentSlot == 0 ? ref state.AmmoSlot0 : ref state.AmmoSlot1);
                ammoRef = weaponStatManager.MagazineSize;
                state.ReloadEndCount++;
                return;
            }

            state.ReloadTimer = reloadTime;
            state.IsEmptyReload = isEmpty;
            state.ReloadStartCount++;

#if UNITY_EDITOR
            if (debugAmmoLogs)
                Debug.Log($"[Shooter] Reloading... {reloadTime:0.00}s", this);
#endif
        }

        private void TrySimulateShoot(ref PlayerShooterDataState state, int currentSlot)
        {
            if (playerState.IsMatchFrozen()) return;

            WeaponProperties weapon = playerEquip.EquippedWeapon;
            if (weapon == null) return;

            WeaponView view = GetActiveWeaponView();
            if (view == null || view.Muzzle == null) return;

            float fireRate = weaponStatManager.FireRate;
            if (fireRate > 0f)
                state.FireCooldown = 1f / fireRate;

            if (weaponStatManager.MagazineSize > 0)
            {
                ref int ammoRef = ref (currentSlot == 0 ? ref state.AmmoSlot0 : ref state.AmmoSlot1);

                if (ammoRef <= 0)
                {
                    state.EmptyTriggerCount++;
                    if (playerActionsInput != null)
                        playerActionsInput.RequestReload();
                    return;
                }

                ammoRef--;

                state.CurrentSpread += weaponStatManager.SpreadPerShot;
                state.CurrentSpread = Mathf.Min(state.CurrentSpread, weaponStatManager.MaxSpread);

#if UNITY_EDITOR
                if (debugAmmoLogs)
                    Debug.Log($"[Shooter] Fired. Ammo: {ammoRef}/{weaponStatManager.MagazineSize}", this);
#endif
            }

            int count = weaponStatManager.ProjectilesPerShot;
            if (count < 1) count = 1;

            WeaponPayload payload = BuildBasePayload(weapon);

            state.LastShotHitPlayer = false;
            state.LastShotDamage = 0f;
            state.LastShotEndPoint = view.Muzzle.position + GetAimDirection(view.Muzzle) * weaponStatManager.Range;

            if (weapon.FiringType == WeaponFiringType.Hitscan)
            {
                Vector3 baseDirection = GetAimDirection(view.Muzzle);
                FireHitscan(weapon, view, payload, baseDirection, count, ref state);
            }
            else
            {
                Vector3 projectileDirection = GetProjectileAimDirection(view.Muzzle);
                FireProjectile(weapon, view, payload, projectileDirection, count, state.CurrentSpread);
            }

            state.ShotCount++;
        }

        protected override PlayerShooterDataState Interpolate(
            PlayerShooterDataState from,
            PlayerShooterDataState to,
            float t)
        {
            return to;
        }

        protected override void UpdateView(PlayerShooterDataState viewState, PlayerShooterDataState? verified)
        {
            if (playerEquip == null) return;

            int newShots = viewState.ShotCount - _lastViewedShotCount;
            int newReloadStarts = viewState.ReloadStartCount - _lastViewedReloadStartCount;
            int newReloadEnds = viewState.ReloadEndCount - _lastViewedReloadEndCount;
            int newEmptyTriggers = viewState.EmptyTriggerCount - _lastViewedEmptyTriggerCount;

            // Update weapon animation state
            if (viewState.ReloadTimer > 0f)
            {
                playerState?.SetWeaponState(viewState.IsEmptyReload ? WeaponState.EmptyReloading : WeaponState.Reloading);
            }
            else if (newShots > 0)
            {
                playerState?.SetWeaponState(WeaponState.Shooting);
            }
            else
            {
                if (playerState?.CurrentWeaponState == WeaponState.Shooting ||
                    playerState?.CurrentWeaponState == WeaponState.Reloading ||
                    playerState?.CurrentWeaponState == WeaponState.EmptyReloading)
                {
                    playerState.SetWeaponState(WeaponState.Idle);
                }
            }

            // Update ammo UI
            int currentSlot = playerEquip.currentState.CurrentSlot;
            int displayAmmo = currentSlot == 0 ? viewState.AmmoSlot0 : viewState.AmmoSlot1;
            viewModel?.SetAmmo(displayAmmo, MagazineSize);
            viewModel?.SetReloadState(viewState.ReloadTimer > 0f);

            if (viewState.ReloadTimer > 0f && weaponStatManager != null)
            {
                WeaponProperties weapon = playerEquip.EquippedWeapon;
                AnimationClip clip = viewState.IsEmptyReload ? weapon?.EmptyReloadClip : weapon?.ReloadClip;
                float reloadDuration = clip != null ? clip.length : weaponStatManager.ReloadTime;
                if (reloadDuration > 0f)
                    viewModel?.SetReloadProgress(Mathf.Clamp01(1f - (viewState.ReloadTimer / reloadDuration)));
            }
            else if (newReloadEnds > 0)
            {
                viewModel?.SetReloadProgress(1f);
                viewModel?.SetReloadState(false);
            }

            // Shot effects
            if (newShots > 0)
            {
                WeaponView currentView = GetActiveWeaponView();
                if (currentView != null)
                {
                    WeaponAudioProperties audioProperties = weaponStatManager?.GetAudioProperties();
                    if (audioProperties != null)
                        currentView.ApplyAudioProperties(audioProperties);

                    MuzzleFlashSettings flashSettings = weaponStatManager?.GetMuzzleFlashSettings();
                    if (flashSettings != null)
                        currentView.ApplyMuzzleFlashSettings(flashSettings);

                    currentView.PlayFire();
                    currentView.PlayMuzzleFlash();
                }

#if !UNITY_SERVER
                if (AudioSourceTracker.Instance != null)
                    AudioSourceTracker.Instance.RegisterSound(transform.position, 1f);
#endif

                if (isOwner)
                {
                    GetActiveWeaponView()?.GetComponentInChildren<MuzzleScreenShake>()?.Shake();

                    if (viewState.LastShotHitPlayer && damageNumberPrefab != null)
                    {
                        DamageNumber number = Instantiate(damageNumberPrefab, viewState.LastShotEndPoint, Quaternion.identity);
                        number.Initialize(viewState.LastShotDamage);
                    }

                    if (!viewState.LastShotHitPlayer)
                        NotifyMissOnServer();
                }

                // Bullet trail
                BulletProperties hitscanBullet = weaponStatManager?.GetBulletProperties();
                if (hitscanBullet?.BulletTrailPrefab != null)
                {
                    Vector3 startPos = isOwner
                        ? (fpArmsManager?.GetActiveFPWeaponView()?.Muzzle.position ?? viewState.LastShotEndPoint)
                        : (playerEquip.CurrentWeaponView?.Muzzle.position ?? viewState.LastShotEndPoint);
                    StartCoroutine(SpawnTrail(startPos, viewState.LastShotEndPoint, hitscanBullet.BulletTrailPrefab));
                }
            }

            // Reload start effects
            if (newReloadStarts > 0)
            {
                WeaponView reloadView = isOwner
                    ? fpArmsManager?.GetActiveFPWeaponView()
                    : playerEquip.CurrentWeaponView;
                reloadView?.PlayReload();
                viewModel?.SetReloadProgress(0f);
            }

            // Empty trigger effects
            if (newEmptyTriggers > 0)
            {
                WeaponView triggerView = isOwner
                    ? fpArmsManager?.GetActiveFPWeaponView()
                    : playerEquip.CurrentWeaponView;
                triggerView?.PlayEmptyTrigger();
            }

            _lastViewedShotCount = viewState.ShotCount;
            _lastViewedReloadStartCount = viewState.ReloadStartCount;
            _lastViewedReloadEndCount = viewState.ReloadEndCount;
            _lastViewedEmptyTriggerCount = viewState.EmptyTriggerCount;
        }

        #endregion

        #region Shooting

        private WeaponPayload BuildBasePayload(WeaponProperties weapon)
        {
            return new WeaponPayload
            {
                Shooter = gameObject,
                Damage = weaponStatManager.Damage,
            };
        }

        private void FireProjectile(WeaponProperties weapon, WeaponView view, WeaponPayload payload, Vector3 baseDirection, int count, float spread)
        {
            BulletProperties bullet = weaponStatManager.GetBulletProperties();
            if (bullet == null || bullet.BulletPrefab == null) return;

            float speedMultiplier = weaponStatManager.MuzzleVelocity;
            float finalBulletSpeed = bullet.BulletBaseSpeed * speedMultiplier;

            payload.BulletSpeed = finalBulletSpeed;
            payload.BulletGravity = bullet.BulletGravity;

            for (int i = 0; i < count; i++)
            {
                Vector3 direction = ApplySpread(baseDirection, spread);
                SpawnProjectile(bullet.BulletPrefab, view.Muzzle, payload, direction);
            }
        }

        private void FireHitscan(WeaponProperties weapon, WeaponView view, WeaponPayload payload, Vector3 baseDirection, int count, ref PlayerShooterDataState state)
        {
            if (playerCamera == null) return;

            Vector3 rayOrigin = playerCamera.transform.position;
            float hitscanMaxDistance = weaponStatManager.Range;

            for (int i = 0; i < count; i++)
            {
                Vector3 dir = ApplySpread(baseDirection, state.CurrentSpread);
                Vector3 endPoint = rayOrigin + dir * hitscanMaxDistance;

                if (Physics.Raycast(rayOrigin, dir, out RaycastHit hit, hitscanMaxDistance, hitscanLayerMask, QueryTriggerInteraction.Ignore))
                {
                    float distance = hit.distance;
                    float finalDamage = ComputeDamageWithFalloff(payload.Damage, distance, weapon);

                    IDamageable target = hit.collider.GetComponent<IDamageable>() ?? hit.collider.GetComponentInParent<IDamageable>();
                    if (target != null && hit.collider.gameObject != gameObject && !hit.collider.transform.IsChildOf(transform))
                    {
                        target.TakeDamage(finalDamage, payload.Shooter);
                        state.LastShotHitPlayer = true;
                        state.LastShotDamage += finalDamage;
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

                state.LastShotEndPoint = endPoint;
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
#if UNITY_EDITOR
            Debug.Log($"Spawn decal at {hitInfo.point}");
#endif
        }

        private void NotifyMissOnServer()
        {
            var matchStats = MatchStatBridge.GetTemporaryReference();
            if (matchStats != null && owner.HasValue)
                matchStats.RecordMiss(gameObject);
        }

        private float ComputeDamageWithFalloff(float payloadDamage, float distance, WeaponProperties weapon)
        {
            if (distance > weaponStatManager.Range / 2)
                return payloadDamage / 2;

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

        private WeaponView GetActiveWeaponView()
        {
            if (isOwner && fpArmsManager != null)
                return fpArmsManager.GetActiveFPWeaponView();

            return playerEquip?.CurrentWeaponView;
        }

        #endregion

        #region Reload

        public void CancelReload()
        {
            if (currentState.ReloadTimer <= 0f) return;
            currentState.ReloadTimer = 0f;
            viewModel?.SetReloadState(false);
            viewModel?.SetReloadProgress(0f);
        }

        public void CancelReloadAndRefill()
        {
            if (!isOwner) return;
            if (playerEquip == null) return;
            CancelReload();
            int slot = playerEquip.currentState.CurrentSlot;
            if (slot == 0)
                currentState.AmmoSlot0 = weaponStatManager.MagazineSize;
            else
                currentState.AmmoSlot1 = weaponStatManager.MagazineSize;
            viewModel?.SetAmmo(CurrentAmmo, MagazineSize);
        }

        #endregion

        #region Weapon Refresh

        public void RefreshWeaponStats()
        {
            if (playerEquip == null || weaponStatManager == null) return;
            int slot = playerEquip.currentState.CurrentSlot;
            int magazineSize = weaponStatManager.MagazineSize;
            if (magazineSize > 0)
            {
                if (slot == 0)
                    currentState.AmmoSlot0 = Mathf.Min(currentState.AmmoSlot0 == 0 ? magazineSize : currentState.AmmoSlot0, magazineSize);
                else
                    currentState.AmmoSlot1 = Mathf.Min(currentState.AmmoSlot1 == 0 ? magazineSize : currentState.AmmoSlot1, magazineSize);
            }
            viewModel?.SetAmmo(CurrentAmmo, MagazineSize);
        }

        #endregion

        #region Aim

        private Vector3 GetAimDirection(Transform muzzle)
        {
            if (playerCamera == null) return muzzle.forward;
            return playerCamera.transform.forward.normalized;
        }

        private Vector3 GetProjectileAimDirection(Transform muzzle)
        {
            if (playerCamera == null) return muzzle.forward;

            Vector3 cameraOrigin = playerCamera.transform.position;
            Vector3 cameraForward = playerCamera.transform.forward;

            Vector3 targetPoint;
            if (Physics.Raycast(cameraOrigin, cameraForward, out RaycastHit hit, weaponStatManager.Range, hitscanLayerMask, QueryTriggerInteraction.Ignore))
                targetPoint = hit.point;
            else
                targetPoint = cameraOrigin + cameraForward * weaponStatManager.Range;

            return (targetPoint - muzzle.position).normalized;
        }

        private Vector3 ApplySpread(Vector3 dir, float spreadDegrees)
        {
            if (spreadDegrees <= 0f) return dir;

            float yaw = Random.Range(-spreadDegrees, spreadDegrees);
            float pitch = Random.Range(-spreadDegrees, spreadDegrees);

            Vector3 result = Quaternion.Euler(pitch, yaw, 0f) * dir;
            if (result.sqrMagnitude < 0.0001f) return dir;

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
                return weaponStatManager != null ? weaponStatManager.MagazineSize : 0;
            }
        }

        public float ReloadProgress01
        {
            get
            {
                if (currentState.ReloadTimer <= 0f) return 0f;
                WeaponProperties weapon = playerEquip?.EquippedWeapon;
                AnimationClip clip = currentState.IsEmptyReload ? weapon?.EmptyReloadClip : weapon?.ReloadClip;
                float reloadDuration = clip != null ? clip.length : (weaponStatManager != null ? weaponStatManager.ReloadTime : 0f);
                if (reloadDuration <= 0f) return 0f;
                return Mathf.Clamp01(1f - (currentState.ReloadTimer / reloadDuration));
            }
        }

        public float ReloadDuration
        {
            get
            {
                if (playerEquip == null) return 0f;
                WeaponProperties weapon = playerEquip.EquippedWeapon;
                if (weapon == null) return 0f;
                AnimationClip clip = currentState.IsEmptyReload ? weapon.EmptyReloadClip : weapon.ReloadClip;
                return clip != null ? clip.length : (weaponStatManager != null ? weaponStatManager.ReloadTime : 0f);
            }
        }

        #endregion
    }
}
