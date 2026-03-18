using System.Collections.Generic;
using System.Linq;
using Resonance.Combat.Weapons;
using Resonance.Inventory;
using TMPro;
using UnityEngine;

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

        [Header("Augment Display")]
        [SerializeField] private TextMeshProUGUI upperAugmentName;
        [SerializeField] private TextMeshProUGUI lowerAugmentName;

        private PlayerInventory playerInventory;

        private List<GameObject> primaryModTexts = new List<GameObject>();
        private List<GameObject> secondaryModTexts = new List<GameObject>();

        private void Awake()
        {
            if (playerInventory == null || !playerInventory.isOwner)
            {
                playerInventory = FindObjectsOfType<PlayerInventory>().FirstOrDefault(p => p.isOwner);
            }
        }

        public void Refresh()
        {
            if (playerInventory == null || !playerInventory.isOwner)
            {
                playerInventory = FindObjectsOfType<PlayerInventory>().FirstOrDefault(p => p.isOwner);
                if (playerInventory == null)
                {
                    return;
                }
            }

            RefreshWeapon(playerInventory.weaponInventory[0], primaryWeaponName, primaryModContainer, primaryModTexts);
            RefreshWeapon(playerInventory.weaponInventory[1], secondaryWeaponName, secondaryModContainer, secondaryModTexts);

            upperAugmentName.text = playerInventory.augmentInventory[0] != null
                ? playerInventory.augmentInventory[0].AugmentName
                : "Empty";

            lowerAugmentName.text = playerInventory.augmentInventory[1] != null
                ? playerInventory.augmentInventory[1].AugmentName
                : "Empty";
        }

        private void RefreshWeapon(WeaponProperties weapon, TextMeshProUGUI nameText, GameObject modContainer, List<GameObject> modTexts)
        {
            foreach (GameObject text in modTexts)
            {
                Destroy(text);
            }

            modTexts.Clear();

            if (weapon == null)
            {
                nameText.text = "Empty";
                return;
            }

            nameText.text = weapon.WeaponName;

            if (weapon.ModList == null || weapon.ModList.Count == 0)
            {
                return;
            }

            foreach (var mod in weapon.ModList)
            {
                if (mod == null)
                {
                    continue;
                }

                GameObject go = new GameObject(mod.ModName);
                go.transform.SetParent(modContainer.transform, false);

                TextMeshProUGUI text = go.AddComponent<TextMeshProUGUI>();
                text.text = $"- {mod.ModName}";

                modTexts.Add(go);
            }
        }
    }
}
