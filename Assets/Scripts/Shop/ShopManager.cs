using System.Linq;
using Resonance.Combat;
using Resonance.Combat.Augments;
using Resonance.Combat.Mods;
using Resonance.Combat.Weapons;
using Resonance.Combat.Weapons.Enums;
using Resonance.Inventory;
using Resonance.PlayerController;
using UnityEngine;
using UnityEngine.UI;

namespace Resonance.Shop
{
    public class ShopManager : MonoBehaviour
    {
        public static ShopManager Instance { get; private set; }

        [SerializeField] private GameObject shopMenu;
        [SerializeField] private ShopInventoryDisplay inventoryDisplay;

        private PlayerInventory playerInventory;
        private PlayerEquip playerEquip;
        private PlayerShooter playerShooter;

        [Header("Main Tab Panels")]
        [SerializeField] private GameObject weaponBuyTab;
        [SerializeField] private GameObject augmentBuyTab;
        [SerializeField] private GameObject modBuyTab;

        [Header("Main Tab Buttons")]
        [SerializeField] private Button weaponTabButton;
        [SerializeField] private Button augmentTabButton;
        [SerializeField] private Button modTabButton;

        [Header("Weapon Sub Tab Buttons")]
        [SerializeField] private Button weaponPrimaryButton;
        [SerializeField] private Button weaponSecondaryButton;

        [Header("Augment Sub Tab Buttons")]
        [SerializeField] private Button augmentUpperButton;
        [SerializeField] private Button augmentLowerButton;

        [Header("Mod Weapon Sub Tab Buttons")]
        [SerializeField] private Button modPrimaryButton;
        [SerializeField] private Button modSecondaryButton;

        [Header("Mod Slot Sub Tab Buttons")]
        [SerializeField] private Button modBarrelButton;
        [SerializeField] private Button modGripButton;
        [SerializeField] private Button modStockButton;
        [SerializeField] private Button modMagazineButton;
        [SerializeField] private Button modOpticButton;
        [SerializeField] private Button modSpecialButton;

        [Header("Item Spawn Areas")]
        [SerializeField] private GameObject weaponItemSpawn;
        [SerializeField] private GameObject augmentItemSpawn;
        [SerializeField] private GameObject modItemSpawn;

        [Header("Shop Item Prefab")]
        [SerializeField] private GameObject shopItemPrefab;

        private Button activeMainTab;
        private Button activeWeaponSubTab;
        private Button activeAugmentSubTab;
        private Button activeModWeaponSubTab;
        private Button activeModSlotSubTab;

        private WeaponSlot selectedWeaponSlot = WeaponSlot.Primary;
        private AugmentSlot selectedAugmentSlot = AugmentSlot.Upper;
        private WeaponSlot selectedModWeaponSlot = WeaponSlot.Secondary;
        private ModSlot selectedModSlot = ModSlot.Barrel;
        private bool modSlotEverSelected = false;
        private bool augmentSlotEverSelected = true;

        private WeaponProperties[] weapons;
        private AugmentProperties[] augments;
        private WeaponModProperties[] mods;

        #region Unity Lifecycle

        private void Awake()
        {
            Instance = this;

            weapons = Resources.LoadAll<WeaponProperties>("Content/Weapons");
            augments = Resources.LoadAll<AugmentProperties>("Content/Augments");
            mods = Resources.LoadAll<WeaponModProperties>("Content/Mods");
        }

        private void Start()
        {
            weaponTabButton.onClick.AddListener(() =>
            {
                SwitchMainTab(weaponBuyTab, weaponTabButton);
                PopulateWeapons();
            });

            augmentTabButton.onClick.AddListener(() =>
            {
                SwitchMainTab(augmentBuyTab, augmentTabButton);
                PopulateAugments();
            });

            modTabButton.onClick.AddListener(() =>
            {
                SwitchMainTab(modBuyTab, modTabButton);
                RefreshModWeaponButtons();
                PopulateMods();
            });

            weaponPrimaryButton.onClick.AddListener(() =>
            {
                SwitchWeaponSubTab(weaponPrimaryButton);
                selectedWeaponSlot = WeaponSlot.Primary;
                PopulateWeapons();
            });

            weaponSecondaryButton.onClick.AddListener(() =>
            {
                SwitchWeaponSubTab(weaponSecondaryButton);
                selectedWeaponSlot = WeaponSlot.Secondary;
                PopulateWeapons();
            });

            augmentUpperButton.onClick.AddListener(() =>
            {
                SwitchAugmentSubTab(augmentUpperButton);
                selectedAugmentSlot = AugmentSlot.Upper;
                augmentSlotEverSelected = true;
                PopulateAugments();
            });

            augmentLowerButton.onClick.AddListener(() =>
            {
                SwitchAugmentSubTab(augmentLowerButton);
                selectedAugmentSlot = AugmentSlot.Lower;
                augmentSlotEverSelected = true;
                PopulateAugments();
            });

            modPrimaryButton.onClick.AddListener(() =>
            {
                SwitchModWeaponSubTab(modPrimaryButton);
                selectedModWeaponSlot = WeaponSlot.Primary;
                PopulateMods();
            });

            modSecondaryButton.onClick.AddListener(() =>
            {
                SwitchModWeaponSubTab(modSecondaryButton);
                selectedModWeaponSlot = WeaponSlot.Secondary;
                PopulateMods();
            });

            modBarrelButton.onClick.AddListener(() =>
            {
                SwitchModSlotSubTab(modBarrelButton);
                selectedModSlot = ModSlot.Barrel;
                modSlotEverSelected = true;
                PopulateMods();
            });

            modGripButton.onClick.AddListener(() =>
            {
                SwitchModSlotSubTab(modGripButton);
                selectedModSlot = ModSlot.Grip;
                modSlotEverSelected = true;
                PopulateMods();
            });

            modStockButton.onClick.AddListener(() =>
            {
                SwitchModSlotSubTab(modStockButton);
                selectedModSlot = ModSlot.Stock;
                modSlotEverSelected = true;
                PopulateMods();
            });

            modMagazineButton.onClick.AddListener(() =>
            {
                SwitchModSlotSubTab(modMagazineButton);
                selectedModSlot = ModSlot.Magazine;
                modSlotEverSelected = true;
                PopulateMods();
            });

            modOpticButton.onClick.AddListener(() =>
            {
                SwitchModSlotSubTab(modOpticButton);
                selectedModSlot = ModSlot.Optic;
                modSlotEverSelected = true;
                PopulateMods();
            });

            modSpecialButton.onClick.AddListener(() =>
            {
                SwitchModSlotSubTab(modSpecialButton);
                selectedModSlot = ModSlot.Special;
                modSlotEverSelected = true;
                PopulateMods();
            });

            SwitchMainTab(weaponBuyTab, weaponTabButton);
            SwitchWeaponSubTab(weaponPrimaryButton);
            SwitchAugmentSubTab(augmentUpperButton);
            PopulateWeapons();
        }

        private PlayerEquip GetPlayerEquip()
        {
            if (playerEquip == null || !playerEquip.isOwner)
            {
                playerEquip = FindObjectsOfType<PlayerEquip>().FirstOrDefault(p => p.isOwner);
            }

            return playerEquip;
        }

        private PlayerInventory GetPlayerInventory()
        {
            if (playerInventory == null || !playerInventory.isOwner)
            {
                playerInventory = FindObjectsOfType<PlayerInventory>().FirstOrDefault(p => p.isOwner);
            }

            return playerInventory;
        }

        private PlayerShooter GetPlayerShooter()
        {
            if (playerShooter == null)
            {
                playerShooter = FindObjectOfType<PlayerShooter>();
            }

            return playerShooter;
        }

        #endregion

        #region Tab Switching

        private void SwitchMainTab(GameObject panel, Button button)
        {
            weaponBuyTab.SetActive(false);
            augmentBuyTab.SetActive(false);
            modBuyTab.SetActive(false);

            panel.SetActive(true);

            SetButtonSelected(activeMainTab, false);
            SetButtonSelected(button, true);
            activeMainTab = button;
        }

        private void SwitchWeaponSubTab(Button button)
        {
            SetButtonSelected(activeWeaponSubTab, false);
            SetButtonSelected(button, true);
            activeWeaponSubTab = button;
        }

        private void SwitchAugmentSubTab(Button button)
        {
            SetButtonSelected(activeAugmentSubTab, false);
            SetButtonSelected(button, true);
            activeAugmentSubTab = button;
        }

        private void SwitchModWeaponSubTab(Button button)
        {
            SetButtonSelected(activeModWeaponSubTab, false);
            SetButtonSelected(button, true);
            activeModWeaponSubTab = button;
        }

        private void SwitchModSlotSubTab(Button button)
        {
            SetButtonSelected(activeModSlotSubTab, false);
            SetButtonSelected(button, true);
            activeModSlotSubTab = button;
        }

        private void SetButtonSelected(Button button, bool selected)
        {
            if (button == null)
            {
                return;
            }

            Color active = new Color(0.5f, 0.5f, 0.5f);
            Color inactive = Color.white;

            ColorBlock colors = button.colors;
            colors.normalColor = selected ? active : inactive;
            colors.selectedColor = selected ? active : inactive;
            colors.highlightedColor = selected ? new Color(0.6f, 0.6f, 0.6f) : new Color(0.9f, 0.9f, 0.9f);
            button.colors = colors;
        }

        private void RefreshModWeaponButtons()
        {
            PlayerInventory inventory = GetPlayerInventory();

            bool hasPrimary = inventory != null && inventory.weaponInventory[0] != null;
            bool hasSecondary = inventory != null && inventory.weaponInventory[1] != null;

            modPrimaryButton.interactable = hasPrimary;
            modSecondaryButton.interactable = hasSecondary;

            // Auto select whichever slot has a weapon, prefer secondary
            if (activeModWeaponSubTab == null)
            {
                if (hasSecondary)
                {
                    SwitchModWeaponSubTab(modSecondaryButton);
                    selectedModWeaponSlot = WeaponSlot.Secondary;
                }
                else if (hasPrimary)
                {
                    SwitchModWeaponSubTab(modPrimaryButton);
                    selectedModWeaponSlot = WeaponSlot.Primary;
                }
            }
            else
            {
                // If currently selected slot no longer has a weapon, switch to the other
                if (selectedModWeaponSlot == WeaponSlot.Primary && !hasPrimary && hasSecondary)
                {
                    SwitchModWeaponSubTab(modSecondaryButton);
                    selectedModWeaponSlot = WeaponSlot.Secondary;
                }
                else if (selectedModWeaponSlot == WeaponSlot.Secondary && !hasSecondary && hasPrimary)
                {
                    SwitchModWeaponSubTab(modPrimaryButton);
                    selectedModWeaponSlot = WeaponSlot.Primary;
                }
            }
        }

        #endregion

        #region Population

        private void ClearItems(GameObject spawnArea)
        {
            foreach (Transform child in spawnArea.transform)
            {
                Destroy(child.gameObject);
            }
        }

        private void PopulateWeapons()
        {
            ClearItems(weaponItemSpawn);

            foreach (WeaponProperties weapon in weapons)
            {
                if (weapon == null)
                {
                    continue;
                }

                if (weapon.Slot != selectedWeaponSlot)
                {
                    continue;
                }

                GameObject go = Instantiate(shopItemPrefab, weaponItemSpawn.transform);
                ShopItem item = go.GetComponent<ShopItem>();
                item.SetupWeapon(weapon);
            }
        }

        private void PopulateAugments()
        {
            ClearItems(augmentItemSpawn);

            foreach (AugmentProperties augment in augments)
            {
                if (augment == null)
                {
                    continue;
                }

                if (augmentSlotEverSelected && augment.Slot != selectedAugmentSlot)
                {
                    continue;
                }

                GameObject go = Instantiate(shopItemPrefab, augmentItemSpawn.transform);
                ShopItem item = go.GetComponent<ShopItem>();
                item.SetupAugment(augment);
            }
        }

        private void PopulateMods()
        {
            ClearItems(modItemSpawn);

            PlayerInventory inventory = GetPlayerInventory();

            WeaponProperties targetWeapon = null;
            if (inventory != null)
            {
                targetWeapon = selectedModWeaponSlot == WeaponSlot.Primary
                    ? inventory.weaponInventory[0]
                    : inventory.weaponInventory[1];
            }

            if (targetWeapon == null)
            {
                return;
            }

            foreach (WeaponModProperties mod in mods)
            {
                if (mod == null)
                {
                    continue;
                }

                if (mod.Slot == ModSlot.Augment)
                {
                    continue;
                }

                if (modSlotEverSelected && mod.Slot != selectedModSlot)
                {
                    continue;
                }

                if (mod.CompatibleWeaponClasses.Count > 0 && !mod.CompatibleWeaponClasses.Contains(targetWeapon.Class))
                {
                    continue;
                }

                GameObject go = Instantiate(shopItemPrefab, modItemSpawn.transform);
                ShopItem item = go.GetComponent<ShopItem>();
                item.SetupMod(mod);
            }
        }

        #endregion

        #region Buying

        public void Buy(WeaponProperties newWeapon)
        {
            PlayerEquip equip = GetPlayerEquip();
            PlayerInventory inventory = GetPlayerInventory();

            if (equip == null || inventory == null)
            {
                return;
            }

            WeaponProperties weapon = newWeapon.Clone();

            if (equip.EquippedWeapon != null && equip.EquippedWeapon.Slot == weapon.Slot)
            {
                equip.RemoveWeapon(weapon.Slot);
                inventory.AddWeapon(weapon);
                equip.EquipWeapon(weapon);
            }
            else
            {
                inventory.AddWeapon(weapon);
            }

            inventoryDisplay.Refresh();
        }

        public void Buy(AugmentProperties augment)
        {
            PlayerEquip equip = GetPlayerEquip();

            if (equip == null)
            {
                return;
            }

            equip.EquipAugment(augment);
            inventoryDisplay.Refresh();
        }

        public void Buy(WeaponModProperties mod)
        {
            PlayerInventory inventory = GetPlayerInventory();

            if (inventory == null)
            {
                return;
            }

            WeaponProperties targetWeapon = selectedModWeaponSlot == WeaponSlot.Primary
                ? inventory.weaponInventory[0]
                : inventory.weaponInventory[1];

            if (targetWeapon == null)
            {
                return;
            }

            targetWeapon.ModList.Add(mod);

            GetPlayerShooter()?.RefreshWeaponStats();
            inventoryDisplay.Refresh();
        }

        public void OnItemHovered(ShopItem item)
        {
        }

        #endregion

        #region Toggle

        public void Toggle()
        {
            PlayerState playerState = FindObjectOfType<PlayerState>();

            if (shopMenu.activeSelf)
            {
                shopMenu.SetActive(false);
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;

                playerState?.SetPlayerMovementState(PlayerMovementState.Idling);
            }
            else
            {
                shopMenu.SetActive(true);
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;

                playerState?.SetPlayerMovementState(PlayerMovementState.InShop);
                RefreshModWeaponButtons();

                if (inventoryDisplay != null)
                {
                    inventoryDisplay.Refresh();
                }
            }
        }

        #endregion
    }
}
