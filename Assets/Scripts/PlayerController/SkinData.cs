using System.Collections.Generic;
using Resonance.Combat.Weapons.Enums;
using UnityEngine;

namespace Resonance.PlayerController
{
    [System.Serializable]
    public class FPArmsEntry
    {
        public WeaponClass weaponClass;
        public GameObject armsPrefab;
        public RuntimeAnimatorController animatorController;
    }

    [CreateAssetMenu(fileName = "New Skin", menuName = "Scriptable Objects/Skin Data")]
    public class SkinData : ScriptableObject
    {
        public string skinName;
        public GameObject bodyMeshPrefab;
        public Avatar bodyAvatar;
        public List<FPArmsEntry> fpArmsVariants = new List<FPArmsEntry>();
        
        public GameObject skillArmsPrefab;
        public RuntimeAnimatorController skillArmsAnimatorController;
        
        public GameObject grappleArmsPrefab;
        public RuntimeAnimatorController grappleArmsAnimatorController;
    }
}