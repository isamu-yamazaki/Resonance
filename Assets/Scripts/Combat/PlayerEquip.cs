using System.Linq;
using PurrNet;
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
    public class PlayerEquip : NetworkBehaviour
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

        [SerializeField] private PlayerActionsInput playerActionsInput;
        private PlayerState playerState;

        private WeaponView currentWeaponView;
        public WeaponView CurrentWeaponView => currentWeaponView;

        public WeaponProperties EquippedWeapon { get; private set; }

        private WeaponProperties[] weapons;
        private bool _isInitialEquip = true;

        private void Awake()
        {
            playerSkinRenderer = GetComponent<PlayerSkinRenderer>();
            playerSkinRenderer.OnNewSkinSpawned += OnNewSkinSpawned;
            weapons = Resources.LoadAll<WeaponProperties>("Content/Weapons");
            playerState = GetComponent<PlayerState>();
            fpArmsAnimator = GetComponent<FPArmsAnimator>();
        }

        protected override void OnSpawned()
        {
            base.OnSpawned();
            playerStats = GetComponent<PlayerStats>();
            playerAugmentEquipper = GetComponent<PlayerAugmentEquipper>();
            playerAbilityManager = GetComponent<PlayerAbilityManager>();
            weaponStatManager = GetComponent<WeaponStatManager>();

            if (isOwner)
                StartCoroutine(EquipStartingWeaponNextFrame());
        }

        private System.Collections.IEnumerator EquipStartingWeaponNextFrame()
        {
            yield return null;

            if (playerInventory == null) yield break;
            if (playerInventory.weaponInventory == null || playerInventory.weaponInventory.Length <= 1) yield break;

            WeaponProperties startWeapon = playerInventory.weaponInventory[1];
            if (startWeapon != null)
            {
                EquipWeapon(startWeapon);
            }
        }

        private void Update()
        {
            if (playerActionsInput == null || playerInventory == null) return;

            if (playerActionsInput.SwapWeaponPressed)
            {
                SwapWeapon();
                playerActionsInput.SetSwapWeaponPressedFalse();
            }

            if (playerActionsInput.SwapSlotOnePressed)
            {
                EquipFromSlot(0);
                playerActionsInput.SetSlotOnePressedFalse();
            }

            if (playerActionsInput.SwapSlotTwoPressed)
            {
                EquipFromSlot(1);
                playerActionsInput.SetSlotTwoPressedFalse();
            }
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

        private void SwapWeapon()
        {
            if (EquippedWeapon == null)
            {
                EquipFromSlot(1);
                return;
            }

            if (EquippedWeapon.Slot == WeaponSlot.Primary)
            {
                EquipFromSlot(1);
            }
            else
            {
                EquipFromSlot(0);
            }
        }

        private void EquipFromSlot(int slotIndex)
        {
            if (slotIndex < 0 || slotIndex >= playerInventory.weaponInventory.Length) return;

            WeaponProperties weapon = playerInventory.weaponInventory[slotIndex];
            if (weapon == null) return;
            if (weapon.Key == EquippedWeapon?.Key) return;

            GetComponent<PlayerShooter>().CancelReload();

            if (playerState.CurrentWeaponState != WeaponState.Idle) return;

            if (isOwner && fpArmsAnimator != null)
                fpArmsAnimator.RequestWeaponSwap(weapon);
            else
                EquipWeapon(weapon);
        }

        public void ExecuteWeaponSwap(WeaponProperties weapon)
        {
            EquipWeapon(weapon);
        }

        public void EquipWeapon(WeaponProperties weapon)
        {
            if (weapon == null) return;
            if (weapon.Key == EquippedWeapon?.Key) return;

            if (EquippedWeapon != null && playerStats != null)
            {
                playerStats.RemoveSpeedModifier(weaponStatManager.GetStat(WeaponStat.Mobility));
            }

            EquippedWeapon = weapon;
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

            if (playerSkinRenderer.CurrentMeshInstance != null)
            {
                RefreshTPWeaponViewOnAllClients(weapon.Key);
            }

            if (!_isInitialEquip)
            {
                PlayEquipOnAllClients();
            }

            _isInitialEquip = false;
        }

        [ObserversRpc(runLocally: true)]
        private void RefreshTPWeaponViewOnAllClients(string weaponKey)
        {
            if (!isOwner)
            {
                WeaponProperties weapon = System.Array.Find(weapons, w => w.Key == weaponKey);
                if (weapon == null) return;
                EquippedWeapon = weapon;
                weaponStatManager?.ManageWeapon(weapon);
                playerState?.SetWeaponClass(weapon.Class);
            }
            RefreshTPWeaponView(playerSkinRenderer.CurrentMeshInstance);
        }

        [ObserversRpc(runLocally: true)]
        private void PlayEquipOnAllClients()
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

                EquippedWeapon = null;

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

            playerInventory.RemoveWeapon(slot);
        }

        public void EquipAugment(AugmentProperties augment)
        {
            if (augment == null || playerAugmentEquipper == null) return;

            switch (augment.Slot)
            {
                case AugmentSlot.Upper:
                    if (playerInventory.augmentInventory[0] != null)
                    {
                        RemoveAugment(playerInventory.augmentInventory[0]);
                    }

                    playerInventory.AddAugment(augment);
                    playerAugmentEquipper.ApplyAugmentStats(augment);
                    playerAbilityManager.OnAugmentEquipped(augment);
                    break;
                case AugmentSlot.Lower:
                    if (playerInventory.augmentInventory[1] != null)
                    {
                        RemoveAugment(playerInventory.augmentInventory[1]);
                    }

                    playerInventory.AddAugment(augment);
                    playerAugmentEquipper.ApplyAugmentStats(augment);
                    playerAbilityManager.OnAugmentEquipped(augment);
                    break;
            }
        }

        public void RemoveAugment(AugmentProperties augment)
        {
            playerAbilityManager.OnAugmentRemoved(augment);
            playerAugmentEquipper.RemoveAugmentStats(augment);
            playerInventory.RemoveAugment(augment.Slot);
        }
    }
}