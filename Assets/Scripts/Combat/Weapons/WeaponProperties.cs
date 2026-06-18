using System.Collections.Generic;
using Resonance.Combat.Mods;
using Resonance.Combat.Weapons.Enums;
using UnityEditor;
using UnityEngine;
using UnityEngine.Serialization;

namespace Resonance.Combat.Weapons
{
    [CreateAssetMenu(fileName = "New Weapon Properties", menuName = "Resonance/Weapons/Weapon Properties")]
    public class WeaponProperties : ScriptableObject
    {
        /// <summary>
        /// The internal ID of a weapon that is populated when a weapon is cloned.
        /// </summary>
        public string Id { get; private set; }

        [Tooltip("Unique identifier.")]
        [SerializeField] private string key;
        public string Key => key;

        [Header("Flavor Text")]
        [SerializeField] private string weaponName;
        public string WeaponName => weaponName;

        [TextArea(1, 5)]
        [SerializeField] private string description;
        public string Description => description;

        [Header("Enum Identifiers")]
        [SerializeField] private WeaponSlot slot;
        public WeaponSlot Slot => slot;

        [SerializeField] private WeaponClass weaponClass;
        public WeaponClass Class => weaponClass;

        [SerializeField] private WeaponFiringType firingType;
        public WeaponFiringType FiringType => firingType;

        [Header("Weapon Visuals")]
        [SerializeField] private Sprite icon;
        public Sprite Icon => icon;

        [SerializeField] private string weaponMuzzleKey;
        public string WeaponMuzzleKey => weaponMuzzleKey;

        [Header("Audio")]
        [SerializeField] private WeaponAudioProperties audioProperties;
        public WeaponAudioProperties AudioProperties => audioProperties;

        [Header("Damage Stats")]
        [SerializeField] private float damage;
        public float Damage => damage;

        [SerializeField] private float fireRate;
        public float FireRate => fireRate;

        [SerializeField] private int projectilesPerShot;
        public int ProjectilesPerShot => projectilesPerShot;

        [Header("Aim Stats")]
        [SerializeField] private float range;
        public float Range => range;

        [SerializeField] private float accuracy;
        public float Accuracy => accuracy;

        [SerializeField] private float control;
        public float Control => control;

        [SerializeField] private float muzzleVelocity;
        public float MuzzleVelocity => muzzleVelocity;

        [Header("Spread Settings")]
        [SerializeField] private float spread;
        public float Spread => spread;

        [SerializeField] private float spreadPerShot;
        public float SpreadPerShot => spreadPerShot;

        [SerializeField] private float maxSpread;
        public float MaxSpread => maxSpread;

        [SerializeField] private float spreadRecoveryRate;
        public float SpreadRecoveryRate => spreadRecoveryRate;

        [Header("Action Stats")]
        [SerializeField] private float mobility = 1f;
        public float Mobility => mobility;

        [SerializeField] private float handling;
        public float Handling => handling;

        [SerializeField] private AnimationClip reloadClip;
        public AnimationClip ReloadClip => reloadClip;

        [SerializeField] private AnimationClip emptyReloadClip;
        public AnimationClip EmptyReloadClip => emptyReloadClip;

        [SerializeField] private AnimationClip fireClip;
        public AnimationClip FireClip => fireClip;
        [Header("Ammo Stats")]
        [SerializeField] private int magazineSize;
        public int MagazineSize => magazineSize;

        [SerializeField] private float reloadTime = 3.3f;
        public float ReloadTime => reloadTime;

        [SerializeField] private BulletProperties bulletProperties;
        public BulletProperties BulletProperties => bulletProperties;

        [Header("Mod List")]
        [SerializeField] private List<WeaponModProperties> modList;
        public List<WeaponModProperties> ModList => modList;

        [Header("Economy Stats")]
        [SerializeField] private float weaponCost;
        public float WeaponCost => weaponCost;

        public WeaponProperties Clone(string id = null)
        {
            WeaponProperties clone = CreateInstance<WeaponProperties>();

            clone.key = key;
            clone.weaponName = weaponName;
            clone.description = description;
            clone.slot = slot;
            clone.weaponClass = weaponClass;
            clone.firingType = firingType;
            clone.icon = icon;
            clone.weaponMuzzleKey = weaponMuzzleKey;
            clone.audioProperties = audioProperties;
            clone.damage = damage;
            clone.fireRate = fireRate;
            clone.range = range;
            clone.accuracy = accuracy;
            clone.control = control;
            clone.mobility = mobility;
            clone.handling = handling;
            clone.magazineSize = magazineSize;
            clone.bulletProperties = bulletProperties;
            clone.projectilesPerShot = projectilesPerShot;
            clone.spread = spread;
            clone.reloadTime = reloadTime;
            clone.spreadPerShot = spreadPerShot;
            clone.maxSpread = maxSpread;
            clone.spreadRecoveryRate = spreadRecoveryRate;
            clone.modList = new List<WeaponModProperties>(modList ?? new List<WeaponModProperties>());
            clone.weaponCost = weaponCost;
            clone.reloadClip = reloadClip;
            clone.emptyReloadClip = emptyReloadClip;
            clone.fireClip = fireClip;

            clone.Id = id ?? GUID.Generate().ToString();

            return clone;
        }
    }
}
