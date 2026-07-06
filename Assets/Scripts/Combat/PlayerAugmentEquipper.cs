using PurrNet.Prediction;
using Resonance.Combat.Augments;
using Resonance.Player;

namespace Resonance.Combat
{
    
    public class PlayerAugmentEquipper : PredictedIdentity<PlayerAugmentEquipperState>
    {
        private WeaponStatManager augmentedWeaponStatTarget;
        private PlayerStats augmentedPlayerStatTarget;

        protected override void LateAwake()
        {
            augmentedPlayerStatTarget = GetComponent<PlayerStats>();
            augmentedWeaponStatTarget = GetComponent<WeaponStatManager>();
        }

        [SimulationOnly]
        public void SimulateApplyAugmentStats(AugmentProperties augment)
        {
            // TODO: fully simulate
            if (augment == null)
            {
                return;
            }

            //Player Stats first
            if (augment.Speed != 0)
            {
                augmentedPlayerStatTarget.SimulateAddSpeedModifier(augment.Speed);
            }
            
            if (augment.DamageReduction != 0)
            {
                augmentedPlayerStatTarget.SimulateAddDamageReductionModifier(augment.DamageReduction);
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

        [SimulationOnly]
        public void SimulateRemoveAugmentStats(AugmentProperties augment)
        {
            // TODO: fully simulate
            if (augment == null)
            {
                return;
            }

            //Player Stats first
            if (augment.Speed != 0)
            {
                augmentedPlayerStatTarget.SimulateRemoveSpeedModifier(augment.Speed);
            }
            
            if (augment.DamageReduction != 0)
            {
                augmentedPlayerStatTarget.SimulateRemoveDamageReductionModifier(augment.DamageReduction);
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

    public struct PlayerAugmentEquipperState : IPredictedData<PlayerAugmentEquipperState>
    {
        public void Dispose()
        {
        }
    }
}
