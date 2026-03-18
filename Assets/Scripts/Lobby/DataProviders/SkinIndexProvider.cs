using UnityEngine;
using UnityEngine.Events;

namespace Resonance.LobbySystem
{
    /// <summary>
    /// Persists the local player's selected skin index across scenes.
    /// </summary>
    public class SkinIndexProvider : MonoBehaviour
    {
        [SerializeField] private int skinIndex;

        public int SkinIndex { get; private set; }

        public UnityEvent<int> OnSkinIndexChanged = new();

        private void Awake() => DontDestroyOnLoad(gameObject);

        private void Start() => SetSkinIndex(skinIndex);

        public void SetSkinIndex(int index)
        {
            skinIndex = index;
            SkinIndex = index;
            OnSkinIndexChanged?.Invoke(index);
        }
    }
}
