using Resonance.PlayerController;
using UnityEngine.InputSystem;
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

            var controls = Resonance.PlayerController.PlayerInputManager.Instance.PlayerControls;
            upperSlotUI.SetKeybindLabel(controls.PlayerActionMap.AbilityUpper.GetBindingDisplayString());
            lowerSlotUI.SetKeybindLabel(controls.PlayerActionMap.AbilityLower.GetBindingDisplayString());
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