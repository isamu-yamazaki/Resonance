using UnityEngine;

namespace Resonance.Combat.Weapons
{
    public class WeaponView : MonoBehaviour
    {
        [SerializeField] private Transform muzzle;
        public Transform Muzzle => muzzle;

        [SerializeField] private MuzzleFlash muzzleFlash;

        public void PlayMuzzleFlash()
        {
            if (muzzleFlash != null)
                muzzleFlash.Play();
        }

        public void ApplyMuzzleFlashSettings(MuzzleFlashSettings settings)
        {
            if (muzzleFlash != null && settings != null)
                muzzleFlash.ApplySettings(settings);
        }
    }
}
