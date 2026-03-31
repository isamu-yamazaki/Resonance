using System.Collections.Generic;
using System.Linq;
using Resonance.Combat.Augments;
using Resonance.Combat.Mods;
using Resonance.Combat.Weapons;
using Resonance.Combat.Weapons.Enums;
using Resonance.Inventory;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Resonance.Shop
{
    public class ShopInventoryDisplay : MonoBehaviour
    {
        [Header("Weapon Display")]
        [SerializeField] private TextMeshProUGUI primaryWeaponName;
        [SerializeField] private TextMeshProUGUI secondaryWeaponName;

        [Header("Mod Containers")]
        [SerializeField] private GameObject primaryModContainer;
        [SerializeField] private GameObject secondaryModContainer;
        [SerializeField] private GameObject modTextPrefab;

        [Header("Augment Display")]
        [SerializeField] private TextMeshProUGUI upperAugmentName;
        [SerializeField] private TextMeshProUGUI lowerAugmentName;

        [Header("Sell Buttons")]
        [SerializeField] private Button primaryWeaponSellButton;
        [SerializeField] private Button secondaryWeaponSellButton;
        [SerializeField] private Button upperAugmentSellButton;
        [SerializeField] private Button lowerAugmentSellButton;

        private PlayerInventory playerInventory;

        private List<GameObject> primaryModTexts = new List<GameObject>();
        private List<GameObject> secondaryModTexts = new List<GameObject>();

        private void Awake()
        {
            if (playerInventory == null || !playerInventory.isOwner)
                playerInventory = FindObjectsOfType<PlayerInventory>().FirstOrDefault(p => p.isOwner);

            primaryWeaponSellButton?.onClick.AddListener(() => ShopManager.Instance.SellWeapon(WeaponSlot.Primary));
            secondaryWeaponSellButton?.onClick.AddListener(() => ShopManager.Instance.SellWeapon(WeaponSlot.Secondary));
            upperAugmentSellButton?.onClick.AddListener(() => ShopManager.Instance.SellAugment(AugmentSlot.Upper));
            lowerAugmentSellButton?.onClick.AddListener(() => ShopManager.Instance.SellAugment(AugmentSlot.Lower));
        }

        public void Refresh()
        {
            if (playerInventory == null || !playerInventory.isOwner)
            {
                playerInventory = FindObjectsOfType<PlayerInventory>().FirstOrDefault(p => p.isOwner);
                if (playerInventory == null) return;
            }

            RefreshWeapon(playerInventory.weaponInventory[0], primaryWeaponName, primaryModContainer,
                primaryModTexts, WeaponSlot.Primary, primaryWeaponSellButton);
            RefreshWeapon(playerInventory.weaponInventory[1], secondaryWeaponName, secondaryModContainer,
                secondaryModTexts, WeaponSlot.Secondary, secondaryWeaponSellButton);

            upperAugmentName.text = playerInventory.augmentInventory[0] != null
                ? playerInventory.augmentInventory[0].AugmentName : "Empty";
            upperAugmentSellButton?.gameObject.SetActive(false);
            var upperHover = upperAugmentSellButton?.GetComponentInParent<InventoryItemHover>();
            if (upperHover != null) upperHover.enabled = playerInventory.augmentInventory[0] != null;
            
            lowerAugmentName.text = playerInventory.augmentInventory[1] != null
                ? playerInventory.augmentInventory[1].AugmentName : "Empty";
            lowerAugmentSellButton?.gameObject.SetActive(false);
            var lowerHover = lowerAugmentSellButton?.GetComponentInParent<InventoryItemHover>();
            if (lowerHover != null) lowerHover.enabled = playerInventory.augmentInventory[1] != null;
        }
        
        private void RefreshWeapon(WeaponProperties weapon, TextMeshProUGUI nameText,
            GameObject modContainer, List<GameObject> modTexts, WeaponSlot slot, Button sellButton)
        {
            var weaponHover = sellButton?.GetComponentInParent<InventoryItemHover>();
            if (weaponHover != null) weaponHover.enabled = weapon != null;
            sellButton?.gameObject.SetActive(false);
            
            foreach (Transform child in modContainer.transform)
                Destroy(child.gameObject);
    
            foreach (GameObject text in modTexts)
                Destroy(text);
            modTexts.Clear();

            if (weapon == null)
            {
                nameText.text = "Empty";
                return;
            }

            nameText.text = weapon.WeaponName;

            if (weapon.ModList == null || weapon.ModList.Count == 0) return;

            foreach (var mod in weapon.ModList)
            {
                if (mod == null) continue;

                GameObject go = Instantiate(modTextPrefab, modContainer.transform);
                TextMeshProUGUI text = go.GetComponent<TextMeshProUGUI>();
                text.text = mod.ModName;

                Button sellBtn = go.GetComponentInChildren<Button>(true);
                if (sellBtn != null)
                {
                    ModSlot modSlot = mod.Slot;
                    sellBtn.onClick.RemoveAllListeners();
                    sellBtn.onClick.AddListener(() => ShopManager.Instance.SellMod(slot, modSlot));
                }
            }
        }
    }
}