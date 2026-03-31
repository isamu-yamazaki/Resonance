using System.Collections.Generic;
using Resonance.Combat.Weapons;
using Resonance.Combat.Weapons.Enums;
using UnityEngine;

namespace Resonance.Combat.Mods
{
    [CreateAssetMenu(fileName = "New Weapon Mod", menuName = "Resonance/Weapons/Weapon Mod")]
    public class WeaponModProperties : ScriptableObject
    {
        [Tooltip("Unique identifier.")]
        [SerializeField] private string key;
        public string Key => key;

        [Header("Flavor Text")]
        [SerializeField] private string modName;
        public string ModName => modName;

        [TextArea(1, 5)]
        [SerializeField] private string description;
        public string Description => description;

        [Header("Identifiers")]
        [SerializeField] private ModSlot slot;
        public ModSlot Slot => slot;

        [Header("Visuals")]
        [SerializeField] private Sprite icon;
        public Sprite Icon => icon;

        [Header("Muzzle Flash Override")]
        [SerializeField] private MuzzleFlashSettings muzzleFlashOverride;
        public MuzzleFlashSettings MuzzleFlashOverride => muzzleFlashOverride;

        [Header("Stat Modifiers")]
        [SerializeField] private List<StatModifier> modifiers = new();
        public IReadOnlyList<StatModifier> Modifiers => modifiers;

        [Header("Bullet Override")]
        [SerializeField] private BulletProperties bulletPropertiesOverride;
        public BulletProperties BulletPropertiesOverride => bulletPropertiesOverride;

        [Header("Weapon Compatibility")]
        [SerializeField] private List<WeaponClass> compatibleWeaponClasses;
        public IReadOnlyList<WeaponClass> CompatibleWeaponClasses => compatibleWeaponClasses;

        [Header("Economy Stats")] 
        [SerializeField] private float modCost;
        public float ModCost => modCost;
    }
}
