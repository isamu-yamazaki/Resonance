using System.Collections.Generic;
using UnityEngine;

namespace Resonance.Combat.Weapons
{
    [CreateAssetMenu(fileName = "New Bullet Properties", menuName = "Resonance/Weapons/Bullet Properties")]
    
    public class BulletProperties : ScriptableObject
    {
        [Tooltip("Unique identifier.")]
        [SerializeField] private string key;
        public string Key => key;
        
        [SerializeField] private float bulletBaseSpeed;
        public float BulletBaseSpeed => bulletBaseSpeed;
        
        [SerializeField] private bool bulletGravity;
        public bool BulletGravity => bulletGravity;
        
        [SerializeField] private GameObject bulletPrefab;
        public GameObject BulletPrefab => bulletPrefab;

        [Header("Visuals")]
        [SerializeField] private TrailRenderer bulletTrailPrefab;
        public TrailRenderer BulletTrailPrefab => bulletTrailPrefab;
        
        [Header("On Hit Effects")]
        [SerializeField] private List<IBulletEffect> bulletEffects;
    }
}