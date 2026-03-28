using System.Collections.Generic;
using Resonance.Combat.Augments;
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
        private Dictionary<string, IAugmentAbility> abilityMap = new();

        #endregion

        #region Startup

        private void Start()
        {
            playerActionsInput = GetComponent<PlayerActionsInput>();
            inventory = GetComponent<PlayerInventory>();

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
            }
        }

        private void TryUseUpperActiveAbility()
        {
            playerActionsInput.SetAbilityUpperPressedFalse();
            IAugmentAbility ability = GetAbility(inventory.augmentInventory[0]?.AbilityKey);
            if (ability == null || !ability.AbilityReady)
                return;

            ability.ActivateAbility();
        }

        private void TryUseLowerActiveAbility()
        {
            playerActionsInput.SetAbilityLowerPressedFalse();
            IAugmentAbility ability = GetAbility(inventory.augmentInventory[1]?.AbilityKey);
            if (ability == null || !ability.AbilityReady)
                return;

            ability.ActivateAbility();
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
