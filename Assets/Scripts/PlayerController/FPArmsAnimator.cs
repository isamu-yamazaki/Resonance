using System.Collections;
using Resonance.Combat.Weapons.Enums;
using Resonance.PlayerController;
using System.Collections.Generic;
using Resonance.Combat.Augments;
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
        private GameObject _skillArmsInstance;
        private OverdriveAbility _overdriveAbility;
        private PlayerHealthStim _playerHealthStim;
        private GameObject _activeSkillArms;
        private AbilityGrappleHook _grappleHook;
        
        private static readonly int IsRunningHash = Animator.StringToHash("isRunning");
        private static readonly int IsFireHash = Animator.StringToHash("isShooting");
        private static readonly int IsReloadingHash = Animator.StringToHash("isReloading");
        private static readonly int IsEmptyReloadHash = Animator.StringToHash("isEmptyReload");
        private static readonly int IsDrawHash = Animator.StringToHash("Draw");
        private static readonly int IsHolsterHash = Animator.StringToHash("Holster");
        private static readonly int IsFirstBuyHash = Animator.StringToHash("FirstBuy");
        private static readonly int FireSpeedHash = Animator.StringToHash("fireSpeed");

        private bool _pendingFirstBuy = false;
        private WeaponProperties _pendingWeapon = null;
        private bool _hasPendingSwap = false;
        private HashSet<WeaponClass> _seenWeaponClasses = new HashSet<WeaponClass>();
        private HashSet<WeaponClass> _holsteredClasses = new HashSet<WeaponClass>();
        private WeaponState _pendingSkillState = WeaponState.Idle;

        public Animator GetActiveAnimatorPublic() => GetActiveAnimator();

        private void Awake()
        {
            _playerState = GetComponent<PlayerState>();
            _playerShooter = GetComponent<PlayerShooter>();
            _fpArmsManager = GetComponent<FPArmsManager>();
            _skinRenderer = GetComponent<PlayerSkinRenderer>();
            _playerActionsInput = GetComponent<PlayerActionsInput>();
            _overdriveAbility = GetComponent<OverdriveAbility>();
            _playerHealthStim = GetComponent<PlayerHealthStim>();
            _grappleHook = GetComponent<AbilityGrappleHook>();
        }

        private void OnDestroy()
        {
            _playerState.OnWeaponClassChanged -= OnWeaponClassChanged;
        }

        public void RequestWeaponSwap(WeaponProperties weapon)
        {
            if (weapon == null) return;

            WeaponClass bucketed = BucketClass(weapon.Class);
            bool isNew = !_seenWeaponClasses.Contains(bucketed);
            bool isHolstered = _holsteredClasses.Contains(bucketed);
            _seenWeaponClasses.Add(bucketed);
            Debug.Log($"[FPArmsAnimator] RequestWeaponSwap - bucketed: {bucketed}, isNew: {isNew}, isHolstered: {isHolstered}, seen: {string.Join(",", _seenWeaponClasses)}, holstered: {string.Join(",", _holsteredClasses)}");

            _pendingWeapon = weapon;
            _pendingFirstBuy = isNew;
            _hasPendingSwap = true;

            if (isHolstered)
            {
                _holsteredClasses.Remove(bucketed);
                _hasPendingSwap = false;
                GetComponent<PlayerEquip>()?.ExecuteWeaponSwap(weapon);
                _fpArmsManager.RefreshArms();
                TriggerOnActiveAnimator(isNew ? IsFirstBuyHash : IsDrawHash);
                _pendingFirstBuy = false;
                _playerState.SetWeaponState(WeaponState.Drawing);
            }
            else
            {
                _playerState.SetWeaponState(WeaponState.Holstering);
                TriggerOnActiveAnimator(IsHolsterHash);
            }
        }

        private void OnWeaponClassChanged(WeaponClass newClass)
        {
        }

        private void Update()
        {
            if (_playerState.CurrentWeaponState == WeaponState.Holstering ||
                _playerState.CurrentWeaponState == WeaponState.Drawing) return;

            Animator active = GetActiveAnimator();
            if (active == null) return;

            
            bool isMoving = _playerState.CurrentPlayerMovementState == PlayerMovementState.Sprinting;

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
            if (_pendingSkillState != WeaponState.Idle)
            {
                StartCoroutine(ActivateSkillArmsRoutine(_pendingSkillState));
                _pendingSkillState = WeaponState.Idle;
                return;
            }

            if (!_hasPendingSwap) return;
            StartCoroutine(OnHolsterCompleteRoutine());
        }

        private IEnumerator ActivateSkillArmsRoutine(WeaponState skillState)
        {
            GameObject skillArms = skillState == WeaponState.Grappling ?
                _skinRenderer.GrappleArmsInstance :
                _skinRenderer.SkillArmsInstance;

            if (skillArms == null) yield break;

            skillArms.SetActive(true);
            _activeSkillArms = skillArms;
            _fpArmsManager.SuppressNextRefresh();

            foreach (var kvp in _skinRenderer.FPArmsInstances)
            {
                if (kvp.Value != null)
                    kvp.Value.SetActive(false);
            }

            _playerState.SetWeaponState(skillState);

            if (skillState != WeaponState.Grappling)
            {
                SkillArmsAnimationBridge bridge = skillArms.GetComponent<SkillArmsAnimationBridge>();
                if (skillState == WeaponState.Casting)
                {
                    bridge?.HideSyringe();
                }
                else
                {
                    bridge?.ShowSyringe();
                }
            }

            Animator skillAnimator = skillArms.GetComponent<Animator>();
            if (skillAnimator != null)
            {
                int hash = skillState == WeaponState.Casting ?
                    Animator.StringToHash("Overdrive") :
                    skillState == WeaponState.Stimming ?
                        Animator.StringToHash("Stim") :
                        Animator.StringToHash("GrappleShoot");

                yield return null;
                skillAnimator.SetTrigger(hash);
            }
        }

        private IEnumerator OnHolsterCompleteRoutine()
        {
            WeaponClass bucketed = BucketClass(_pendingWeapon.Class);
            _holsteredClasses.Add(bucketed);
            _hasPendingSwap = false;

            _fpArmsManager.SuppressNextRefresh();
            GetComponent<PlayerEquip>()?.ExecuteWeaponSwap(_pendingWeapon);

            _fpArmsManager.RefreshArms();

            yield return null;

            TriggerOnActiveAnimator(_pendingFirstBuy ? IsFirstBuyHash : IsDrawHash);
            _pendingFirstBuy = false;
            _pendingWeapon = null;
            _playerState.SetWeaponState(WeaponState.Drawing);
        }

        public void OnDrawComplete()
        {
            _playerState.SetWeaponState(WeaponState.Idle);
        }

        public void TriggerFirstBuy()
        {
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
            {
                return instance?.GetComponent<Animator>();
            }

            return null;
        }

        private WeaponClass BucketClass(WeaponClass weaponClass)
        {
            if (weaponClass == WeaponClass.Pistol || weaponClass == WeaponClass.Sword)
            {
                return weaponClass;
            }

            return WeaponClass.Rifle;
        }

        public void ResetForMatchStart()
        {
            _hasPendingSwap = false;
            _pendingFirstBuy = false;
            _pendingWeapon = null;
            _seenWeaponClasses.Clear();
            _holsteredClasses.Clear();
            _playerState.SetWeaponState(WeaponState.Idle);
        }
        
        public void RequestOverdriveActivation()
        {
            if (_overdriveAbility == null || !_overdriveAbility.IsReady) return;
            if (_playerState.CurrentWeaponState == WeaponState.Drawing) return;
            if (_playerState.CurrentWeaponState == WeaponState.Holstering) return;
            if (_playerState.CurrentWeaponState == WeaponState.Reloading) return;
            if (_playerState.CurrentWeaponState == WeaponState.EmptyReloading) return;
            if (_playerState.CurrentWeaponState == WeaponState.Casting) return;
            if (_playerState.CurrentWeaponState == WeaponState.Stimming) return;

            _pendingSkillState = WeaponState.Casting;
            _playerState.SetWeaponState(WeaponState.Holstering);
            TriggerOnActiveAnimator(IsHolsterHash);
        }

        public void RequestStimActivation()
        {
            if (_playerHealthStim == null || !_playerHealthStim.HasCharges) return;
            if (_playerState.CurrentWeaponState == WeaponState.Drawing) return;
            if (_playerState.CurrentWeaponState == WeaponState.Holstering) return;
            if (_playerState.CurrentWeaponState == WeaponState.Reloading) return;
            if (_playerState.CurrentWeaponState == WeaponState.EmptyReloading) return;
            if (_playerState.CurrentWeaponState == WeaponState.Casting) return;
            if (_playerState.CurrentWeaponState == WeaponState.Stimming) return;

            _pendingSkillState = WeaponState.Stimming;
            _playerState.SetWeaponState(WeaponState.Holstering);
            TriggerOnActiveAnimator(IsHolsterHash);
        }

        private IEnumerator SkillActivationRoutine(WeaponState skillState)
        {
            _playerState.SetWeaponState(WeaponState.Holstering);
            TriggerOnActiveAnimator(IsHolsterHash);

            while (_playerState.CurrentWeaponState == WeaponState.Holstering)
                yield return null;

            _skillArmsInstance = _skinRenderer.SkillArmsInstance;
            if (_skillArmsInstance == null) yield break;

            _skillArmsInstance.SetActive(true);
            _playerState.SetWeaponState(skillState);

            Animator skillAnimator = _skillArmsInstance.GetComponent<Animator>();
            if (skillAnimator != null)
            {
                int hash = skillState == WeaponState.Casting ?
                    Animator.StringToHash("Overdrive") :
                    Animator.StringToHash("Stim");
                skillAnimator.SetTrigger(hash);
            }
        }

        public void OnOverdriveAnimActivate()
        {
            _overdriveAbility?.TryActivateOverdrive();
        }
        
        public void OnStimAnimActivate()
        {
            _playerHealthStim?.ActivateStim();
        }

        public void OnSkillComplete()
        {
            StartCoroutine(OnSkillCompleteRoutine());
        }

        private IEnumerator OnSkillCompleteRoutine()
        {
            if (_activeSkillArms != null)
            {
                _activeSkillArms.SetActive(false);
                _activeSkillArms = null;
            }

            _fpArmsManager.RefreshArms();

            yield return null;

            TriggerOnActiveAnimator(IsDrawHash);
            _playerState.SetWeaponState(WeaponState.Drawing);
        }
        
        public void RequestGrappleActivation()
        {
            if (_grappleHook == null || !_grappleHook.AbilityReady) return;
            if (_playerState.CurrentWeaponState != WeaponState.Idle) return;
            if (!_grappleHook.CanGrapple()) return;

            _pendingSkillState = WeaponState.Grappling;
            _playerState.SetWeaponState(WeaponState.Holstering);
            TriggerOnActiveAnimator(IsHolsterHash);
        }

        public void OnGrappleFireHook()
        {
            _grappleHook?.ActivateAbility();
        }

        public void OnGrappleComplete()
        {
            StartCoroutine(OnGrappleCompleteRoutine());
        }

        private IEnumerator OnGrappleCompleteRoutine()
        {
            if (_activeSkillArms != null)
            {
                _activeSkillArms.SetActive(false);
                _activeSkillArms = null;
            }

            _fpArmsManager.RefreshArms();

            yield return null;

            TriggerOnActiveAnimator(IsDrawHash);
            _playerState.SetWeaponState(WeaponState.Drawing);
        }

        public void TriggerGrappleEnd()
        {
            if (_activeSkillArms == null) return;
            Animator grappleAnimator = _activeSkillArms.GetComponent<Animator>();
            grappleAnimator?.SetTrigger(Animator.StringToHash("GrappleEnd"));
        }
    }
}