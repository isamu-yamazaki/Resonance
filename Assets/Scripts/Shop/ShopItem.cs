using Resonance.Combat.Augments;
using Resonance.Combat.Mods;
using Resonance.Combat.Weapons;
using Resonance.Economy;
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

        private float itemCost;

        public void SetupWeapon(WeaponProperties weapon)
        {
            Weapon = weapon;
            itemCost = weapon.WeaponCost;
            itemNameText.text = weapon.WeaponName;
            itemCostText.text = $"₢ {weapon.WeaponCost:0}";
            iconImage.sprite = weapon.Icon;
            iconImage.enabled = weapon.Icon != null;
            button.onClick.AddListener(() => ShopOverlayView.Instance.Buy(weapon));
            RefreshAffordability();
        }

        public void SetupAugment(AugmentProperties augment)
        {
            Augment = augment;
            itemCost = augment.AugmentCost;
            itemNameText.text = augment.AugmentName;
            itemCostText.text = $"₢ {augment.AugmentCost:0}";
            iconImage.sprite = augment.Icon;
            iconImage.enabled = augment.Icon != null;
            button.onClick.AddListener(() => ShopOverlayView.Instance.Buy(augment));
            RefreshAffordability();
        }

        public void SetupMod(WeaponModProperties mod)
        {
            Mod = mod;
            itemCost = mod.ModCost;
            itemNameText.text = mod.ModName;
            itemCostText.text = $"₢ {mod.ModCost:0}";
            iconImage.sprite = mod.Icon;
            iconImage.enabled = mod.Icon != null;
            button.onClick.AddListener(() => ShopOverlayView.Instance.Buy(mod));
            RefreshAffordability();
        }

        public void RefreshAffordability()
        {
            if (PlayerMoney.LocalInstance == null) return;

            bool canAfford = PlayerMoney.LocalInstance.CanAfford(itemCost);
            button.interactable = canAfford;
            itemCostText.color = canAfford ? Color.white : Color.red;
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
#if !UNITY_SERVER
            AkSoundEngine.PostEvent("Play_UI_Button_Hover", gameObject);
#endif
            ShopOverlayView.Instance.OnItemHovered(this);
        }
    }
}
