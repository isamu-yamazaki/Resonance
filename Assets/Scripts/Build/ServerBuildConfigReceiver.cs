using UnityEngine;

namespace Resonance.BuildTools
{
    public class ServerBuildConfigReceiver : MonoBehaviour
    {
        public static ServerBuildConfigReceiver Instance { get; private set; }

        /// <summary>
        /// During build time, this field is injected with the correct config.
        /// In editor mode, this can be changed to simulate a different build config.
        /// </summary>
        [SerializeField] ServerBuildConfig config;

        public ServerBuildConfig Config => config;

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
