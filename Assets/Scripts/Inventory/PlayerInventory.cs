using System;
using System.Collections.Generic;
using PurrNet;
using Resonance.Combat.Augments;
using Resonance.Combat.Weapons;
using Resonance.Combat.Weapons.Enums;
using UnityEngine;

namespace Resonance.Inventory
{
    public class PlayerInventory : NetworkBehaviour
    {
        public WeaponProperties[] weaponInventory = new WeaponProperties[2];
        [SerializeField] WeaponProperties startingWeapon;
        public AugmentProperties[] augmentInventory = new AugmentProperties[2];

         private void Awake()
         {
             WeaponProperties weapon = startingWeapon.Clone();
             AddWeapon(weapon);
         }
         
         public void AddWeapon(WeaponProperties weaponToAdd)
         {
             if (weaponToAdd == null)
             {
                 return;
             }
             
             switch (weaponToAdd.Slot)
             {
                 case WeaponSlot.Primary:
                     weaponInventory[0] = weaponToAdd;
                     break;
                 case WeaponSlot.Secondary:
                     weaponInventory[1] = weaponToAdd;
                     break;
             }
         }
         
         public void RemoveWeapon(WeaponSlot slot)
         {
             switch (slot)
             {
                 case WeaponSlot.Primary:
                     weaponInventory[0] = null;
                     break;
                 case WeaponSlot.Secondary:
                     weaponInventory[1] = null;
                     break;
             }
         }
         
         public void AddAugment(AugmentProperties augmentToAdd)
         {
             if (augmentToAdd == null)
             {
                 return;
             }

             switch (augmentToAdd.Slot)
             {
                 case AugmentSlot.Upper:
                     augmentInventory[0] = augmentToAdd;
                     break;
                 case AugmentSlot.Lower:
                     augmentInventory[1] = augmentToAdd;
                     break;
             }
         }

         public void RemoveAugment(AugmentSlot slot)
         {
             switch (slot)
             {
                 case AugmentSlot.Upper:
                     augmentInventory[0] = null;
                     break;
                 case AugmentSlot.Lower:
                     augmentInventory[1] = null;
                     break;
             }
         }
    }
}
