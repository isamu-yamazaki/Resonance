using UnityEngine;

namespace Resonance.Combat.Augments.UI
{
    public class AugmentHUDManager : MonoBehaviour
    {
        public static AugmentHUDManager Instance { get; private set; }

        [SerializeField] private AugmentSlotUI upperSlotUI;
        [SerializeField] private AugmentSlotUI lowerSlotUI;

        private void Awake()
        {
            Instance = this;
        }

        public void OnAugmentEquipped(AugmentProperties augment, IAugmentAbility ability)
        {
            var ui = augment.Slot == AugmentSlot.Upper ? upperSlotUI : lowerSlotUI;
            ui.SetAugment(augment, ability);
        }

        public void OnAugmentRemoved(AugmentSlot slot)
        {
            var ui = slot == AugmentSlot.Upper ? upperSlotUI : lowerSlotUI;
            ui.ClearAugment();
        }

        public void OnAbilityUsed(AugmentSlot slot)
        {
            var ui = slot == AugmentSlot.Upper ? upperSlotUI : lowerSlotUI;
            ui.OnAbilityActivated();
        }
    }
}