using UnityEngine;

namespace Resonance.Combat.Weapons
{
    [CreateAssetMenu(fileName = "New Weapon Class Camera Config", menuName = "Resonance/Camera/Weapon Class Camera Config")]
    public class WeaponClassCameraConfig : ScriptableObject
    {
        [Header("Camera")]
        public Vector3 cameraLocalPosition = new Vector3(0f, 1.67f, 0f);
        public float aimOffset = 0f;
    }
}