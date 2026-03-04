using UnityEngine;

namespace Resonance.Combat.Weapons
{
    [CreateAssetMenu(fileName = "New Bullet Properties", menuName = "Resonance/Weapons/Bullet Properties")]
    
    public class BulletProperties : ScriptableObject
    {
        [SerializeField] private float bulletBaseSpeed;
        public float BulletBaseSpeed => bulletBaseSpeed;
        
        [SerializeField] private bool bulletGravity;
        public bool BulletGravity => bulletGravity;
        
        [SerializeField] private GameObject bulletPrefab;
        public GameObject BulletPrefab => bulletPrefab;
        
        [Header("On Hit Effects")]
        [SerializeField] private float damageOverTime;
        public float DamageOverTime => damageOverTime;

        [SerializeField] private float speedReduction;
        public float SpeedReduction => speedReduction;
        
        [SerializeField] private float lifesteal;
        public float Lifesteal => lifesteal;
    }
}
