using Resonance.Combat.Weapons.Enums;
using Resonance.PlayerController;
using System.Collections.Generic;
using Resonance.Combat.Weapons;
using UnityEngine;

namespace Resonance.Combat
{
    public class FPArmsAnimator : MonoBehaviour
    {
        private PlayerState _playerState;
        private PlayerShooter _playerShooter;
        private FPArmsManager _fpArmsManager;
        private PlayerSkinRenderer _skinRenderer;
        private PlayerActionsInput _playerActionsInput;

        private static readonly int IsRunningHash = Animator.StringToHash("isRunning");
        private static readonly int IsFireHash = Animator.StringToHash("isShooting");
        private static readonly int IsReloadingHash = Animator.StringToHash("isReloading");
        private static readonly int IsEmptyReloadHash = Animator.StringToHash("isEmptyReload");
        private static readonly int IsDrawHash = Animator.StringToHash("Draw");
        private static readonly int IsHolsterHash = Animator.StringToHash("Holster");
        private static readonly int IsFirstBuyHash = Animator.StringToHash("FirstBuy");
        private static readonly int FireSpeedHash = Animator.StringToHash("fireSpeed");

        private bool _pendingFirstBuy = false;
        private WeaponClass _pendingWeaponClass;
        private bool _hasPendingSwap = false;
        private HashSet<WeaponClass> _seenWeaponClasses = new HashSet<WeaponClass>();
        private HashSet<WeaponClass> _holsteredClasses = new HashSet<WeaponClass>();
        public Animator GetActiveAnimatorPublic() => GetActiveAnimator();

        private void Awake()
        {
            _playerState = GetComponent<PlayerState>();
            _playerShooter = GetComponent<PlayerShooter>();
            _fpArmsManager = GetComponent<FPArmsManager>();
            _skinRenderer = GetComponent<PlayerSkinRenderer>();
            _playerActionsInput = GetComponent<PlayerActionsInput>();

            _playerState.OnWeaponClassChanged += OnWeaponClassChanged;
        }

        private void OnDestroy()
        {
            _playerState.OnWeaponClassChanged -= OnWeaponClassChanged;
        }

        private void OnWeaponClassChanged(WeaponClass newClass)
        {
            WeaponClass bucketed = BucketClass(newClass);
            bool isNew = !_seenWeaponClasses.Contains(bucketed);
            bool isHolstered = _holsteredClasses.Contains(bucketed);
            _seenWeaponClasses.Add(bucketed);

            _pendingWeaponClass = newClass;
            _hasPendingSwap = true;
            _pendingFirstBuy = isNew;

            if (isHolstered)
            {
                _holsteredClasses.Remove(bucketed);
                _fpArmsManager.RefreshArms();
                TriggerOnActiveAnimator(isNew ? IsFirstBuyHash : IsDrawHash);
                _pendingFirstBuy = false;
                _hasPendingSwap = false;
                _playerState.SetWeaponState(WeaponState.Drawing);
            }
            else
            {
                _playerState.SetWeaponState(WeaponState.Holstering);
                TriggerOnActiveAnimator(IsHolsterHash);
            }
        }

        private void Update()
        {
            if (_playerState.CurrentWeaponState == WeaponState.Holstering ||
                _playerState.CurrentWeaponState == WeaponState.Drawing) return;

            Animator active = GetActiveAnimator();
            if (active == null) return;

            bool isMoving = _playerState.CurrentPlayerMovementState == PlayerMovementState.Running ||
                            _playerState.CurrentPlayerMovementState == PlayerMovementState.Sprinting;

            active.SetBool(IsRunningHash, isMoving);
            active.SetBool(IsReloadingHash, _playerState.IsReloading && _playerShooter.CurrentAmmo > 0);
            active.SetBool(IsEmptyReloadHash, _playerState.IsReloading && _playerShooter.CurrentAmmo <= 0);

            active.SetBool(IsFireHash, _playerState.IsAttacking);

            WeaponProperties weapon = GetComponent<PlayerEquip>()?.EquippedWeapon;
            if (weapon?.FireClip != null)
            {
                float fireRate = GetComponent<WeaponStatManager>()?.FireRate ?? 1f;
                active.SetFloat(FireSpeedHash, _playerActionsInput.AttackHeld ? weapon.FireClip.length * fireRate : 1f);
            }
        }

        public void OnHolsterComplete()
        {
            if (!_hasPendingSwap) return;

            WeaponClass bucketed = BucketClass(_pendingWeaponClass);
            _holsteredClasses.Add(bucketed);
            _hasPendingSwap = false;

            GetComponent<PlayerEquip>()?.ExecutePendingSwap();

            _fpArmsManager.RefreshArms();
            TriggerOnActiveAnimator(_pendingFirstBuy ? IsFirstBuyHash : IsDrawHash);
            _pendingFirstBuy = false;
            _playerState.SetWeaponState(WeaponState.Drawing);
        }

        public void OnDrawComplete()
        {
            _playerState.SetWeaponState(WeaponState.Idle);
        }

        public void TriggerFirstBuy()
        {
            Animator active = GetActiveAnimator();
            Debug.Log($"[FPArmsAnimator] TriggerFirstBuy - active animator: {active?.name ?? "NULL"}, weaponClass: {_playerState.CurrentWeaponClass}");
            _holsteredClasses.Remove(BucketClass(_playerState.CurrentWeaponClass));
            TriggerOnActiveAnimator(IsFirstBuyHash);
            _playerState.SetWeaponState(WeaponState.Drawing);
        }

        public void TriggerDraw()
        {
            _holsteredClasses.Remove(BucketClass(_playerState.CurrentWeaponClass));
            TriggerOnActiveAnimator(IsDrawHash);
            _playerState.SetWeaponState(WeaponState.Drawing);
        }

        private void TriggerOnActiveAnimator(int hash)
        {
            Animator active = GetActiveAnimator();
            if (active == null) return;
            active.SetTrigger(hash);
        }

        private Animator GetActiveAnimator()
        {
            if (_skinRenderer?.FPArmsInstances == null) return null;

            WeaponClass classToCheck = BucketClass(_playerState.CurrentWeaponClass);

            if (_skinRenderer.FPArmsInstances.TryGetValue(classToCheck, out GameObject instance))
                return instance?.GetComponent<Animator>();

            return null;
        }

        private WeaponClass BucketClass(WeaponClass weaponClass)
        {
            if (weaponClass == WeaponClass.Pistol || weaponClass == WeaponClass.Sword)
                return weaponClass;
            return WeaponClass.Rifle;
        }
        
        public void ResetForMatchStart()
        {
            _hasPendingSwap = false;
            _pendingFirstBuy = false;
            _seenWeaponClasses.Clear();
            _holsteredClasses.Clear();
            _playerState.SetWeaponState(WeaponState.Idle);
        }
    }
}