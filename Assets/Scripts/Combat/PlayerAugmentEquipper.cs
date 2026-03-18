using Resonance.Combat.Augments;
using Resonance.Player;
using UnityEngine;

namespace Resonance.Combat
{
    
    public class PlayerAugmentEquipper : MonoBehaviour
    {
        private WeaponStatManager augmentedWeaponStatTarget;
        private PlayerStats augmentedPlayerStatTarget;

        private void Awake()
        {
            augmentedPlayerStatTarget = GetComponent<PlayerStats>();
            augmentedWeaponStatTarget = GetComponent<WeaponStatManager>();
        }

        public void ApplyAugmentStats(AugmentProperties augment)
        {
            if (augment == null)
            {
                return;
            }

            //Player Stats first
            if (augment.Speed != 0)
            {
                augmentedPlayerStatTarget.AddSpeedModifier(augment.Speed);
            }
            
            if (augment.DamageReduction != 0)
            {
                augmentedPlayerStatTarget.AddDamageReductionModifier(augment.DamageReduction);
            }
            
            if (augment.Regen != 0)
            {
                augmentedPlayerStatTarget.AddRegenModifier(augment.Regen);
            }
            
            //Then handle weapon stats
            if (augment.ModProperties != null)
            {
                augmentedWeaponStatTarget.AddAugmentMod(augment.ModProperties);
            }
        }
        
        public void RemoveAugmentStats(AugmentProperties augment)
        {
            if (augment == null)
            {
                return;
            }

            //Player Stats first
            if (augment.Speed != 0)
            {
                augmentedPlayerStatTarget.RemoveSpeedModifier(augment.Speed);
            }
            
            if (augment.DamageReduction != 0)
            {
                augmentedPlayerStatTarget.RemoveDamageReductionModifier(augment.DamageReduction);
            }
            
            if (augment.Regen != 0)
            {
                augmentedPlayerStatTarget.RemoveRegenModifier(augment.Regen);
            }
            
            //Then handle weapon stats
            if (augment.ModProperties != null)
            {
                augmentedWeaponStatTarget.RemoveAugmentMod(augment.ModProperties);
            }
        }
    }
}
