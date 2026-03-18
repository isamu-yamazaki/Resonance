using Resonance.Combat.Augments;
using Resonance.Inventory;
using UnityEngine;

namespace Resonance.Combat
{
    public class PlayerAugmentManager : MonoBehaviour
    {
        private PlayerInventory playerInventory;
        private IAugmentAbility[] abilities;

        private void Awake()
        {
            playerInventory = GetComponent<PlayerInventory>();
            abilities = GetComponents<IAugmentAbility>();
        }

        public void ActivateAugmentAbility(AugmentSlot slot)
        {
            AugmentProperties augment = slot == AugmentSlot.Upper
                ? playerInventory.augmentInventory[0]
                : playerInventory.augmentInventory[1];

            if (augment == null)
            {
                return;
            }

            IAugmentAbility ability = GetAbility(augment.AbilityKey);
            if (ability == null)
            {
                return;
            }

            ability.ActivateAbility();
        }

        private IAugmentAbility GetAbility(string key)
        {
            if (string.IsNullOrEmpty(key))
            {
                return null;
            }

            foreach (IAugmentAbility ability in abilities)
            {
                if (ability.Name == key)
                {
                    return ability;
                }
            }

            return null;
        }
    }
}