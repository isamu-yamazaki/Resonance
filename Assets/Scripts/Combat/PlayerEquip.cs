using System;
using System.Linq;
using PurrNet.Prediction;
using Resonance.Audio;
using Resonance.Combat.Augments;
using Resonance.Combat.Weapons;
using Resonance.Combat.Weapons.Enums;
using Resonance.Helper;
using Resonance.Inventory;
using Resonance.Player;
using Resonance.PlayerController;
using UnityEngine;

namespace Resonance.Combat
{
    /// <summary>
    /// Manage authoritative state about the current weapon slot,
    /// and orchestrate weapon equips based on dependency data.
    /// </summary>
    [DefaultExecutionOrder(-1)]
    [RequireComponent(typeof(PlayerSkinRenderer))]
    [RequireComponent(typeof(PlayerState))]
    [RequireComponent(typeof(FPArmsAnimator))]
    [RequireComponent(typeof(PlayerStats))]
    [RequireComponent(typeof(PlayerAugmentEquipper))]
    [RequireComponent(typeof(PlayerAbilityManager))]
    [RequireComponent(typeof(WeaponStatManager))]
    public class PlayerEquip : PredictedIdentity<PlayerEquipInputData, PlayerEquipDataState>
    {
        private PlayerStats _playerStats;
        private PlayerSkinRenderer _playerSkinRenderer;
        private WeaponStatManager _weaponStatManager;
        private PlayerAugmentEquipper _playerAugmentEquipper;
        private PlayerAbilityManager _playerAbilityManager;
        private FPArmsAnimator _fpArmsAnimator;

        private ObservableValue<WeaponProperties> _equippedWeaponObservable = new ObservableValue<WeaponProperties>();
        public ObservableValue<WeaponProperties> EquippedWeaponObservable => _equippedWeaponObservable;

        [SerializeField] private PlayerInventory playerInventory;
        private PlayerActionsInput _playerActionsInput;
        private PlayerState _playerState;

        public WeaponView CurrentWeaponView
        {
            get
            {
                if (EquippedWeapon == null) return null;

                var skinInstance = _playerSkinRenderer.CurrentMeshInstance;
                if (skinInstance == null) return null;

                var allViews = skinInstance.GetComponentsInChildren<WeaponView>(true);
                return allViews.FirstOrDefault(v => v.WeaponKey == EquippedWeapon.WeaponMuzzleKey);
            }
        }

        public WeaponProperties EquippedWeapon => playerInventory.WeaponInventory[currentState.CurrentSlot];

        private const int StartingSlot = 1;
        private bool _isInitialEquip = true;
        private int _lastViewedSlot = int.MinValue;
        private string _pendingWeaponKeyToEquip;
        private string _pendingWeaponIdToEquip;

        // Last TP skin mesh instance we refreshed the TP weapon for. A Unity-side artifact
        // (not deterministic state), only ever reassigned on a verified tick, so comparing it
        // against the renderer's CurrentMeshInstance reproduces the old OnNewSkinSpawned edge.
        private GameObject _lastTpRefreshedSkinInstance;

        #region Lifecycle

        protected override void LateAwake()
        {
            _playerSkinRenderer = GetComponent<PlayerSkinRenderer>();
            _playerState = GetComponent<PlayerState>();
            _fpArmsAnimator = GetComponent<FPArmsAnimator>();
            _playerStats = GetComponent<PlayerStats>();
            _playerAugmentEquipper = GetComponent<PlayerAugmentEquipper>();
            _playerAbilityManager = GetComponent<PlayerAbilityManager>();
            _weaponStatManager = GetComponent<WeaponStatManager>();
            _playerActionsInput = PlayerActionsInput.Instance;
        }

        protected override PlayerEquipDataState GetInitialState()
        {
            return new PlayerEquipDataState { CurrentSlot = StartingSlot };
        }

        #endregion

        #region Input

        protected override void UpdateInput(ref PlayerEquipInputData input)
        {
            if (_playerActionsInput == null) return;

            if (_playerActionsInput.SwapWeaponPressed)
            {
                input.SwapWeaponPressed = true;
                _playerActionsInput.SetSwapWeaponPressedFalse();
            }

            if (_playerActionsInput.SwapSlotOnePressed)
            {
                input.SwapSlotOnePressed = true;
                _playerActionsInput.SetSlotOnePressedFalse();
            }

            if (_playerActionsInput.SwapSlotTwoPressed)
            {
                input.SwapSlotTwoPressed = true;
                _playerActionsInput.SetSlotTwoPressedFalse();
            }
        }

        #endregion

        #region Simulation

        protected override void Simulate(PlayerEquipInputData input, ref PlayerEquipDataState state, float delta)
        {
            // slot transition
            state.LastSlot = state.CurrentSlot;
            if (input.SwapWeaponPressed)
                state.CurrentSlot = state.CurrentSlot == 0 ? 1 : 0;
            else if (input.SwapSlotOnePressed)
                state.CurrentSlot = 0;
            else if (input.SwapSlotTwoPressed)
                state.CurrentSlot = 1;

            var weapon = playerInventory.WeaponInventory[state.CurrentSlot];
            if (weapon == null && state.LastEquippedWeapon != null && state.LastSlot == state.CurrentSlot)
            {
                SimulateOrchestrateRemoveWeapon(ref state);
            }
            else
            {
                SimulateOrchestrateEquipWeapon(ref state);
            }

            SimulateOrchestrateEquipAugment(ref state);

            if (!predictionManager.isVerified) return;

            // PlayerSkinRenderer (exec order -2) applies the new skin earlier this tick;
            // detect the fresh mesh instance by reference and refresh the TP weapon view.
            // Gated on isVerified so this side effect stays off predicted resim ticks.
            var skinInstance = _playerSkinRenderer.CurrentMeshInstance;
            if (skinInstance == _lastTpRefreshedSkinInstance) return;

            _lastTpRefreshedSkinInstance = skinInstance;
            if (skinInstance != null && EquippedWeapon != null)
                SimulateTpWeaponRefresh(skinInstance);
        }


        [SimulationOnly]
        private void SimulateOrchestrateEquipWeapon(ref PlayerEquipDataState state)
        {
            var weapon = playerInventory.WeaponInventory[state.CurrentSlot];
            if (weapon == null)
            {
                return;
            }

            var weaponIdentity = WeaponIdentity.FromWeaponProperties(weapon);

            // detect any weapon switch, including slot switches
            if (state.LastEquippedWeapon != weaponIdentity)
            {
                _playerState?.SetSimulatedWeaponClass(weapon.Class);

                // refresh magazine size reported in PlayerShooter
                if (_weaponStatManager != null)
                {
                    _weaponStatManager.SetWeaponPropertiesToManage(weapon);
                }

                if (_equippedWeaponObservable != null)
                {
                    _equippedWeaponObservable.Value = weapon;
                }

                if (_playerStats != null)
                {
                    if (state.LastEquippedWeapon.HasValue)
                    {
                        var lastEquippedWeapon = state.LastEquippedWeapon.Value;
                        var baseWeapon = playerInventory.FindWeaponByKey(lastEquippedWeapon.Key);
                        var mobilityToRemove = baseWeapon.Mobility;
                        _playerStats.SimulateRemoveSpeedModifier(mobilityToRemove);
                    }
                    _playerStats.SimulateAddSpeedModifier(_weaponStatManager.Mobility);
                }
            }


            state.LastEquippedWeapon = weaponIdentity;
        }

        [SimulationOnly]
        private void SimulateOrchestrateRemoveWeapon(ref PlayerEquipDataState state)
        {
            // we're just accounting for the case where the weapon in the current slot disappears
            var currentWeapon = playerInventory.WeaponInventory[state.CurrentSlot];
            if (currentWeapon != null || state.LastEquippedWeapon == null ||
                state.LastSlot != state.CurrentSlot) return;

            var baseLastWeapon = playerInventory.FindWeaponByKey(state.LastEquippedWeapon.Value.Key);
            if (_playerStats != null)
            {
                _playerStats.SimulateRemoveSpeedModifier(baseLastWeapon.Mobility);
            }

            if (_weaponStatManager != null)
            {
                _weaponStatManager.SetWeaponPropertiesToManage(null);
            }

            if (_equippedWeaponObservable != null)
            {
                _equippedWeaponObservable.Value = null;
            }


            state.LastEquippedWeapon = null;
        }

        [SimulationOnly]
        private void SimulateOrchestrateEquipAugment(ref PlayerEquipDataState state)
        {
            // Augment auth state lives in PlayerInventory; diff each slot against the last-equipped
            // key (carried in predicted state, so this is resim-safe) and emit side effects on edges.
            var augments = playerInventory.AugmentInventory;

            state.LastEquippedUpperAugmentKey =
                SimulateOrchestrateAugmentSlot(augments[0], state.LastEquippedUpperAugmentKey);
            state.LastEquippedLowerAugmentKey =
                SimulateOrchestrateAugmentSlot(augments[1], state.LastEquippedLowerAugmentKey);
        }

        // Diffs one augment slot against its last-equipped key, applying remove/apply stat and
        // ability side effects on equip/swap/remove edges. Returns the new last-equipped key.
        private string SimulateOrchestrateAugmentSlot(AugmentProperties current, string lastKey)
        {
            string currentKey = current?.Key;
            if (currentKey == lastKey) return lastKey;

            if (!string.IsNullOrEmpty(lastKey))
            {
                var previous = playerInventory.FindAugmentByKey(lastKey);
                if (previous != null)
                {
                    _playerAugmentEquipper?.RemoveAugmentStats(previous);
                    _playerAbilityManager?.OnAugmentRemoved(previous);
                }
            }

            if (current != null)
            {
                _playerAugmentEquipper?.ApplyAugmentStats(current);
                _playerAbilityManager?.OnAugmentEquipped(current);
            }

            return currentKey;
        }


        [SimulationOnly]
        private void SimulateTpWeaponRefresh(GameObject skinInstance)
        {
            if (skinInstance == null) return;


            var allMeshes = skinInstance.GetComponentsInChildren<TPWeaponMesh>(true);

            foreach (var mesh in allMeshes)
            {
                mesh.gameObject.SetActive(false);
            }

            WeaponClass classToShow = EquippedWeapon.Class;
            if (classToShow != WeaponClass.Pistol && classToShow != WeaponClass.Sword)
            {
                classToShow = WeaponClass.Rifle;
            }

            foreach (var mesh in allMeshes)
            {
                if (mesh.weaponClass != classToShow) continue;
                mesh.gameObject.SetActive(true);
                break;
            }


            if (CurrentWeaponView == null)
            {
                Debug.LogWarning($"[PlayerEquip] No WeaponView found for key: {EquippedWeapon.WeaponMuzzleKey}", this);
                return;
            }

            MuzzleFlashSettings flashSettings = _weaponStatManager?.GetMuzzleFlashSettings();
            if (flashSettings != null)
            {
                CurrentWeaponView.ApplyMuzzleFlashSettings(flashSettings);
            }

            WeaponAudioProperties audioProperties = _weaponStatManager?.GetAudioProperties();
            if (audioProperties != null)
            {
                CurrentWeaponView.ApplyAudioProperties(audioProperties);
            }
        }

        #endregion


        #region Local view update

        protected override PlayerEquipDataState Interpolate(
            PlayerEquipDataState from,
            PlayerEquipDataState to,
            float t)
        {
            return to;
        }

        protected override void UpdateView(PlayerEquipDataState viewState, PlayerEquipDataState? verified)
        {
            if (_lastViewedSlot == viewState.CurrentSlot) return;
            if (playerInventory == null || playerInventory.WeaponInventory == null) return;
            if (viewState.CurrentSlot < 0 || viewState.CurrentSlot >= playerInventory.WeaponInventory.Length) return;

            WeaponProperties weapon = playerInventory.WeaponInventory[viewState.CurrentSlot];
            if (weapon == null) return;

            _lastViewedSlot = viewState.CurrentSlot;

            HideTpMeshIfOwner();
            RequestFpWeaponSwapIfOwner(weapon);

            if (!_isInitialEquip)
            {
                PlayEquipEffects();
            }

            _isInitialEquip = false;
        }

        private void HideTpMeshIfOwner()
        {
            var skinInstance = _playerSkinRenderer.CurrentMeshInstance;
            var allMeshes = skinInstance.GetComponentsInChildren<TPWeaponMesh>(true);
            WeaponClass? classToShow = EquippedWeapon?.Class;

            if (!classToShow.HasValue) return;

            foreach (var mesh in allMeshes)
            {
                if (mesh.weaponClass != classToShow) continue;
                mesh.gameObject.SetActive(false);
                break;
            }
        }

        private void RequestFpWeaponSwapIfOwner(WeaponProperties weapon)
        {
            if (isOwner)
            {
                if (_fpArmsAnimator != null)
                {
                    _fpArmsAnimator.RequestWeaponSwap(weapon);
                }
            }
        }

        private void PlayEquipEffects()
        {
            CurrentWeaponView?.PlayEquip();

#if !UNITY_SERVER
            if (AudioSourceTracker.Instance != null)
            {
                AudioSourceTracker.Instance.RegisterSound(transform.position, 1f);
            }
#endif
        }

        #endregion
    }
}