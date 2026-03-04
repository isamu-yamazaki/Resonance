using UnityEngine;

namespace Resonance.Combat.Weapons
{
    public class WeaponView : MonoBehaviour
    {
        [SerializeField] private Transform muzzle;
        public Transform Muzzle => muzzle;
    }
}