using Resonance.Combat.Mods;
using UnityEngine;
using UnityEngine.Serialization;

namespace Resonance.Combat.Augments
{
    [CreateAssetMenu(fileName = "New Augment Properties", menuName = "Resonance/Augment/Augment Properties")]
    public class AugmentProperties : ScriptableObject
    {
        [Tooltip("Unique identifier.")]
        [SerializeField] private string key;
        public string Key => key;
        
        [Header("Flavor Text")]
        [SerializeField] private string augmentName;
        public string AugmentName => augmentName;
        
        [TextArea(1, 5)]
        [SerializeField] private string description;
        public string Description => description;
        
        [SerializeField] private Sprite icon;
        public Sprite Icon => icon;
        
        [Header("Enum Identifiers")]
        [SerializeField] private AugmentSlot slot;
        public AugmentSlot Slot => slot;
        
        [Header("Augmented Player Stats")]
        [SerializeField] private float speed;
        public float Speed => speed;
        
        [SerializeField] private float regen;
        public float Regen => regen;

        [SerializeField] private float damageReduction;
        public float DamageReduction => damageReduction;
        
        [Header("Augmented Weapon Mod")]
        [SerializeField] private WeaponModProperties modProperties;
        public WeaponModProperties ModProperties => modProperties;
        
        [Header("Augment Ability")]
        [SerializeField] private string abilityKey;
        public string AbilityKey => abilityKey;
    }
}
