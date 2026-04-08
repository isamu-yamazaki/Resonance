using System.Collections.Generic;
using Resonance.Combat.Augments;
using Resonance.Combat.Augments.UI;
using Resonance.Inventory;
using Resonance.PlayerController;
using UnityEngine;

namespace Resonance.Combat
{
    public class PlayerAbilityManager : MonoBehaviour
    {
        #region Fields

        private PlayerActionsInput playerActionsInput;
        private PlayerInventory inventory;
        private FPArmsAnimator fpArmsAnimator;
        private Dictionary<string, IAugmentAbility> abilityMap = new();

        #endregion

        #region Startup

        private void Start()
        {
            playerActionsInput = GetComponent<PlayerActionsInput>();
            inventory = GetComponent<PlayerInventory>();
            fpArmsAnimator = GetComponent<FPArmsAnimator>();

            foreach (IAugmentAbility ability in GetComponents<IAugmentAbility>())
            {
                abilityMap[ability.AbilityKey] = ability;
                SetAbilityEnabled(ability, false);
            }
        }

        #endregion

        #region Update

        private void Update()
        {
            if (playerActionsInput.AbilityUpperPressed)
            {
                TryUseUpperActiveAbility();
            }

            if (playerActionsInput.AbilityLowerPressed)
            {
                TryUseLowerActiveAbility();
            }
        }

        #endregion

        #region Methods

        public void OnAugmentEquipped(AugmentProperties augment)
        {
            if (augment == null || string.IsNullOrEmpty(augment.AbilityKey))
            {
                return;
            }

            if (abilityMap.TryGetValue(augment.AbilityKey, out IAugmentAbility ability))
            {
                SetAbilityEnabled(ability, true);
                AugmentHUDManager.Instance.OnAugmentEquipped(augment, ability);
            }
        }

        public void OnAugmentRemoved(AugmentProperties augment)
        {
            if (augment == null || string.IsNullOrEmpty(augment.AbilityKey))
            {
                return;
            }

            if (abilityMap.TryGetValue(augment.AbilityKey, out IAugmentAbility ability))
            {
                SetAbilityEnabled(ability, false);
                AugmentHUDManager.Instance.OnAugmentRemoved(augment.Slot);
            }
        }

        private void TryUseUpperActiveAbility()
        {
            playerActionsInput.SetAbilityUpperPressedFalse();
            IAugmentAbility ability = GetAbility(inventory.augmentInventory[0]?.AbilityKey);
            if (ability == null || !ability.AbilityReady) return;

            if (ability is AbilityGrappleHook)
            {
                fpArmsAnimator?.RequestGrappleActivation();
                return;
            }

            ability.ActivateAbility();
            AugmentHUDManager.Instance.OnAbilityUsed(AugmentSlot.Upper);
        }

        private void TryUseLowerActiveAbility()
        {
            playerActionsInput.SetAbilityLowerPressedFalse();
            IAugmentAbility ability = GetAbility(inventory.augmentInventory[1]?.AbilityKey);
            if (ability == null || !ability.AbilityReady) return;

            if (ability is AbilityGrappleHook)
            {
                fpArmsAnimator?.RequestGrappleActivation();
                return;
            }

            ability.ActivateAbility();
            AugmentHUDManager.Instance.OnAbilityUsed(AugmentSlot.Lower);
        }

        private IAugmentAbility GetAbility(string key)
        {
            if (string.IsNullOrEmpty(key))
            {
                return null;
            }

            abilityMap.TryGetValue(key, out IAugmentAbility ability);
            return ability;
        }

        private void SetAbilityEnabled(IAugmentAbility ability, bool enabled)
        {
            if (ability is MonoBehaviour mb)
            {
                mb.enabled = enabled;
            }
        }

        #endregion
    }
}
