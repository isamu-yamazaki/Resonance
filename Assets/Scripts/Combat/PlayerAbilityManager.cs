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

        #endregion

        #region Startup

        private void Start()
        {
            playerActionsInput = GetComponent<PlayerActionsInput>();
            inventory = GetComponent<PlayerInventory>();
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

        private void TryUseUpperActiveAbility()
        {
            AugmentAbility ability = inventory.augmentInventory[0]?.Ability;
            if (ability == null || !ability.AbilityReady)
            {
                return;
            }

            ability.ActivateAbility();
        }

        private void TryUseLowerActiveAbility()
        {
            AugmentAbility ability = inventory.augmentInventory[1]?.Ability;
            if (ability == null || !ability.AbilityReady)
            {
                return;
            }

            ability.ActivateAbility();
        }

        #endregion
    }
}