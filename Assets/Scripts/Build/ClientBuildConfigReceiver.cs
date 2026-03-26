using UnityEngine;

namespace Resonance.BuildTools
{
    public class ClientBuildConfigReceiver : MonoBehaviour
    {
        public static ClientBuildConfigReceiver Instance { get; private set; }

        /// <summary>
        /// During build time, this field is injected with the correct config.
        /// In editor mode, this can be changed to simulate a different build config.
        /// </summary>
        [SerializeField] ClientBuildConfig config;

        public ClientBuildConfig Config => config;

        void Awake()
        {
            if (Instance != null)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
    }
}
