using Resonance.Combat.Weapons.Enums;
using UnityEngine;

namespace Resonance.Combat
{
    public class WeaponEquipSlot : MonoBehaviour
    {
        public WeaponClass weaponClass;
        [SerializeField] private Transform handBoneToFollow;
        [SerializeField] private Vector3 positionOffset;
        [SerializeField] private Vector3 rotationOffset;

        private void LateUpdate()
        {
            if (handBoneToFollow == null) return;
            transform.position = handBoneToFollow.position + handBoneToFollow.TransformDirection(positionOffset);
            transform.rotation = handBoneToFollow.rotation * Quaternion.Euler(rotationOffset);
        }
    }
}