using Resonance.Combat.Augments;
using Resonance.Combat.Mods;
using Resonance.Combat.Weapons;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

namespace Resonance.Shop
{
    public class ShopItem : MonoBehaviour, IPointerEnterHandler
    {
        [SerializeField] private TextMeshProUGUI itemNameText;
        [SerializeField] private TextMeshProUGUI itemCostText;
        [SerializeField] private Image iconImage;
        [SerializeField] private Button button;

        public WeaponProperties Weapon { get; private set; }
        public AugmentProperties Augment { get; private set; }
        public WeaponModProperties Mod { get; private set; }

        public void SetupWeapon(WeaponProperties weapon)
        {
            Weapon = weapon;
            itemNameText.text = weapon.WeaponName;
            //itemCostText.text = weapon.cost;
            iconImage.sprite = weapon.Icon;
            button.onClick.AddListener(() => ShopManager.Instance.Buy(weapon));
        }

        public void SetupAugment(AugmentProperties augment)
        {
            Augment = augment;
            itemNameText.text = augment.AugmentName;
            iconImage.sprite = augment.Icon;
            //itemCostText.text = augment.cost
            button.onClick.AddListener(() => ShopManager.Instance.Buy(augment));
        }

        public void SetupMod(WeaponModProperties mod)
        {
            Mod = mod;
            itemNameText.text = mod.ModName;
            //itemCostText.text = mod.cost
            iconImage.sprite = mod.Icon;
            button.onClick.AddListener(() => ShopManager.Instance.Buy(mod));
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            ShopManager.Instance.OnItemHovered(this);
        }
    }
}