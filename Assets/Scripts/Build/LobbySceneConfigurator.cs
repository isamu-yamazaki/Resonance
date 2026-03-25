using Resonance.LobbySystem;
using UnityEngine;

namespace Resonance.BuildTools
{
    public class LobbySceneConfigurator : MonoBehaviour
    {
        [SerializeField] LobbyManager lobbyManager;
        [SerializeField] GameObject steamProvider;
        [SerializeField] GameObject dummyProvider;

        void Awake()
        {
            var receiver = FindFirstObjectByType<ClientBuildConfigReceiver>();
            if (receiver == null)
            {
                Debug.LogError("[LobbySceneConfigurator] No ClientBuildConfigReceiver found in scene.");
                return;
            }

            var config = receiver.Config;
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
            var receiver = FindFirstObjectByType<ClientBuildConfigReceiver>();
            if (receiver == null || lobbyManager == null)
            {
                return;
            }

            var config = receiver.Config;
            var provider = config.enableSteamLobby
                ? steamProvider.GetComponent<ILobbyProvider>()
                : dummyProvider.GetComponent<ILobbyProvider>();
            lobbyManager.SetProvider(provider);
        }
    }
}
