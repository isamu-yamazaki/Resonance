using UnityEngine;
using UnityEngine.Events;

namespace Resonance.LobbySystem.DataProviders
{
    public class FPSCounterDisplaySetting : MonoBehaviour
    {
        public static FPSCounterDisplaySetting Instance { get; private set; }

        [SerializeField] private bool isEnabled;

        public bool IsEnabled { get; private set; }

        public UnityEvent<bool> OnIsEnabledChanged = new();

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

        private void Start() => SetEnabled(isEnabled);

        public void SetEnabled(bool value)
        {
            isEnabled = value;
            IsEnabled = value;
            OnIsEnabledChanged?.Invoke(value);
        }
    }
}
