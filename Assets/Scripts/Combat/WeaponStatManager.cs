using System.Collections.Generic;
using System.Linq;
using Resonance.Combat.Mods;
using Resonance.Combat.Weapons;
using Resonance.Combat.Weapons.Enums;
using UnityEngine;

namespace Resonance.Combat
{
    public class WeaponStatManager : MonoBehaviour
    {
        private WeaponProperties managedWeapon;
        private readonly List<WeaponModProperties> augmentMods = new List<WeaponModProperties>();

        public void ManageWeapon(WeaponProperties weaponToManage)
        {
            managedWeapon = weaponToManage;
        }

        public void AddAugmentMod(WeaponModProperties mod)
        {
            if (mod == null) return;
            augmentMods.Add(mod);
        }

        public void RemoveAugmentMod(WeaponModProperties mod)
        {
            augmentMods.Remove(mod);
        }

        public void ClearAugmentMods()
        {
            augmentMods.Clear();
        }

        public float GetStat(WeaponStat stat)
        {
            if (managedWeapon == null) return 0f;

            float baseStat = GetBaseValue(stat);
            float additiveSum = 0f;
            float multiplicativeProduct = 1f;

            IEnumerable<WeaponModProperties> allMods = managedWeapon.ModList.Concat(augmentMods).Where(mod => mod != null);

            foreach (WeaponModProperties mod in allMods)
            {
                foreach (StatModifier modifier in mod.Modifiers)
                {
                    if (modifier.stat != stat) continue;

                    if (modifier.type == ModifierType.Additive)
                        additiveSum += modifier.value;
                    else
                        multiplicativeProduct *= modifier.value;
                }
            }

            return (baseStat + additiveSum) * multiplicativeProduct;
        }

        public BulletProperties GetBulletProperties()
        {
            if (managedWeapon == null) return null;

            IEnumerable<WeaponModProperties> allMods = managedWeapon.ModList.Concat(augmentMods).Where(mod => mod != null);

            foreach (WeaponModProperties mod in allMods)
            {
                if (mod.BulletPropertiesOverride != null)
                    return mod.BulletPropertiesOverride;
            }

            return managedWeapon.BulletProperties;
        }
        
        public MuzzleFlashSettings GetMuzzleFlashSettings()
        {
            if (managedWeapon == null) return null;

            IEnumerable<WeaponModProperties> allMods = managedWeapon.ModList.Concat(augmentMods).Where(mod => mod != null);

            foreach (WeaponModProperties mod in allMods)
            {
                if (mod.Slot == ModSlot.Barrel && mod.MuzzleFlashOverride != null)
                    return mod.MuzzleFlashOverride;
            }

            return null;
        }

        private float GetBaseValue(WeaponStat stat) => stat switch
        {
            WeaponStat.Damage             => managedWeapon.Damage,
            WeaponStat.FireRate           => managedWeapon.FireRate,
            WeaponStat.ProjectilesPerShot => managedWeapon.ProjectilesPerShot,
            WeaponStat.Range              => managedWeapon.Range,
            WeaponStat.Accuracy           => managedWeapon.Accuracy,
            WeaponStat.Control            => managedWeapon.Control,
            WeaponStat.Spread             => managedWeapon.Spread,
            WeaponStat.MuzzleVelocity     => managedWeapon.MuzzleVelocity,
            WeaponStat.Mobility           => managedWeapon.Mobility,
            WeaponStat.Handling           => managedWeapon.Handling,
            WeaponStat.MagazineSize       => managedWeapon.MagazineSize,
            WeaponStat.ReloadTime         => managedWeapon.ReloadTime,
            WeaponStat.SpreadPerShot      => managedWeapon.SpreadPerShot,
            WeaponStat.MaxSpread          => managedWeapon.MaxSpread,
            WeaponStat.SpreadRecoveryRate => managedWeapon.SpreadRecoveryRate,
            _                             => 0f
        };

        public float Damage             => GetStat(WeaponStat.Damage);
        public float FireRate           => GetStat(WeaponStat.FireRate);
        public int   ProjectilesPerShot => Mathf.RoundToInt(GetStat(WeaponStat.ProjectilesPerShot));
        public float Range              => GetStat(WeaponStat.Range);
        public float Accuracy           => GetStat(WeaponStat.Accuracy);
        public float Control            => GetStat(WeaponStat.Control);
        public float Spread             => GetStat(WeaponStat.Spread);
        public float MuzzleVelocity     => GetStat(WeaponStat.MuzzleVelocity);
        public float Mobility           => GetStat(WeaponStat.Mobility);
        public float Handling           => GetStat(WeaponStat.Handling);
        public int   MagazineSize       => Mathf.RoundToInt(GetStat(WeaponStat.MagazineSize));
        public float ReloadTime         => GetStat(WeaponStat.ReloadTime);
        public float SpreadPerShot      => GetStat(WeaponStat.SpreadPerShot);
        public float MaxSpread          => GetStat(WeaponStat.MaxSpread);
        public float SpreadRecoveryRate => GetStat(WeaponStat.SpreadRecoveryRate);

        public WeaponProperties ManagedWeapon => managedWeapon;
    }
}
