using UnityEngine;

namespace Resonance.PlayerController
{
    [CreateAssetMenu(fileName = "New Skin", menuName = "Scriptable Objects/Skin Data")]
    public class SkinData : ScriptableObject
    {
        public string skinName;
        public GameObject bodyMeshPrefab;
        public Avatar bodyAvatar;
        public GameObject armsMeshPrefab;
        public Avatar armsAvatar;
    }
}
