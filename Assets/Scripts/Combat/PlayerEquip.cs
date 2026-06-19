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
        private PlayerStats playerStats;
        private PlayerSkinRenderer playerSkinRenderer;
        private WeaponStatManager weaponStatManager;
        private PlayerAugmentEquipper playerAugmentEquipper;
        private PlayerAbilityManager playerAbilityManager;
        private FPArmsAnimator fpArmsAnimator;

        private ObservableValue<WeaponProperties> equippedWeaponObservable = new ObservableValue<WeaponProperties>();
        public ObservableValue<WeaponProperties> EquippedWeaponObservable => equippedWeaponObservable;

        [SerializeField] private PlayerInventory playerInventory;
        private PlayerActionsInput playerActionsInput;
        private PlayerState playerState;

        // TODO: move to properly simulated state (somewhere, maybe not in this class)
        private WeaponView currentWeaponView;
        public WeaponView CurrentWeaponView => currentWeaponView;

        private WeaponProperties[] _weapons;
        public WeaponProperties EquippedWeapon => playerInventory.WeaponInventory[currentState.CurrentSlot];

        private const int StartingSlot = 1;
        private bool _isInitialEquip = true;
        private int _lastViewedSlot = int.MinValue;
        private string _pendingWeaponKeyToEquip;
        private bool _pendingPrimaryWeaponSlotRemoval;
        private bool _pendingSecondaryWeaponSlotRemoval;
        private bool _pendingUpperAugmentRemoval;
        private bool _pendingLowerAugmentRemoval;
        private string _pendingWeaponIdToEquip;

        #region Lifecycle

        protected override void LateAwake()
        {
            playerSkinRenderer = GetComponent<PlayerSkinRenderer>();
            playerSkinRenderer.OnNewSkinSpawned += OnNewSkinSpawned;
            _weapons = Resources.LoadAll<WeaponProperties>("Content/Weapons");
            playerState = GetComponent<PlayerState>();
            fpArmsAnimator = GetComponent<FPArmsAnimator>();
            playerStats = GetComponent<PlayerStats>();
            playerAugmentEquipper = GetComponent<PlayerAugmentEquipper>();
            playerAbilityManager = GetComponent<PlayerAbilityManager>();
            weaponStatManager = GetComponent<WeaponStatManager>();
            playerActionsInput = PlayerActionsInput.Instance;
        }

        protected override PlayerEquipDataState GetInitialState()
        {
            return new PlayerEquipDataState { CurrentSlot = StartingSlot };
        }

        #endregion

        #region Input

        protected override void UpdateInput(ref PlayerEquipInputData input)
        {
            if (playerActionsInput == null) return;

            if (playerActionsInput.SwapWeaponPressed)
            {
                input.SwapWeaponPressed = true;
                playerActionsInput.SetSwapWeaponPressedFalse();
            }

            if (playerActionsInput.SwapSlotOnePressed)
            {
                input.SwapSlotOnePressed = true;
                playerActionsInput.SetSlotOnePressedFalse();
            }

            if (playerActionsInput.SwapSlotTwoPressed)
            {
                input.SwapSlotTwoPressed = true;
                playerActionsInput.SetSlotTwoPressedFalse();
            }

            if (_pendingWeaponKeyToEquip != null
                && !string.IsNullOrEmpty(_pendingWeaponIdToEquip))
            {
                input.WeaponKeyToEquip = _pendingWeaponKeyToEquip;
                input.WeaponIdToEquip = _pendingWeaponIdToEquip;
                _pendingWeaponKeyToEquip = null;
                _pendingWeaponIdToEquip = null;
            }

            if (_pendingPrimaryWeaponSlotRemoval)
            {
                input.PendingPrimaryWeaponSlotRemoval = true;
                _pendingPrimaryWeaponSlotRemoval = false;
            }

            if (_pendingSecondaryWeaponSlotRemoval)
            {
                input.PendingSecondaryWeaponSlotRemoval = true;
                _pendingSecondaryWeaponSlotRemoval = false;
            }

            if (_pendingUpperAugmentRemoval)
            {
                input.PendingUpperAugmentRemoval = true;
                _pendingUpperAugmentRemoval = false;
            }

            if (_pendingLowerAugmentRemoval)
            {
                input.PendingLowerAugmentRemoval = true;
                _pendingLowerAugmentRemoval = false;
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

            if (input.PendingPrimaryWeaponSlotRemoval)
            {
                playerInventory.RemoveWeapon(WeaponSlot.Primary);
            }

            if (input.PendingSecondaryWeaponSlotRemoval)
            {
                playerInventory.RemoveWeapon(WeaponSlot.Secondary);
            }

            if (input.PendingUpperAugmentRemoval)
            {
                playerInventory.RemoveAugment(AugmentSlot.Upper);
            }

            if (input.PendingLowerAugmentRemoval)
            {
                playerInventory.RemoveAugment(AugmentSlot.Lower);
            }

            SimulateOrchestrateEquipWeapon(ref state);
            // SimulateWeaponViewUpdate();
        }

        [SimulationOnly]
        private void SimulateWeaponViewUpdate()
        {
            if (EquippedWeapon == null) return;

            var skinInstance = playerSkinRenderer.CurrentMeshInstance;
            if (skinInstance == null) return;

            var allViews = skinInstance.GetComponentsInChildren<WeaponView>(true);
            currentWeaponView = allViews.FirstOrDefault(v => v.WeaponKey == EquippedWeapon.WeaponMuzzleKey);
        }

        [SimulationOnly]
        private void SimulateOrchestrateEquipWeapon(ref PlayerEquipDataState state)
        {
            var weapon = playerInventory.WeaponInventory[state.CurrentSlot];
            if (weapon == null) return;

            var weaponIdentity = WeaponIdentity.FromWeaponProperties(weapon);
            if (!weaponIdentity.HasValue) return;

            // detect any weapon switch, including slot switches
            if (state.LastEquippedWeapon != weaponIdentity)
            {
                if (weapon != null)
                {
                    // TODO: add simulation variant
                    playerState?.SetWeaponClass(weapon.Class);

                    // refresh magazine size reported in PlayerShooter
                    if (weaponStatManager != null)
                    {
                        weaponStatManager.SetWeaponPropertiesToManage(weapon);
                    }

                    if (equippedWeaponObservable != null)
                    {
                        equippedWeaponObservable.Value = weapon;
                    }

                    // TODO: add simulation variant
                    if (playerStats != null)
                    {
                        playerStats.AddSpeedModifierExternal(weaponStatManager.Mobility);
                    }
                }
            }

            state.LastEquippedWeapon = weaponIdentity;
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

            if (isOwner)
            {
                if (fpArmsAnimator != null)
                {
                    fpArmsAnimator.RequestWeaponSwap(weapon);
                }
            }

            if (playerSkinRenderer.CurrentMeshInstance != null)
            {
                RefreshTPWeaponView(playerSkinRenderer.CurrentMeshInstance);
            }

            if (!_isInitialEquip)
            {
                PlayEquipEffects();
            }

            _isInitialEquip = false;
        }

        private void OnNewSkinSpawned(GameObject skinInstance)
        {
            if (EquippedWeapon != null)
                RefreshTPWeaponView(skinInstance);
        }

        private void RefreshTPWeaponView(GameObject skinInstance)
        {
            if (skinInstance == null) return;

            var allMeshes = skinInstance.GetComponentsInChildren<TPWeaponMesh>(true);

            foreach (var mesh in allMeshes)
            {
                mesh.gameObject.SetActive(false);
            }

            if (EquippedWeapon == null)
            {
                currentWeaponView = null;
                return;
            }

            WeaponClass classToShow = EquippedWeapon.Class;
            if (classToShow != WeaponClass.Pistol && classToShow != WeaponClass.Sword)
            {
                classToShow = WeaponClass.Rifle;
            }

            foreach (var mesh in allMeshes)
            {
                if (mesh.weaponClass == classToShow)
                {
                    if (!playerSkinRenderer.IsTPHidden)
                    {
                        mesh.gameObject.SetActive(true);
                    }

                    break;
                }
            }


            if (currentWeaponView == null)
            {
                Debug.LogWarning($"[PlayerEquip] No WeaponView found for key: {EquippedWeapon.WeaponMuzzleKey}", this);
                return;
            }

            MuzzleFlashSettings flashSettings = weaponStatManager?.GetMuzzleFlashSettings();
            if (flashSettings != null)
            {
                currentWeaponView.ApplyMuzzleFlashSettings(flashSettings);
            }

            WeaponAudioProperties audioProperties = weaponStatManager?.GetAudioProperties();
            if (audioProperties != null)
            {
                currentWeaponView.ApplyAudioProperties(audioProperties);
            }
        }

        private void PlayEquipEffects()
        {
            currentWeaponView?.PlayEquip();

#if !UNITY_SERVER
            if (AudioSourceTracker.Instance != null)
            {
                AudioSourceTracker.Instance.RegisterSound(transform.position, 1f);
            }
#endif
        }

        public void RemoveWeapon(WeaponSlot slot)
        {
            // note that this is a non-simulation path

            WeaponProperties existing = slot == WeaponSlot.Primary
                ? playerInventory.WeaponInventory[0]
                : playerInventory.WeaponInventory[1];

            if (existing == null) return;

            if (EquippedWeapon == existing)
            {
                if (playerStats != null)
                {
                    playerStats.RemoveSpeedModifierExternal(existing.Mobility);
                }

                if (weaponStatManager != null)
                {
                    weaponStatManager.SetWeaponToManageExternal(null);
                }

                if (equippedWeaponObservable != null)
                {
                    equippedWeaponObservable.Value = null;
                }

                currentWeaponView = null;

                if (playerSkinRenderer.CurrentMeshInstance != null)
                {
                    RefreshTPWeaponView(playerSkinRenderer.CurrentMeshInstance);
                }
            }

            if (slot == WeaponSlot.Primary)
            {
                _pendingPrimaryWeaponSlotRemoval = true;
            }
            else
            {
                _pendingSecondaryWeaponSlotRemoval = true;
            }
        }

        public void EquipAugment(AugmentProperties augment)
        {
            if (augment == null || playerAugmentEquipper == null) return;

            switch (augment.Slot)
            {
                case AugmentSlot.Upper:
                    if (playerInventory.augmentInventory[0] != null)
                    {
                        RemoveAugmentExternal(playerInventory.augmentInventory[0]);
                    }

                    playerInventory.AddAugment(augment);
                    playerAugmentEquipper.ApplyAugmentStats(augment);
                    playerAbilityManager.OnAugmentEquipped(augment);
                    break;
                case AugmentSlot.Lower:
                    if (playerInventory.augmentInventory[1] != null)
                    {
                        RemoveAugmentExternal(playerInventory.augmentInventory[1]);
                    }

                    playerInventory.AddAugment(augment);
                    playerAugmentEquipper.ApplyAugmentStats(augment);
                    playerAbilityManager.OnAugmentEquipped(augment);
                    break;
            }
        }

        public void RemoveAugmentExternal(AugmentProperties augment)
        {
            playerAbilityManager.OnAugmentRemoved(augment);
            playerAugmentEquipper.RemoveAugmentStats(augment);
            if (augment.Slot == AugmentSlot.Upper)
            {
                _pendingUpperAugmentRemoval = true;
            }
            else
            {
                _pendingLowerAugmentRemoval = true;
            }
        }

        #endregion
    }
}