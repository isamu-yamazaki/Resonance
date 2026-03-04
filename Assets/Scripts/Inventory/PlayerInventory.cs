using System;
using System.Collections.Generic;
using Resonance.Combat.Weapons;
using Resonance.Combat.Weapons.Enums;
using UnityEngine;

namespace Resonance.Inventory
{
    public class PlayerInventory : MonoBehaviour
    {
        public WeaponProperties[] weaponInventory = new WeaponProperties[2];
        [SerializeField] WeaponProperties startingWeapon;
         //List of augments

         public void AddWeapon(WeaponProperties weaponToAdd)
         {
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

         private void Start()
         {
             AddWeapon(startingWeapon);
         }
    }
}
