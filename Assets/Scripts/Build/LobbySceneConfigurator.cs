using Resonance.LobbySystem;
using UnityEngine;

namespace Resonance.BuildTools
{
    public class LobbySceneConfigurator : MonoBehaviour
    {
        public static ClientBuildConfig Current { get; private set; }

        [SerializeField] ClientBuildConfig config;
        [SerializeField] LobbyManager lobbyManager;
        [SerializeField] GameObject steamProvider;
        [SerializeField] GameObject dummyProvider;

        void Awake()
        {
            Current = config;
            if (steamProvider != null)
            {
                steamProvider.SetActive(config.enableSteamLobby);
                if (dummyProvider != null)
                {
                    dummyProvider.SetActive(!config.enableSteamLobby);
                }
            }
        }

        void Start()
        {
            if (lobbyManager == null)
            {
                return;
            }

            var provider = config.enableSteamLobby
                ? steamProvider.GetComponent<ILobbyProvider>()
                : dummyProvider.GetComponent<ILobbyProvider>();
            lobbyManager.SetProvider(provider);
        }
    }
}
