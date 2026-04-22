using UnityEngine;
using UnityEngine.Events;

namespace Resonance.LobbySystem.DataProviders
{
    /// <summary>
    /// Persists the local player's selected skin index across scenes.
    /// </summary>
    public class SkinIndexProvider : MonoBehaviour
    {
        public static SkinIndexProvider Instance { get; private set; }

        [SerializeField] private int skinIndex;

        public int SkinIndex { get; private set; }

        public UnityEvent<int> OnSkinIndexChanged = new();

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(this);
        }

        private void Start() => SetSkinIndex(skinIndex);

        public void SetSkinIndex(int index)
        {
            skinIndex = index;
            SkinIndex = index;
            OnSkinIndexChanged?.Invoke(index);
        }
    }
}
