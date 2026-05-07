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

        [SerializeField] PlayerInventory playerInventory;
        public PlayerInventory PlayerInventory => playerInventory;

        private PlayerActionsInput playerActionsInput;
        private PlayerState playerState;

        private WeaponView currentWeaponView;
        public WeaponView CurrentWeaponView => currentWeaponView;

        private WeaponProperties[] weapons;
        public WeaponProperties EquippedWeapon => System.Array.Find(weapons, w => w.Key == currentState.EquippedWeaponKey);

        private bool _isInitialEquip = true;
        private int _lastViewedSlot = int.MinValue;
        private const int StartingSlot = 1;
        private string pendingWeaponKeyToEquip;
        private bool pendingPrimaryWeaponSlotRemoval;
        private bool pendingSecondaryWeaponSlotRemoval;
        private bool pendingUpperAugmentRemoval;
        private bool pendingLowerAugmentRemoval;

        protected override void LateAwake()
        {
            playerSkinRenderer = GetComponent<PlayerSkinRenderer>();
            playerSkinRenderer.OnNewSkinSpawned += OnNewSkinSpawned;
            weapons = Resources.LoadAll<WeaponProperties>("Content/Weapons");
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

            if (pendingWeaponKeyToEquip != null)
            {
                input.WeaponKeyToEquip = pendingWeaponKeyToEquip;
                pendingWeaponKeyToEquip = null;
            }
            if (pendingPrimaryWeaponSlotRemoval)
            {
                input.PendingPrimaryWeaponSlotRemoval = true;
                pendingPrimaryWeaponSlotRemoval = false;
            }
            if (pendingSecondaryWeaponSlotRemoval)
            {
                input.PendingSecondaryWeaponSlotRemoval = true;
                pendingSecondaryWeaponSlotRemoval = false;
            }
            if (pendingUpperAugmentRemoval)
            {
                input.PendingUpperAugmentRemoval = true;
                pendingUpperAugmentRemoval = false;
            }
            if (pendingLowerAugmentRemoval)
            {
                input.PendingLowerAugmentRemoval = true;
                pendingLowerAugmentRemoval = false;
            }
        }

        protected override void Simulate(PlayerEquipInputData input, ref PlayerEquipDataState state, float delta)
        {
            // slot transition
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

            SimulateEquipWeapon(input, ref state);
        }

        [SimulationOnly]
        private void SimulateEquipWeapon(PlayerEquipInputData input, ref PlayerEquipDataState state)
        {
            if (input.WeaponKeyToEquip != null)
            {
                state.EquippedWeaponKey = input.WeaponKeyToEquip;

                var weapon = Array.Find(weapons, w => w.Key == input.WeaponKeyToEquip);

                if (EquippedWeapon != null && playerStats != null)
                {
                    playerStats.RemoveSpeedModifier(weaponStatManager.GetStat(WeaponStat.Mobility));
                }

                playerState?.SetWeaponClass(weapon.Class);

                if (weaponStatManager != null)
                {
                    weaponStatManager.ManageWeapon(weapon);
                }

                if (equippedWeaponObservable != null)
                {
                    equippedWeaponObservable.Value = weapon;
                }

                if (playerStats != null)
                {
                    playerStats.AddSpeedModifier(weaponStatManager.Mobility);
                }
            }

        }

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
            if (playerInventory == null || playerInventory.weaponInventory == null) return;
            if (viewState.CurrentSlot < 0 || viewState.CurrentSlot >= playerInventory.weaponInventory.Length) return;

            WeaponProperties weapon = playerInventory.weaponInventory[viewState.CurrentSlot];
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

            var allViews = skinInstance.GetComponentsInChildren<WeaponView>(true);
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

            currentWeaponView = allViews.FirstOrDefault(v => v.WeaponKey == EquippedWeapon.WeaponMuzzleKey);

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

        public void ExecuteWeaponSwap(WeaponProperties weapon)
        {
            EquipWeaponExternal(weapon);
        }

        public void EquipWeaponExternal(WeaponProperties weapon)
        {
            if (weapon == null) return;
            if (weapon.Key == EquippedWeapon?.Key) return;

            pendingWeaponKeyToEquip = weapon.Key;
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
            WeaponProperties existing = slot == WeaponSlot.Primary
                ? playerInventory.weaponInventory[0]
                : playerInventory.weaponInventory[1];

            if (existing == null) return;

            if (EquippedWeapon == existing)
            {
                if (playerStats != null)
                {
                    playerStats.RemoveSpeedModifier(existing.Mobility);
                }

                if (weaponStatManager != null)
                {
                    weaponStatManager.ManageWeapon(null);
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
                pendingPrimaryWeaponSlotRemoval = true;
            }
            else
            {
                pendingSecondaryWeaponSlotRemoval = true;
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
                pendingUpperAugmentRemoval = true;
            }
            else
            {
                pendingLowerAugmentRemoval = true;
            }
        }

        [SimulationOnly]
        public void RemoveAugment(AugmentProperties augment)
        {
            playerInventory.RemoveAugment(augment.Slot);
        }
    }
}
