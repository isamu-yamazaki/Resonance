using System.Collections;
using Resonance.Assemblies.Player;
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

            // The outgoing weapon is whatever is genuinely shown right now. Resolve it from the
            // active instance, NOT the predicted weapon class — by the time this runs on the view
            // tick the predicted class has already flipped to the incoming weapon.
            Animator outgoing = GetActiveAnimator();
            bool canHolster = outgoing != null && outgoing.gameObject.activeInHierarchy;

            if (isHolstered)
            {
                // This class was previously holstered: bring it straight back, no holster step.
                _holsteredClasses.Remove(bucketed);
                DrawIncomingImmediately(bucketed, isNew);
            }
            else if (!canHolster)
            {
                // Nothing to holster (e.g. first equip): show the incoming arms and draw at once.
                DrawIncomingImmediately(bucketed, isNew);
            }
            else
            {
                // Normal swap: holster the currently-shown weapon. The incoming arms are activated
                // and drawn in OnHolsterCompleteRoutine. Suppress the verified-class RefreshArms so
                // it can't deactivate the outgoing instance out from under the holster animation.
                _hasPendingSwap = true;
                _fpArmsManager.SuppressNextRefresh();
                _playerState.SetExternalWeaponState(WeaponState.Holstering);
                TriggerOnActiveAnimator(IsHolsterHash);
            }
        }

        // Activates the incoming arms now and plays its draw (or first-buy) animation with no
        // holster step. Used on first equip and when re-drawing a previously-holstered class.
        private void DrawIncomingImmediately(WeaponClass bucketed, bool isNew)
        {
            _hasPendingSwap = false;
            _pendingWeapon = null;
            _pendingFirstBuy = false;
            _fpArmsManager.ShowClass(bucketed);
            TriggerOnActiveAnimator(isNew ? IsFirstBuyHash : IsDrawHash);
            _playerState.SetExternalWeaponState(WeaponState.Drawing);
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
                active.SetFloat(FireSpeedHash, PlayerActionsInput.Instance.AttackHeld ? weapon.FireClip.length * fireRate : 1f);
            }

            if (PlayerActionsInput.Instance.OverdrivePressed)
            {
                RequestOverdriveActivation();
                PlayerActionsInput.Instance.SetOverdrivePressedFales();
            }
            if (PlayerActionsInput.Instance.StimPressed)
            {
                RequestStimActivation();
                PlayerActionsInput.Instance.SetStimPressedFalse();
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

            _playerState.SetExternalWeaponState(skillState);

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

            // Outgoing weapon has finished holstering: activate the incoming arms now, then draw.
            _fpArmsManager.ShowClass(bucketed);

            yield return null;

            TriggerOnActiveAnimator(_pendingFirstBuy ? IsFirstBuyHash : IsDrawHash);
            _pendingFirstBuy = false;
            _pendingWeapon = null;
            _playerState.SetExternalWeaponState(WeaponState.Drawing);
        }

        public void OnDrawComplete()
        {
            _playerState.SetExternalWeaponState(WeaponState.Idle);
        }

        public void TriggerFirstBuy()
        {
            _holsteredClasses.Remove(BucketClass(_playerState.CurrentWeaponClass));
            TriggerOnActiveAnimator(IsFirstBuyHash);
            _playerState.SetExternalWeaponState(WeaponState.Drawing);
        }

        public void TriggerDraw()
        {
            _holsteredClasses.Remove(BucketClass(_playerState.CurrentWeaponClass));
            TriggerOnActiveAnimator(IsDrawHash);
            _playerState.SetExternalWeaponState(WeaponState.Drawing);
        }

        private void TriggerOnActiveAnimator(int hash)
        {
            Animator active = GetActiveAnimator();
            if (active == null)
            {
                return;
            }
            active.SetTrigger(hash);
        }

        // The active animator is the genuinely-shown FP-arms instance (owned by FPArmsManager), NOT
        // the one implied by the predicted weapon class. Those diverge during a swap: the predicted
        // class can point at an instance that has not been SetActive(true) yet, and a one-shot
        // SetTrigger on an inactive Animator is silently lost.
        private Animator GetActiveAnimator()
        {
            return _fpArmsManager != null ? _fpArmsManager.GetShownAnimator() : null;
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
            _playerState.SetExternalWeaponState(WeaponState.Idle);
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
            _playerState.SetExternalWeaponState(WeaponState.Holstering);
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
            _playerState.SetExternalWeaponState(WeaponState.Holstering);
            TriggerOnActiveAnimator(IsHolsterHash);
        }

        private IEnumerator SkillActivationRoutine(WeaponState skillState)
        {
            _playerState.SetExternalWeaponState(WeaponState.Holstering);
            TriggerOnActiveAnimator(IsHolsterHash);

            while (_playerState.CurrentWeaponState == WeaponState.Holstering)
                yield return null;

            _skillArmsInstance = _skinRenderer.SkillArmsInstance;
            if (_skillArmsInstance == null) yield break;

            _skillArmsInstance.SetActive(true);
            _playerState.SetExternalWeaponState(skillState);

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

            _fpArmsManager.RefreshArmsForCurrentWeaponInPlayerState();

            yield return null;

            TriggerOnActiveAnimator(IsDrawHash);
            _playerState.SetExternalWeaponState(WeaponState.Drawing);
        }
        
        public void RequestGrappleActivation()
        {
            if (_grappleHook == null || !_grappleHook.AbilityReady) return;
            if (_playerState.CurrentWeaponState != WeaponState.Idle) return;
            if (!_grappleHook.CanGrapple()) return;

            _pendingSkillState = WeaponState.Grappling;
            _playerState.SetExternalWeaponState(WeaponState.Holstering);
            TriggerOnActiveAnimator(IsHolsterHash);
        }

        public void OnGrappleFireHook()
        {
            _grappleHook?.ActivateAbilityExternal();
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

            _fpArmsManager.RefreshArmsForCurrentWeaponInPlayerState();

            yield return null;

            TriggerOnActiveAnimator(IsDrawHash);
            _playerState.SetExternalWeaponState(WeaponState.Drawing);
        }

        public void TriggerGrappleEnd()
        {
            if (_activeSkillArms == null) return;
            Animator grappleAnimator = _activeSkillArms.GetComponent<Animator>();
            grappleAnimator?.SetTrigger(Animator.StringToHash("GrappleEnd"));
        }
    }
}
