using System.Collections.Generic;
using UnityEngine;

namespace Resonance.PlayerController
{
    [CreateAssetMenu(fileName = "Skin Catalog", menuName = "Scriptable Objects/Skin Catalog")]
    public class SkinCatalog : ScriptableObject
    {
        [SerializeField] private List<SkinData> skins = new();

        public int Count => skins.Count;

        public SkinData Get(int index)
        {
            if (index < 0 || index >= skins.Count)
            {
                Debug.LogError($"[SkinCatalog] Skin index {index} out of range (0-{skins.Count - 1})");
                return skins.Count > 0 ? skins[0] : null;
            }

            return skins[index];
        }
    }
}
